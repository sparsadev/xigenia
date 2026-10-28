using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public sealed class FinanceService
{
    private readonly IFinanceStore _store;

    public FinanceService(IFinanceStore store)
    {
        _store = store;
    }

    public async Task<ObligationResponse> CreateObligationAsync(
        string ownerId,
        CreateObligationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(request);

        var obligation = new Obligation(
            Guid.NewGuid(),
            ownerId,
            request.Kind,
            request.Title,
            new Money(request.Amount, request.Currency),
            request.IssueDate,
            request.DueDate);

        await _store.AddObligationAsync(obligation, cancellationToken);

        return Map(obligation, 0);
    }

    public async Task<IReadOnlyList<ObligationResponse>> ListObligationsAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var obligations = await _store.ListObligationsAsync(ownerId, cancellationToken);
        var movements = await _store.ListMoneyMovementsAsync(ownerId, cancellationToken);

        var settledByObligation = movements
            .Where(m => m.ObligationId.HasValue)
            .GroupBy(m => m.ObligationId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Amount.Amount));

        return obligations
            .Select(o => Map(o, settledByObligation.GetValueOrDefault(o.Id)))
            .ToList();
    }

    public async Task<MoneyMovementResponse> CreateMoneyMovementAsync(
        string ownerId,
        CreateMoneyMovementRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(request);

        var amount = new Money(request.Amount, request.Currency);

        if (request.ObligationId is null)
        {
            var standalone = new MoneyMovement(
                Guid.NewGuid(),
                ownerId,
                null,
                request.Direction,
                amount,
                request.MovementDate,
                request.Note);

            await _store.AddMoneyMovementAsync(standalone, cancellationToken);

            return Map(standalone);
        }

        var obligation = await _store.GetObligationAsync(request.ObligationId.Value, ownerId, cancellationToken);
        if (obligation is null)
        {
            throw new KeyNotFoundException($"Obligation '{request.ObligationId.Value}' was not found.");
        }

        var settled = await _store.SumLinkedMovementsAsync(obligation.Id, ownerId, cancellationToken);
        MovementRules.AssertCompatible(obligation.Kind, request.Direction);
        MovementRules.AssertSameCurrency(obligation.Total.Currency, amount.Currency);
        MovementRules.AssertWithinBalance(obligation.Total.Amount, settled, amount.Amount);

        var movement = new MoneyMovement(
            Guid.NewGuid(),
            ownerId,
            obligation.Id,
            request.Direction,
            amount,
            request.MovementDate,
            request.Note);

        await _store.RecordLinkedMovementAsync(movement, cancellationToken);

        return Map(movement);
    }

    public async Task<IReadOnlyList<MoneyMovementResponse>> ListMoneyMovementsAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var movements = await _store.ListMoneyMovementsAsync(ownerId, cancellationToken);

        return movements.Select(Map).ToList();
    }

    public async Task<FinancialAccountResponse> CreateFinancialAccountAsync(
        string ownerId,
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(request);

        var account = new FinancialAccount(
            Guid.NewGuid(),
            ownerId,
            request.Name,
            request.Type,
            request.Currency,
            request.OpeningBalance,
            true,
            DateTimeOffset.UtcNow,
            request.CreditLimit,
            request.StatementClosingDay,
            request.PaymentDueDay);

        await _store.AddFinancialAccountAsync(account, cancellationToken);

        return Map(account);
    }

    public async Task<IReadOnlyList<FinancialAccountResponse>> ListFinancialAccountsAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var accounts = await _store.ListFinancialAccountsAsync(ownerId, cancellationToken);

        return accounts.Select(Map).ToList();
    }

    public async Task<FinancialAccountResponse?> GetFinancialAccountAsync(
        string ownerId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var account = await _store.GetFinancialAccountAsync(accountId, ownerId, cancellationToken);

        return account is null ? null : Map(account);
    }

    private static ObligationResponse Map(Obligation obligation, decimal settledSum) => new(
        obligation.Id,
        obligation.Kind,
        obligation.Title,
        obligation.Total.Amount,
        obligation.Total.Currency,
        obligation.IssueDate,
        obligation.DueDate,
        MovementRules.Outstanding(obligation.Total.Amount, settledSum));

    private static MoneyMovementResponse Map(MoneyMovement movement) => new(
        movement.Id,
        movement.ObligationId,
        movement.Direction,
        movement.Amount.Amount,
        movement.Amount.Currency,
        movement.MovementDate,
        movement.Note);

    private static FinancialAccountResponse Map(FinancialAccount account) => new(
        account.Id,
        account.Name,
        account.Type,
        account.Currency,
        account.OpeningBalance,
        account.IsActive,
        account.CreatedAt,
        account.CreditLimit,
        account.StatementClosingDay,
        account.PaymentDueDay);
}
