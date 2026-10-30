using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceObligationMovementTests
{
    private static readonly Guid ObligationId = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly DateOnly IssueDate = new(2026, 3, 1);
    private static readonly DateOnly MovementDate = new(2026, 3, 14);
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    [Fact]
    public async Task CreateMovement_WithMissingObligation_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateSettlementRequest(ObligationId),
            CancellationToken.None));

        var lookup = Assert.Single(store.ObligationLookups);
        Assert.Equal(ObligationId, lookup.ObligationId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateMovement_WithOtherOwnerObligation_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(CreateObligation(ownerId: OtherOwnerId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateSettlementRequest(ObligationId),
            CancellationToken.None));

        var lookup = Assert.Single(store.ObligationLookups);
        Assert.Equal(ObligationId, lookup.ObligationId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        AssertNoPersistence(store);
    }

    [Theory]
    [MemberData(nameof(WithinBalanceAmounts))]
    public async Task CreateSettlement_WithinBalance_UsesLinkedPathExactlyOnce(decimal amount)
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(CreateObligation(total: 1m));
        store.SeedSettlementTotal(ObligationId, OwnerId, 0.7m);
        var service = new FinanceService(store);

        var response = await service.CreateMoneyMovementAsync(
            OwnerId,
            CreateSettlementRequest(ObligationId, amount),
            CancellationToken.None);

        var captured = Assert.Single(store.LinkedMovements);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Equal(ObligationId, captured.ObligationId);
        Assert.Equal(amount, captured.Amount.Amount);
        Assert.Equal(OwnerId, captured.OwnerId);
        AssertMovementResponseMatches(response, captured);
        var obligationLookup = Assert.Single(store.ObligationLookups);
        Assert.Equal(ObligationId, obligationLookup.ObligationId);
        Assert.Equal(OwnerId, obligationLookup.OwnerId);
        var settlementLookup = Assert.Single(store.SettlementLookups);
        Assert.Equal(ObligationId, settlementLookup.ObligationId);
        Assert.Equal(OwnerId, settlementLookup.OwnerId);
    }

    [Fact]
    public async Task CreateSettlement_AboveBalance_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(CreateObligation(total: 1m));
        store.SeedSettlementTotal(ObligationId, OwnerId, 0.7m);
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateSettlementRequest(ObligationId, 0.31m),
            CancellationToken.None));

        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateSettlement_WithKindDirectionMismatch_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(CreateObligation(kind: ObligationKind.PorPagar));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateSettlementRequest(ObligationId, direction: MovementDirection.Cobrar),
            CancellationToken.None));

        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateSettlement_WithCurrencyMismatch_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(CreateObligation(currency: "EUR"));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateSettlementRequest(ObligationId),
            CancellationToken.None));

        AssertNoPersistence(store);
    }

    public static TheoryData<decimal> WithinBalanceAmounts => new()
    {
        0.29m,
        0.30m,
    };

    private static void AssertMovementResponseMatches(MoneyMovementResponse response, MoneyMovement captured)
    {
        Assert.Equal(captured.Id, response.Id);
        Assert.Equal(captured.ObligationId, response.ObligationId);
        Assert.Equal(captured.FinancialAccountId, response.FinancialAccountId);
        Assert.Equal(captured.TransferGroupId, response.TransferGroupId);
        Assert.Equal(captured.Direction, response.Direction);
        Assert.Equal(captured.Amount.Amount, response.Amount);
        Assert.Equal(captured.Amount.Currency, response.Currency);
        Assert.Equal(captured.MovementDate, response.MovementDate);
        Assert.Equal(captured.Note, response.Note);
        Assert.Equal(captured.Classification, response.Classification);
        Assert.Equal(captured.FinancialCategoryId, response.FinancialCategoryId);
    }

    private static Obligation CreateObligation(
        Guid? id = null,
        string ownerId = OwnerId,
        ObligationKind kind = ObligationKind.PorPagar,
        decimal total = 250m,
        string currency = "USD") => new(
            id ?? ObligationId,
            ownerId,
            kind,
            "Invoice",
            new Money(total, currency),
            IssueDate);

    private static CreateMoneyMovementRequest CreateSettlementRequest(
        Guid obligationId,
        decimal amount = 100m,
        MovementDirection direction = MovementDirection.Pagar) => new(
            Direction: direction,
            Amount: amount,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: obligationId,
            FinancialAccountId: null,
            Classification: MovementClassification.Unclassified,
            FinancialCategoryId: null);

    private static void AssertNoPersistence(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }
}
