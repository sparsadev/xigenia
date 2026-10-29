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

        if (request.FinancialAccountId.HasValue)
        {
            var account = await _store.GetFinancialAccountAsync(request.FinancialAccountId.Value, ownerId, cancellationToken);
            if (account is null)
            {
                throw new KeyNotFoundException($"Financial account '{request.FinancialAccountId.Value}' was not found.");
            }

            MovementRules.AssertAccountActive(account);
            MovementRules.AssertSameAccountCurrency(account.Currency, amount.Currency);
        }

        if (request.ObligationId is null)
        {
            var standalone = new MoneyMovement(
                Guid.NewGuid(),
                ownerId,
                null,
                request.Direction,
                amount,
                request.MovementDate,
                request.Note,
                request.FinancialAccountId,
                null,
                request.Classification);

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
            request.Note,
            request.FinancialAccountId,
            null,
            request.Classification);

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

        return Map(account, 0, 0, 0);
    }

    public async Task<IReadOnlyList<FinancialAccountResponse>> ListFinancialAccountsAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var accounts = await _store.ListFinancialAccountsAsync(ownerId, cancellationToken);
        var sums = await SumMovementsByAccountAsync(ownerId, cancellationToken);

        return accounts.Select(a =>
        {
            sums.TryGetValue(a.Id, out var sum);
            return Map(a, sum.Cobrar, sum.Pagar, sum.Count);
        }).ToList();
    }

    public async Task<FinancialAccountResponse?> GetFinancialAccountAsync(
        string ownerId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var account = await _store.GetFinancialAccountAsync(accountId, ownerId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var sums = await SumMovementsByAccountAsync(ownerId, cancellationToken);
        sums.TryGetValue(account.Id, out var sum);

        return Map(account, sum.Cobrar, sum.Pagar, sum.Count);
    }

    public async Task<TransferResponse> CreateTransferAsync(
        string ownerId,
        CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceAccountId == Guid.Empty || request.DestinationAccountId == Guid.Empty)
        {
            throw new ArgumentException("Transfer accounts must be valid.", nameof(request));
        }

        var amount = new Money(request.Amount, request.Currency);

        MovementRules.AssertDistinctAccounts(request.SourceAccountId, request.DestinationAccountId);

        var source = await _store.GetFinancialAccountAsync(request.SourceAccountId, ownerId, cancellationToken);
        if (source is null)
        {
            throw new KeyNotFoundException($"Source account '{request.SourceAccountId}' was not found.");
        }

        var destination = await _store.GetFinancialAccountAsync(request.DestinationAccountId, ownerId, cancellationToken);
        if (destination is null)
        {
            throw new KeyNotFoundException($"Destination account '{request.DestinationAccountId}' was not found.");
        }

        MovementRules.AssertAssetTransferSource(source.Type);
        MovementRules.AssertAccountActive(source);
        MovementRules.AssertAccountActive(destination);
        MovementRules.AssertSameAccountCurrency(source.Currency, amount.Currency);
        MovementRules.AssertSameAccountCurrency(destination.Currency, amount.Currency);

        var transferGroupId = Guid.NewGuid();
        var sourceLeg = new MoneyMovement(
            Guid.NewGuid(),
            ownerId,
            null,
            MovementDirection.Pagar,
            new Money(request.Amount, request.Currency),
            request.MovementDate,
            request.Note,
            source.Id,
            transferGroupId,
            MovementClassification.InternalTransfer);
        var destinationLeg = new MoneyMovement(
            Guid.NewGuid(),
            ownerId,
            null,
            MovementDirection.Cobrar,
            new Money(request.Amount, request.Currency),
            request.MovementDate,
            request.Note,
            destination.Id,
            transferGroupId,
            MovementClassification.InternalTransfer);

        await _store.RecordTransferAsync(sourceLeg, destinationLeg, cancellationToken);

        return new TransferResponse(transferGroupId, Map(sourceLeg), Map(destinationLeg));
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
        movement.FinancialAccountId,
        movement.TransferGroupId,
        movement.Direction,
        movement.Amount.Amount,
        movement.Amount.Currency,
        movement.MovementDate,
        movement.Note,
        movement.Classification);

    private static FinancialAccountResponse Map(FinancialAccount account, decimal cobrarTotal, decimal pagarTotal, int linkedMovementCount)
    {
        var currentBalance = AccountBalances.CurrentBalance(account.Type, account.OpeningBalance, cobrarTotal, pagarTotal);

        return new FinancialAccountResponse(
            account.Id,
            account.Name,
            account.Type,
            account.Currency,
            account.OpeningBalance,
            account.IsActive,
            account.CreatedAt,
            account.CreditLimit,
            account.StatementClosingDay,
            account.PaymentDueDay,
            currentBalance,
            AccountBalances.AvailableCredit(account.CreditLimit, currentBalance),
            linkedMovementCount);
    }

    private async Task<Dictionary<Guid, (decimal Cobrar, decimal Pagar, int Count)>> SumMovementsByAccountAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        var movements = await _store.ListMoneyMovementsAsync(ownerId, cancellationToken);

        return movements
            .Where(m => m.FinancialAccountId.HasValue)
            .GroupBy(m => m.FinancialAccountId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (
                    g.Where(m => m.Direction == MovementDirection.Cobrar).Sum(m => m.Amount.Amount),
                    g.Where(m => m.Direction == MovementDirection.Pagar).Sum(m => m.Amount.Amount),
                    g.Count()));
    }
}
