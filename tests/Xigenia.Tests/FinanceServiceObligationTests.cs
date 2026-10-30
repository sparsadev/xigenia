using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceObligationTests
{
    private static readonly Guid ObligationAId = new("26262626-2626-2626-2626-262626262626");
    private static readonly Guid ObligationBId = new("27272727-2727-2727-2727-272727272727");
    private static readonly Guid OtherOwnerObligationId = new("28282828-2828-2828-2828-282828282828");
    private static readonly Guid SettlementOneId = new("29292929-2929-2929-2929-292929292929");
    private static readonly Guid SettlementTwoId = new("30303030-3030-3030-3030-303030303030");
    private static readonly Guid SettlementThreeId = new("31313131-3131-3131-3131-313131313131");
    private static readonly Guid UnlinkedMovementId = new("32323232-3232-3232-3232-323232323232");
    private static readonly Guid OtherOwnerMovementId = new("33333333-3333-3333-3333-333333333333");
    private static readonly DateOnly IssueDate = new(2026, 3, 1);
    private static readonly DateOnly DueDate = new(2026, 4, 1);
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    [Fact]
    public async Task CreateObligation_MapsNormalizedValuesWithFullOutstanding()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var response = await service.CreateObligationAsync(
            OwnerId,
            new CreateObligationRequest(
                Kind: ObligationKind.PorPagar,
                Title: "  Power bill  ",
                Amount: 250.75m,
                Currency: " usd ",
                IssueDate: IssueDate,
                DueDate: DueDate),
            CancellationToken.None);

        var captured = Assert.Single(store.AddedObligations);
        Assert.Equal(OwnerId, captured.OwnerId);
        Assert.NotEqual(Guid.Empty, captured.Id);
        Assert.Equal(ObligationKind.PorPagar, captured.Kind);
        Assert.Equal("Power bill", captured.Title);
        Assert.Equal(250.75m, captured.Total.Amount);
        Assert.Equal("USD", captured.Total.Currency);
        Assert.Equal(IssueDate, captured.IssueDate);
        Assert.Equal(DueDate, captured.DueDate);
        Assert.Equal(captured.Id, response.Id);
        Assert.Equal(captured.Kind, response.Kind);
        Assert.Equal(captured.Title, response.Title);
        Assert.Equal(captured.Total.Amount, response.Amount);
        Assert.Equal(captured.Total.Currency, response.Currency);
        Assert.Equal(captured.IssueDate, response.IssueDate);
        Assert.Equal(captured.DueDate, response.DueDate);
        Assert.Equal(250.75m, response.OutstandingAmount);
        AssertNoMovementPersistence(store);
        Assert.Empty(store.AddedAccounts);
    }

    [Fact]
    public async Task CreateObligation_AcceptsNullDueDate()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var response = await service.CreateObligationAsync(
            OwnerId,
            new CreateObligationRequest(
                Kind: ObligationKind.PorCobrar,
                Title: "Invoice",
                Amount: 100m,
                Currency: "USD",
                IssueDate: IssueDate,
                DueDate: null),
            CancellationToken.None);

        var captured = Assert.Single(store.AddedObligations);
        Assert.Null(captured.DueDate);
        Assert.Null(response.DueDate);
        Assert.Equal(100m, response.OutstandingAmount);
        AssertNoMovementPersistence(store);
        Assert.Empty(store.AddedAccounts);
    }

    [Fact]
    public async Task ListObligations_ComputesOutstandingFromLinkedMovements()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(CreateObligation(ObligationAId, total: 100m));
        store.SeedObligation(CreateObligation(ObligationBId, kind: ObligationKind.PorCobrar, total: 50m));
        store.SeedObligation(CreateObligation(OtherOwnerObligationId, ownerId: OtherOwnerId));
        // Linked movements settle by amount regardless of cash direction.
        store.SeedMovement(CreateMovement(SettlementOneId, ObligationAId, MovementDirection.Pagar, 30m));
        store.SeedMovement(CreateMovement(SettlementTwoId, ObligationAId, MovementDirection.Pagar, 20m));
        store.SeedMovement(CreateMovement(SettlementThreeId, ObligationAId, MovementDirection.Cobrar, 10m));
        store.SeedMovement(CreateMovement(UnlinkedMovementId, null, MovementDirection.Pagar, 999m));
        store.SeedMovement(CreateMovement(OtherOwnerMovementId, ObligationAId, MovementDirection.Pagar, 1000m, OtherOwnerId));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var responses = await service.ListObligationsAsync(OwnerId, cts.Token);

        var obligationCall = Assert.Single(store.ObligationListCalls);
        Assert.Equal(OwnerId, obligationCall.OwnerId);
        Assert.Equal(cts.Token, obligationCall.CancellationToken);
        var movementCall = Assert.Single(store.MovementListCalls);
        Assert.Equal(OwnerId, movementCall.OwnerId);
        Assert.Equal(cts.Token, movementCall.CancellationToken);
        Assert.Equal(2, responses.Count);
        var settled = responses.Single(r => r.Id == ObligationAId);
        Assert.Equal(ObligationAId, settled.Id);
        Assert.Equal(ObligationKind.PorPagar, settled.Kind);
        Assert.Equal("Invoice", settled.Title);
        Assert.Equal(100m, settled.Amount);
        Assert.Equal("USD", settled.Currency);
        Assert.Equal(IssueDate, settled.IssueDate);
        Assert.Equal(DueDate, settled.DueDate);
        Assert.Equal(40m, settled.OutstandingAmount);
        var untouched = responses.Single(r => r.Id == ObligationBId);
        Assert.Equal(50m, untouched.OutstandingAmount);
        Assert.Equal(ObligationKind.PorCobrar, untouched.Kind);
        Assert.DoesNotContain(responses, r => r.Id == OtherOwnerObligationId);
        AssertNoMovementPersistence(store);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
    }

    private static Obligation CreateObligation(
        Guid id,
        string ownerId = OwnerId,
        ObligationKind kind = ObligationKind.PorPagar,
        decimal total = 250m) => new(
            id,
            ownerId,
            kind,
            "Invoice",
            new Money(total, "USD"),
            IssueDate,
            DueDate);

    private static MoneyMovement CreateMovement(
        Guid id,
        Guid? obligationId,
        MovementDirection direction,
        decimal amount,
        string ownerId = OwnerId) => new(
            id,
            ownerId,
            obligationId,
            direction,
            new Money(amount, "USD"),
            new DateOnly(2026, 3, 14));

    private static void AssertNoMovementPersistence(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }
}
