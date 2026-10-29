using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public interface IFinanceStore
{
    Task AddObligationAsync(Obligation obligation, CancellationToken cancellationToken);

    Task<IReadOnlyList<Obligation>> ListObligationsAsync(string ownerId, CancellationToken cancellationToken);

    Task AddMoneyMovementAsync(MoneyMovement movement, CancellationToken cancellationToken);

    Task<IReadOnlyList<MoneyMovement>> ListMoneyMovementsAsync(string ownerId, CancellationToken cancellationToken);

    Task<Obligation?> GetObligationAsync(Guid obligationId, string ownerId, CancellationToken cancellationToken);

    Task<decimal> SumLinkedMovementsAsync(Guid obligationId, string ownerId, CancellationToken cancellationToken);

    Task RecordLinkedMovementAsync(MoneyMovement movement, CancellationToken cancellationToken);

    Task AddFinancialAccountAsync(FinancialAccount account, CancellationToken cancellationToken);

    Task<IReadOnlyList<FinancialAccount>> ListFinancialAccountsAsync(string ownerId, CancellationToken cancellationToken);

    Task<FinancialAccount?> GetFinancialAccountAsync(Guid accountId, string ownerId, CancellationToken cancellationToken);

    Task RecordTransferAsync(MoneyMovement sourceLeg, MoneyMovement destinationLeg, CancellationToken cancellationToken);

    Task AddFinancialCategoryAsync(FinancialCategory category, CancellationToken cancellationToken);

    Task<IReadOnlyList<FinancialCategory>> ListAccessibleCategoriesAsync(string ownerId, CancellationToken cancellationToken);

    Task<FinancialCategory?> GetAccessibleCategoryAsync(Guid categoryId, string ownerId, CancellationToken cancellationToken);

    Task UpdateFinancialCategoryAsync(FinancialCategory category, CancellationToken cancellationToken);
}
