using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceCancellationTests
{
    private static readonly Guid AccountId = new("54545454-5454-5454-5454-545454545454");
    private static readonly Guid ObligationId = new("55555555-5555-5555-5555-555555555555");
    private static readonly Guid CategoryId = new("56565656-5656-5656-5656-565656565656");
    private static readonly Guid MovementId = new("57575757-5757-5757-5757-575757575757");
    private const string OwnerId = "owner-1";

    [Fact]
    public async Task Summarize_ForwardsTokenToSingleStoreCall()
    {
        var store = new FakeFinanceStore();
        store.ConfiguredSummary = new MoneyMovementSummaryResponse(
            [new MoneyMovementCurrencySummary("USD", 100m, 20m, 80m, 2)],
            0,
            1);
        var service = new FinanceService(store);
        var filter = new MovementFilter(
            From: new DateOnly(2026, 3, 1),
            To: new DateOnly(2026, 3, 31),
            Classification: MovementClassification.Income);
        using var cts = new CancellationTokenSource();

        var response = await service.SummarizeMoneyMovementsAsync(OwnerId, filter, cts.Token);

        var call = Assert.Single(store.SummaryCalls);
        Assert.Equal(OwnerId, call.OwnerId);
        Assert.Equal(filter, call.Filter);
        Assert.Equal(cts.Token, call.CancellationToken);
        Assert.Single(response.Totals);
        Assert.Equal(new[] { "SummarizeMovements" }, store.OperationTrace);
        Assert.Empty(store.MovementListCalls);
        Assert.Empty(store.FilteredListCalls);
        Assert.Empty(store.CountCalls);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task ListObligations_ForwardsTokenToBothStoreCalls()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(
            ObligationId,
            OwnerId,
            ObligationKind.PorPagar,
            "Invoice",
            new Money(100m, "USD"),
            new DateOnly(2026, 3, 1)));
        store.SeedMovement(new MoneyMovement(
            MovementId,
            OwnerId,
            ObligationId,
            MovementDirection.Pagar,
            new Money(30m, "USD"),
            new DateOnly(2026, 3, 14)));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var responses = await service.ListObligationsAsync(OwnerId, cts.Token);

        var obligationCall = Assert.Single(store.ObligationListCalls);
        Assert.Equal(OwnerId, obligationCall.OwnerId);
        Assert.Equal(cts.Token, obligationCall.CancellationToken);
        var movementCall = Assert.Single(store.MovementListCalls);
        Assert.Equal(OwnerId, movementCall.OwnerId);
        Assert.Equal(cts.Token, movementCall.CancellationToken);
        var response = Assert.Single(responses);
        Assert.Equal(70m, response.OutstandingAmount);
        Assert.Equal(new[] { "ListObligations", "ListMovements" }, store.OperationTrace);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task CreateMovement_ForwardsTokenThroughLookupAndPersistence()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount());
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await service.CreateMoneyMovementAsync(
            OwnerId,
            new CreateMoneyMovementRequest(
                Direction: MovementDirection.Pagar,
                Amount: 100m,
                Currency: "USD",
                MovementDate: new DateOnly(2026, 3, 14),
                Note: null,
                ObligationId: null,
                FinancialAccountId: AccountId,
                Classification: MovementClassification.Unclassified,
                FinancialCategoryId: null),
            cts.Token);

        var lookup = Assert.Single(store.AccountLookupCalls);
        Assert.Equal(AccountId, lookup.AccountId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        var addCall = Assert.Single(store.MoneyMovementAddCalls);
        Assert.Equal(OwnerId, addCall.Movement.OwnerId);
        Assert.Equal(cts.Token, addCall.CancellationToken);
        var persisted = Assert.Single(store.AddedMovements);
        Assert.Equal(AccountId, persisted.FinancialAccountId);
        Assert.Equal(OwnerId, persisted.OwnerId);
        Assert.Equal(new[] { "GetAccount", "AddMovement" }, store.OperationTrace);
        Assert.Empty(store.ObligationLookups);
        Assert.Empty(store.CategoryLookups);
        Assert.Empty(store.MovementListCalls);
        Assert.Empty(store.SummaryCalls);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
        Assert.Empty(store.AddedCategories);
        Assert.Empty(store.UpdatedCategories);
    }

    [Fact]
    public async Task RenameCategory_ForwardsTokenThroughLookupAndUpdate()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(new FinancialCategory(
            CategoryId,
            OwnerId,
            null,
            "Category",
            MovementClassification.Expense,
            true,
            new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero)));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var response = await service.RenameFinancialCategoryAsync(
            OwnerId,
            CategoryId,
            new RenameFinancialCategoryRequest("Updated"),
            cts.Token);

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(CategoryId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        var updateCall = Assert.Single(store.CategoryUpdateCalls);
        Assert.Equal("Updated", updateCall.Category.Name);
        Assert.Equal(cts.Token, updateCall.CancellationToken);
        var updated = Assert.Single(store.UpdatedCategories);
        Assert.Equal(CategoryId, updated.Id);
        Assert.Equal(OwnerId, updated.OwnerId);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(new[] { "GetCategory", "UpdateCategory" }, store.OperationTrace);
        Assert.Equal("Updated", response.Name);
        Assert.Empty(store.CategoryListCalls);
        Assert.Empty(store.AccountLookups);
        Assert.Empty(store.MovementListCalls);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
        Assert.Empty(store.AddedCategories);
    }

    [Fact]
    public async Task ListObligations_WhenMovementListFails_PropagatesStoreException()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(
            ObligationId,
            OwnerId,
            ObligationKind.PorPagar,
            "Invoice",
            new Money(100m, "USD"),
            new DateOnly(2026, 3, 1)));
        store.ListMoneyMovementsError = new InvalidOperationException("Movement list unavailable.");
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListObligationsAsync(OwnerId, cts.Token));

        Assert.Same(store.ListMoneyMovementsError, ex);
        var obligationCall = Assert.Single(store.ObligationListCalls);
        Assert.Equal(OwnerId, obligationCall.OwnerId);
        Assert.Equal(cts.Token, obligationCall.CancellationToken);
        var movementCall = Assert.Single(store.MovementListCalls);
        Assert.Equal(OwnerId, movementCall.OwnerId);
        Assert.Equal(cts.Token, movementCall.CancellationToken);
        Assert.Equal(new[] { "ListObligations", "ListMovements" }, store.OperationTrace);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task CreateMovement_WhenPersistenceFails_PropagatesStoreException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount());
        store.MoneyMovementAddError = new InvalidOperationException("Movement persist unavailable.");
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            new CreateMoneyMovementRequest(
                Direction: MovementDirection.Pagar,
                Amount: 100m,
                Currency: "USD",
                MovementDate: new DateOnly(2026, 3, 14),
                Note: null,
                ObligationId: null,
                FinancialAccountId: AccountId,
                Classification: MovementClassification.Unclassified,
                FinancialCategoryId: null),
            cts.Token));

        Assert.Same(store.MoneyMovementAddError, ex);
        var lookup = Assert.Single(store.AccountLookupCalls);
        Assert.Equal(AccountId, lookup.AccountId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        var addCall = Assert.Single(store.MoneyMovementAddCalls);
        Assert.Equal(AccountId, addCall.Movement.FinancialAccountId);
        Assert.Equal(OwnerId, addCall.Movement.OwnerId);
        Assert.Equal(cts.Token, addCall.CancellationToken);
        Assert.Empty(store.AddedMovements);
        Assert.Equal(new[] { "GetAccount", "AddMovement" }, store.OperationTrace);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task ListAccounts_WhenMovementListCanceled_PropagatesCancellation()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount());
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();
        var cancellation = new OperationCanceledException(cts.Token);
        store.ListMoneyMovementsError = cancellation;
        cts.Cancel();

        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.ListFinancialAccountsAsync(OwnerId, cts.Token));

        Assert.Same(cancellation, ex);
        var accountCall = Assert.Single(store.AccountListCalls);
        Assert.Equal(OwnerId, accountCall.OwnerId);
        Assert.Equal(cts.Token, accountCall.CancellationToken);
        var movementCall = Assert.Single(store.MovementListCalls);
        Assert.Equal(OwnerId, movementCall.OwnerId);
        Assert.Equal(cts.Token, movementCall.CancellationToken);
        Assert.Equal(new[] { "ListAccounts", "ListMovements" }, store.OperationTrace);
        AssertNoWrites(store);
    }

    private static FinancialAccount CreateAccount() => new(
        AccountId,
        OwnerId,
        "Checking",
        FinancialAccountType.BankAccount,
        "USD",
        0m,
        true,
        new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero));

    private static void AssertNoWrites(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
        Assert.Empty(store.AddedCategories);
        Assert.Empty(store.UpdatedCategories);
    }
}
