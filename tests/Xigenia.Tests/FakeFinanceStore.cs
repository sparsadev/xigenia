using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

internal sealed class FakeFinanceStore : IFinanceStore
{
    private readonly Dictionary<(Guid AccountId, string OwnerId), FinancialAccount> _accounts = new();

    public List<(Guid AccountId, string OwnerId)> AccountLookups { get; } = new();

    public List<(Guid AccountId, string OwnerId, CancellationToken CancellationToken)> AccountLookupCalls { get; } = new();

    public List<MoneyMovement> AddedMovements { get; } = new();

    public List<(MoneyMovement Movement, CancellationToken CancellationToken)> MoneyMovementAddCalls { get; } = new();

    public Exception? ListMoneyMovementsError { get; set; }

    public Exception? MoneyMovementAddError { get; set; }

    public List<string> OperationTrace { get; } = new();

    public List<(MoneyMovement SourceLeg, MoneyMovement DestinationLeg)> RecordedTransfers { get; } = new();

    public void SeedAccount(FinancialAccount account) => _accounts[(account.Id, account.OwnerId)] = account;

    private readonly Dictionary<Guid, FinancialCategory> _categories = new();

    private readonly Dictionary<(Guid ObligationId, string OwnerId), Obligation> _obligations = new();

    private readonly Dictionary<(Guid ObligationId, string OwnerId), decimal> _settledTotals = new();

    public List<(Guid CategoryId, string OwnerId)> CategoryLookups { get; } = new();

    public List<(Guid ObligationId, string OwnerId)> ObligationLookups { get; } = new();

    public List<(Guid ObligationId, string OwnerId)> SettlementLookups { get; } = new();

    public List<MoneyMovement> LinkedMovements { get; } = new();

    public void SeedCategory(FinancialCategory category) => _categories[category.Id] = category;

    public List<FinancialCategory> AddedCategories { get; } = new();

    public List<FinancialCategory> UpdatedCategories { get; } = new();

    public List<(FinancialCategory Category, CancellationToken CancellationToken)> CategoryUpdateCalls { get; } = new();

    public List<(string OwnerId, CancellationToken CancellationToken)> CategoryListCalls { get; } = new();

    public List<(Guid CategoryId, string OwnerId, CancellationToken CancellationToken)> CategoryLookupCalls { get; } = new();

    public void SeedObligation(Obligation obligation) => _obligations[(obligation.Id, obligation.OwnerId)] = obligation;

    public void SeedSettlementTotal(Guid obligationId, string ownerId, decimal settledTotal) =>
        _settledTotals[(obligationId, ownerId)] = settledTotal;

    private readonly List<MoneyMovement> _movements = new();

    public int ConfiguredTotalCount { get; set; }

    public MoneyMovementSummaryResponse? ConfiguredSummary { get; set; }

    public List<(string OwnerId, CancellationToken CancellationToken)> MovementListCalls { get; } = new();

    public List<(string OwnerId, MovementFilter Filter, MovementPaging Paging, MovementSorting Sorting, CancellationToken CancellationToken)> FilteredListCalls { get; } = new();

    public List<(string OwnerId, MovementFilter Filter, CancellationToken CancellationToken)> CountCalls { get; } = new();

    public List<(string OwnerId, MovementFilter Filter, CancellationToken CancellationToken)> SummaryCalls { get; } = new();

    public void SeedMovement(MoneyMovement movement) => _movements.Add(movement);

    public List<Obligation> AddedObligations { get; } = new();

    public List<FinancialAccount> AddedAccounts { get; } = new();

    public List<(string OwnerId, CancellationToken CancellationToken)> ObligationListCalls { get; } = new();

    public List<(string OwnerId, CancellationToken CancellationToken)> AccountListCalls { get; } = new();

    public Task AddMoneyMovementAsync(MoneyMovement movement, CancellationToken cancellationToken)
    {
        OperationTrace.Add("AddMovement");
        MoneyMovementAddCalls.Add((movement, cancellationToken));
        if (MoneyMovementAddError is not null)
        {
            throw MoneyMovementAddError;
        }

        AddedMovements.Add(movement);
        return Task.CompletedTask;
    }

    public Task<FinancialAccount?> GetFinancialAccountAsync(Guid accountId, string ownerId, CancellationToken cancellationToken)
    {
        OperationTrace.Add("GetAccount");
        AccountLookups.Add((accountId, ownerId));
        AccountLookupCalls.Add((accountId, ownerId, cancellationToken));
        _accounts.TryGetValue((accountId, ownerId), out var account);
        return Task.FromResult(account);
    }

    public Task RecordTransferAsync(MoneyMovement sourceLeg, MoneyMovement destinationLeg, CancellationToken cancellationToken)
    {
        RecordedTransfers.Add((sourceLeg, destinationLeg));
        return Task.CompletedTask;
    }

    public Task AddObligationAsync(Obligation obligation, CancellationToken cancellationToken)
    {
        AddedObligations.Add(obligation);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Obligation>> ListObligationsAsync(string ownerId, CancellationToken cancellationToken)
    {
        OperationTrace.Add("ListObligations");
        ObligationListCalls.Add((ownerId, cancellationToken));
        IReadOnlyList<Obligation> rows = _obligations
            .Where(kv => kv.Key.OwnerId == ownerId)
            .Select(kv => kv.Value)
            .ToList();
        return Task.FromResult(rows);
    }

    public Task<IReadOnlyList<MoneyMovement>> ListMoneyMovementsAsync(string ownerId, CancellationToken cancellationToken)
    {
        OperationTrace.Add("ListMovements");
        MovementListCalls.Add((ownerId, cancellationToken));
        if (ListMoneyMovementsError is not null)
        {
            throw ListMoneyMovementsError;
        }

        IReadOnlyList<MoneyMovement> rows = _movements.Where(m => m.OwnerId == ownerId).ToList();
        return Task.FromResult(rows);
    }

    public Task<IReadOnlyList<MoneyMovement>> ListMoneyMovementsAsync(
        string ownerId,
        MovementFilter filter,
        MovementPaging paging,
        MovementSorting sorting,
        CancellationToken cancellationToken)
    {
        FilteredListCalls.Add((ownerId, filter, paging, sorting, cancellationToken));
        IReadOnlyList<MoneyMovement> rows = _movements.Where(m => m.OwnerId == ownerId).ToList();
        return Task.FromResult(rows);
    }

    public Task<int> CountMoneyMovementsAsync(string ownerId, MovementFilter filter, CancellationToken cancellationToken)
    {
        CountCalls.Add((ownerId, filter, cancellationToken));
        return Task.FromResult(ConfiguredTotalCount);
    }

    public Task<MoneyMovementSummaryResponse> SummarizeMoneyMovementsAsync(
        string ownerId,
        MovementFilter filter,
        CancellationToken cancellationToken)
    {
        OperationTrace.Add("SummarizeMovements");
        SummaryCalls.Add((ownerId, filter, cancellationToken));
        var summary = ConfiguredSummary
            ?? new MoneyMovementSummaryResponse(Array.Empty<MoneyMovementCurrencySummary>(), 0, 0);
        return Task.FromResult(summary);
    }

    public Task<Obligation?> GetObligationAsync(Guid obligationId, string ownerId, CancellationToken cancellationToken)
    {
        ObligationLookups.Add((obligationId, ownerId));
        _obligations.TryGetValue((obligationId, ownerId), out var obligation);
        return Task.FromResult(obligation);
    }

    public Task<decimal> SumLinkedMovementsAsync(Guid obligationId, string ownerId, CancellationToken cancellationToken)
    {
        SettlementLookups.Add((obligationId, ownerId));
        _settledTotals.TryGetValue((obligationId, ownerId), out var settledTotal);
        return Task.FromResult(settledTotal);
    }

    public Task RecordLinkedMovementAsync(MoneyMovement movement, CancellationToken cancellationToken)
    {
        LinkedMovements.Add(movement);
        return Task.CompletedTask;
    }

    public Task AddFinancialAccountAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        AddedAccounts.Add(account);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FinancialAccount>> ListFinancialAccountsAsync(string ownerId, CancellationToken cancellationToken)
    {
        OperationTrace.Add("ListAccounts");
        AccountListCalls.Add((ownerId, cancellationToken));
        IReadOnlyList<FinancialAccount> rows = _accounts
            .Where(kv => kv.Key.OwnerId == ownerId)
            .Select(kv => kv.Value)
            .ToList();
        return Task.FromResult(rows);
    }

    public Task AddFinancialCategoryAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        AddedCategories.Add(category);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FinancialCategory>> ListAccessibleCategoriesAsync(string ownerId, CancellationToken cancellationToken)
    {
        CategoryListCalls.Add((ownerId, cancellationToken));
        IReadOnlyList<FinancialCategory> rows = _categories.Values
            .Where(c => (c.OwnerId is null && c.IsActive) || c.OwnerId == ownerId)
            .ToList();
        return Task.FromResult(rows);
    }

    public Task<FinancialCategory?> GetAccessibleCategoryAsync(Guid categoryId, string ownerId, CancellationToken cancellationToken)
    {
        OperationTrace.Add("GetCategory");
        CategoryLookups.Add((categoryId, ownerId));
        CategoryLookupCalls.Add((categoryId, ownerId, cancellationToken));
        _categories.TryGetValue(categoryId, out var category);
        if (category is not null && category.OwnerId is not null && category.OwnerId != ownerId)
        {
            category = null;
        }

        return Task.FromResult(category);
    }

    public Task UpdateFinancialCategoryAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        OperationTrace.Add("UpdateCategory");
        UpdatedCategories.Add(category);
        CategoryUpdateCalls.Add((category, cancellationToken));
        return Task.CompletedTask;
    }
}
