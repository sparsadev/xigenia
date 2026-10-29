using System.Data;
using Microsoft.EntityFrameworkCore;
using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Api.Data;

public sealed class EfFinanceStore(ApplicationDbContext context) : IFinanceStore
{
    public async Task AddObligationAsync(Obligation obligation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(obligation);

        context.Obligations.Add(obligation);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Obligation>> ListObligationsAsync(string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.Obligations
            .AsNoTracking()
            .Where(o => o.OwnerId == ownerId)
            .OrderBy(o => o.IssueDate)
            .ThenBy(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMoneyMovementAsync(MoneyMovement movement, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(movement);

        if (movement.FinancialAccountId is null)
        {
            context.MoneyMovements.Add(movement);
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        await AssertAccountLinkAsync(movement, cancellationToken);

        context.MoneyMovements.Add(movement);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MoneyMovement>> ListMoneyMovementsAsync(string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.MoneyMovements
            .AsNoTracking()
            .Where(m => m.OwnerId == ownerId)
            .OrderBy(m => m.MovementDate)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MoneyMovement>> ListMoneyMovementsAsync(
        string ownerId,
        MovementFilter filter,
        MovementPaging paging,
        MovementSorting sorting,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(paging);
        ArgumentNullException.ThrowIfNull(sorting);

        IQueryable<MoneyMovement> query = ApplyMovementSorting(FilterMoneyMovements(ownerId, filter), sorting);

        if (paging.Page.HasValue || paging.PageSize.HasValue)
        {
            if (!paging.Page.HasValue || !paging.PageSize.HasValue)
            {
                throw new ArgumentException("Page and PageSize must be provided together.", nameof(paging));
            }

            if (paging.Page.Value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(paging), "Page must be greater than zero.");
            }

            if (paging.PageSize.Value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(paging), "PageSize must be greater than zero.");
            }

            query = query
                .Skip((paging.Page.Value - 1) * paging.PageSize.Value)
                .Take(paging.PageSize.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountMoneyMovementsAsync(string ownerId, MovementFilter filter, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(filter);

        return await FilterMoneyMovements(ownerId, filter).CountAsync(cancellationToken);
    }

    public async Task<MoneyMovementSummaryResponse> SummarizeMoneyMovementsAsync(
        string ownerId,
        MovementFilter filter,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(filter);

        var query = FilterMoneyMovements(ownerId, filter);

        var rows = await query
            .GroupBy(m => m.Amount.Currency)
            .Select(g => new
            {
                Currency = g.Key,
                Income = g.Sum(m => m.Classification == MovementClassification.Income ? m.Amount.Amount : 0m),
                Expenses = g.Sum(m => m.Classification == MovementClassification.Expense ? m.Amount.Amount : 0m),
                MovementCount = g.Count(),
            })
            .OrderBy(r => r.Currency)
            .ToListAsync(cancellationToken);

        var transferCount = await query
            .Where(m => m.Classification == MovementClassification.InternalTransfer)
            .CountAsync(cancellationToken);

        var unclassifiedCount = await query
            .Where(m => m.Classification == MovementClassification.Unclassified)
            .CountAsync(cancellationToken);

        var totals = rows
            .Select(r => new MoneyMovementCurrencySummary(r.Currency, r.Income, r.Expenses, r.Income - r.Expenses, r.MovementCount))
            .ToList();

        return new MoneyMovementSummaryResponse(totals, transferCount, unclassifiedCount);
    }

    private IQueryable<MoneyMovement> FilterMoneyMovements(string ownerId, MovementFilter filter)
    {
        var query = context.MoneyMovements
            .AsNoTracking()
            .Where(m => m.OwnerId == ownerId);

        if (filter.From.HasValue)
        {
            query = query.Where(m => m.MovementDate >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(m => m.MovementDate <= filter.To.Value);
        }

        if (filter.FinancialAccountId.HasValue)
        {
            query = query.Where(m => m.FinancialAccountId == filter.FinancialAccountId.Value);
        }

        if (filter.FinancialCategoryId.HasValue)
        {
            query = query.Where(m => m.FinancialCategoryId == filter.FinancialCategoryId.Value);
        }

        if (filter.Classification.HasValue)
        {
            query = query.Where(m => m.Classification == filter.Classification.Value);
        }

        return query;
    }

    private static IOrderedQueryable<MoneyMovement> ApplyMovementSorting(IQueryable<MoneyMovement> query, MovementSorting sorting)
    {
        if (sorting.Sort != MovementSortField.MovementDate)
        {
            throw new ArgumentOutOfRangeException(nameof(sorting), "Only MovementDate sorting is supported.");
        }

        if (!Enum.IsDefined(sorting.SortDirection))
        {
            throw new ArgumentOutOfRangeException(nameof(sorting), "Sort direction is not recognized.");
        }

        return sorting.SortDirection == MovementSortDirection.Desc
            ? query.OrderByDescending(m => m.MovementDate).ThenBy(m => m.Id)
            : query.OrderBy(m => m.MovementDate).ThenBy(m => m.Id);
    }

    public async Task<Obligation?> GetObligationAsync(Guid obligationId, string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.Obligations
            .AsNoTracking()
            .Where(o => o.Id == obligationId && o.OwnerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> SumLinkedMovementsAsync(Guid obligationId, string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.MoneyMovements
            .AsNoTracking()
            .Where(m => m.ObligationId == obligationId && m.OwnerId == ownerId)
            .SumAsync(m => m.Amount.Amount, cancellationToken);
    }

    public async Task RecordLinkedMovementAsync(MoneyMovement movement, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(movement);
        if (movement.ObligationId is null)
        {
            throw new ArgumentException("Movement must reference an obligation.", nameof(movement));
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var obligationId = movement.ObligationId.Value;
        var obligation = await context.Obligations
            .AsNoTracking()
            .Where(o => o.Id == obligationId && o.OwnerId == movement.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);
        if (obligation is null)
        {
            throw new KeyNotFoundException($"Obligation '{obligationId}' was not found.");
        }

        var settled = await context.MoneyMovements
            .AsNoTracking()
            .Where(m => m.ObligationId == obligationId && m.OwnerId == movement.OwnerId)
            .SumAsync(m => m.Amount.Amount, cancellationToken);

        MovementRules.AssertCompatible(obligation.Kind, movement.Direction);
        MovementRules.AssertSameCurrency(obligation.Total.Currency, movement.Amount.Currency);
        MovementRules.AssertWithinBalance(obligation.Total.Amount, settled, movement.Amount.Amount);
        await AssertAccountLinkAsync(movement, cancellationToken);

        context.MoneyMovements.Add(movement);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddFinancialAccountAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);

        context.FinancialAccounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialAccount>> ListFinancialAccountsAsync(string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.OwnerId == ownerId)
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<FinancialAccount?> GetFinancialAccountAsync(Guid accountId, string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.Id == accountId && a.OwnerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task AssertAccountLinkAsync(MoneyMovement movement, CancellationToken cancellationToken)
    {
        if (movement.FinancialAccountId is null)
        {
            return;
        }

        var account = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.Id == movement.FinancialAccountId.Value && a.OwnerId == movement.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            throw new KeyNotFoundException($"Financial account '{movement.FinancialAccountId.Value}' was not found.");
        }

        MovementRules.AssertAccountActive(account);
        MovementRules.AssertSameAccountCurrency(account.Currency, movement.Amount.Currency);
    }

    public async Task RecordTransferAsync(MoneyMovement sourceLeg, MoneyMovement destinationLeg, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceLeg);
        ArgumentNullException.ThrowIfNull(destinationLeg);
        if (sourceLeg.TransferGroupId is null || sourceLeg.TransferGroupId != destinationLeg.TransferGroupId)
        {
            throw new ArgumentException("Transfer legs must share the same transfer group id.", nameof(destinationLeg));
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var source = await GetRequiredAccountAsync(sourceLeg.FinancialAccountId, sourceLeg.OwnerId, "Source", cancellationToken);
        var destination = await GetRequiredAccountAsync(destinationLeg.FinancialAccountId, destinationLeg.OwnerId, "Destination", cancellationToken);

        MovementRules.AssertDistinctAccounts(source.Id, destination.Id);
        MovementRules.AssertAssetTransferSource(source.Type);
        MovementRules.AssertAccountActive(source);
        MovementRules.AssertAccountActive(destination);
        MovementRules.AssertSameAccountCurrency(source.Currency, sourceLeg.Amount.Currency);
        MovementRules.AssertSameAccountCurrency(destination.Currency, destinationLeg.Amount.Currency);

        context.MoneyMovements.AddRange(sourceLeg, destinationLeg);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<FinancialAccount> GetRequiredAccountAsync(Guid? accountId, string ownerId, string role, CancellationToken cancellationToken)
    {
        if (accountId is null)
        {
            throw new ArgumentException($"{role} transfer leg must reference an account.");
        }

        var account = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.Id == accountId.Value && a.OwnerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            throw new KeyNotFoundException($"{role} account '{accountId.Value}' was not found.");
        }

        return account;
    }

    public async Task AddFinancialCategoryAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        context.FinancialCategories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialCategory>> ListAccessibleCategoriesAsync(string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.FinancialCategories
            .AsNoTracking()
            .Where(c => (c.OwnerId == null && c.IsActive) || c.OwnerId == ownerId)
            .OrderBy(c => c.Classification)
            .ThenBy(c => c.ParentCategoryId.HasValue)
            .ThenBy(c => c.Name)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<FinancialCategory?> GetAccessibleCategoryAsync(Guid categoryId, string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.FinancialCategories
            .AsNoTracking()
            .Where(c => c.Id == categoryId && (c.OwnerId == null || c.OwnerId == ownerId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateFinancialCategoryAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        context.FinancialCategories.Update(category);
        await context.SaveChangesAsync(cancellationToken);
    }
}
