using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceMovementTests
{
    private static readonly Guid AccountId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateOnly MovementDate = new(2026, 3, 14);
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    [Fact]
    public async Task CreateStandaloneMovement_PreservesNormalizedValues()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await service.CreateMoneyMovementAsync(OwnerId, CreateStandaloneRequest(), CancellationToken.None);

        var captured = Assert.Single(store.AddedMovements);
        Assert.Equal(123.45m, captured.Amount.Amount);
        Assert.Equal("USD", captured.Amount.Currency);
        Assert.Equal(MovementClassification.Income, captured.Classification);
        Assert.Equal(MovementDate, captured.MovementDate);
        Assert.Equal("Lunch", captured.Note);
        Assert.Equal(MovementDirection.Cobrar, captured.Direction);
        Assert.NotEqual(Guid.Empty, captured.Id);
        Assert.Null(captured.ObligationId);
        Assert.Null(captured.FinancialAccountId);
        Assert.Null(captured.TransferGroupId);
        Assert.Null(captured.FinancialCategoryId);
    }

    [Fact]
    public async Task CreateStandaloneMovement_PersistsExactlyOnceWithRequestingOwner()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await service.CreateMoneyMovementAsync(OwnerId, CreateStandaloneRequest(), CancellationToken.None);

        var captured = Assert.Single(store.AddedMovements);
        Assert.Equal(OwnerId, captured.OwnerId);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateStandaloneMovement_ResponseMatchesCapturedMovement()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var response = await service.CreateMoneyMovementAsync(OwnerId, CreateStandaloneRequest(), CancellationToken.None);

        var captured = Assert.Single(store.AddedMovements);
        Assert.Equal(captured.Id, response.Id);
        Assert.Equal(captured.Direction, response.Direction);
        Assert.Equal(captured.Amount.Amount, response.Amount);
        Assert.Equal(captured.Amount.Currency, response.Currency);
        Assert.Equal(captured.MovementDate, response.MovementDate);
        Assert.Equal(captured.Note, response.Note);
        Assert.Equal(captured.Classification, response.Classification);
        Assert.Equal(captured.ObligationId, response.ObligationId);
        Assert.Equal(captured.FinancialAccountId, response.FinancialAccountId);
        Assert.Equal(captured.TransferGroupId, response.TransferGroupId);
        Assert.Equal(captured.FinancialCategoryId, response.FinancialCategoryId);
    }

    [Fact]
    public async Task CreateMovement_WithMissingAccount_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateAccountLinkedRequest(),
            CancellationToken.None));

        var lookup = Assert.Single(store.AccountLookups);
        Assert.Equal(AccountId, lookup.AccountId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateMovement_WithOtherOwnerAccount_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(ownerId: OtherOwnerId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateAccountLinkedRequest(),
            CancellationToken.None));

        var lookup = Assert.Single(store.AccountLookups);
        Assert.Equal(AccountId, lookup.AccountId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateMovement_WithInactiveAccount_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(isActive: false));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateAccountLinkedRequest(),
            CancellationToken.None));

        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateMovement_WithCurrencyMismatch_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(currency: "EUR"));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateAccountLinkedRequest(),
            CancellationToken.None));

        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }

    private static CreateMoneyMovementRequest CreateStandaloneRequest() => new(
        Direction: MovementDirection.Cobrar,
        Amount: 123.45m,
        Currency: " usd ",
        MovementDate: MovementDate,
        Note: "  Lunch  ",
        ObligationId: null,
        FinancialAccountId: null,
        Classification: MovementClassification.Income,
        FinancialCategoryId: null);

    private static CreateMoneyMovementRequest CreateAccountLinkedRequest() => new(
        Direction: MovementDirection.Pagar,
        Amount: 100m,
        Currency: "USD",
        MovementDate: MovementDate,
        Note: null,
        ObligationId: null,
        FinancialAccountId: AccountId,
        Classification: MovementClassification.Unclassified,
        FinancialCategoryId: null);

    private static FinancialAccount CreateAccount(string ownerId = OwnerId, bool isActive = true, string currency = "USD") => new(
        AccountId,
        ownerId,
        "Checking",
        FinancialAccountType.BankAccount,
        currency,
        0m,
        isActive,
        CreatedAt);
}
