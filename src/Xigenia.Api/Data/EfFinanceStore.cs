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
}
