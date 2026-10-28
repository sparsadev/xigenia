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

        return Map(obligation);
    }

    public async Task<IReadOnlyList<ObligationResponse>> ListObligationsAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var obligations = await _store.ListObligationsAsync(ownerId, cancellationToken);

        return obligations.Select(Map).ToList();
    }

    private static ObligationResponse Map(Obligation obligation) => new(
        obligation.Id,
        obligation.Kind,
        obligation.Title,
        obligation.Total.Amount,
        obligation.Total.Currency,
        obligation.IssueDate,
        obligation.DueDate);
}
