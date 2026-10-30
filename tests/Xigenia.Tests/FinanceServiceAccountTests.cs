using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceAccountTests
{
    private static readonly Guid AccountAId = new("34343434-3434-3434-3434-343434343434");
    private static readonly Guid AccountCId = new("35353535-3535-3535-3535-353535353535");
    private static readonly Guid OtherOwnerAccountId = new("36363636-3636-3636-3636-363636363636");
    private static readonly Guid InflowAId = new("37373737-3737-3737-3737-373737373737");
    private static readonly Guid OutflowAId = new("38383838-3838-3838-3838-383838383838");
    private static readonly Guid PurchaseCId = new("39393939-3939-3939-3939-393939393939");
    private static readonly Guid PaymentCId = new("40404040-4040-4040-4040-404040404040");
    private static readonly Guid UnlinkedMovementId = new("41414141-4141-4141-4141-414141414141");
    private static readonly Guid OtherOwnerMovementId = new("42424242-4242-4242-4242-424242424242");
    private static readonly Guid ObligationOneId = new("43434343-4343-4343-4343-434343434343");
    private static readonly Guid ObligationTwoId = new("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    [Theory]
    [MemberData(nameof(CreationCases))]
    public async Task CreateFinancialAccount_MapsRequestWithInitialBalances(
        FinancialAccountType type,
        decimal openingBalance,
        decimal? creditLimit,
        decimal expectedBalance,
        decimal? expectedAvailableCredit)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        int? closingDay = type == FinancialAccountType.CreditCard ? 5 : null;
        int? dueDay = type == FinancialAccountType.CreditCard ? 25 : null;

        var response = await service.CreateFinancialAccountAsync(
            OwnerId,
            new CreateFinancialAccountRequest(
                Name: "  Checking  ",
                Type: type,
                Currency: " usd ",
                OpeningBalance: openingBalance,
                CreditLimit: creditLimit,
                StatementClosingDay: closingDay,
                PaymentDueDay: dueDay),
            CancellationToken.None);

        var captured = Assert.Single(store.AddedAccounts);
        Assert.Equal(OwnerId, captured.OwnerId);
        Assert.NotEqual(Guid.Empty, captured.Id);
        Assert.Equal("Checking", captured.Name);
        Assert.Equal(type, captured.Type);
        Assert.Equal("USD", captured.Currency);
        Assert.Equal(openingBalance, captured.OpeningBalance);
        Assert.True(captured.IsActive);
        Assert.Equal(creditLimit, captured.CreditLimit);
        Assert.Equal(closingDay, captured.StatementClosingDay);
        Assert.Equal(dueDay, captured.PaymentDueDay);
        Assert.Equal(captured.Id, response.Id);
        Assert.Equal(captured.Name, response.Name);
        Assert.Equal(captured.Type, response.Type);
        Assert.Equal(captured.Currency, response.Currency);
        Assert.Equal(captured.OpeningBalance, response.OpeningBalance);
        Assert.Equal(captured.IsActive, response.IsActive);
        Assert.Equal(captured.CreatedAt, response.CreatedAt);
        Assert.Equal(captured.CreditLimit, response.CreditLimit);
        Assert.Equal(captured.StatementClosingDay, response.StatementClosingDay);
        Assert.Equal(captured.PaymentDueDay, response.PaymentDueDay);
        Assert.Equal(expectedBalance, response.CurrentBalance);
        Assert.Equal(expectedAvailableCredit, response.AvailableCredit);
        Assert.Equal(0, response.LinkedMovementCount);
        AssertNoMovementPersistence(store);
        Assert.Empty(store.AddedObligations);
    }

    [Fact]
    public async Task ListFinancialAccounts_ComputesBalancesCountsAndCredit()
    {
        var store = new FakeFinanceStore();
        var asset = CreateAccount(AccountAId, "Checking", FinancialAccountType.BankAccount, 100m);
        var card = CreateAccount(
            AccountCId,
            "Card",
            FinancialAccountType.CreditCard,
            50m,
            creditLimit: 1000m,
            statementClosingDay: 5,
            paymentDueDay: 25);
        store.SeedAccount(asset);
        store.SeedAccount(card);
        store.SeedAccount(CreateAccount(OtherOwnerAccountId, "Other", ownerId: OtherOwnerId));
        store.SeedMovement(CreateMovement(InflowAId, AccountAId, MovementDirection.Cobrar, 20m, ObligationOneId));
        store.SeedMovement(CreateMovement(OutflowAId, AccountAId, MovementDirection.Pagar, 30m, null));
        store.SeedMovement(CreateMovement(PurchaseCId, AccountCId, MovementDirection.Pagar, 200m, null));
        store.SeedMovement(CreateMovement(PaymentCId, AccountCId, MovementDirection.Cobrar, 60m, ObligationTwoId));
        store.SeedMovement(CreateMovement(UnlinkedMovementId, null, MovementDirection.Pagar, 999m, null));
        store.SeedMovement(CreateMovement(OtherOwnerMovementId, AccountAId, MovementDirection.Cobrar, 1000m, null, OtherOwnerId));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var responses = await service.ListFinancialAccountsAsync(OwnerId, cts.Token);

        var accountCall = Assert.Single(store.AccountListCalls);
        Assert.Equal(OwnerId, accountCall.OwnerId);
        Assert.Equal(cts.Token, accountCall.CancellationToken);
        var movementCall = Assert.Single(store.MovementListCalls);
        Assert.Equal(OwnerId, movementCall.OwnerId);
        Assert.Equal(cts.Token, movementCall.CancellationToken);
        Assert.Equal(2, responses.Count);
        AssertAccountResponseMatches(responses.Single(r => r.Id == AccountAId), asset, 90m, null, 2);
        AssertAccountResponseMatches(responses.Single(r => r.Id == AccountCId), card, 190m, 810m, 2);
        Assert.DoesNotContain(responses, r => r.Id == OtherOwnerAccountId);
        AssertNoMovementPersistence(store);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
    }

    [Fact]
    public async Task GetFinancialAccount_ReturnsMappedAccountWithSums()
    {
        var store = new FakeFinanceStore();
        var asset = CreateAccount(AccountAId, "Checking", FinancialAccountType.BankAccount, 100m);
        store.SeedAccount(asset);
        store.SeedMovement(CreateMovement(InflowAId, AccountAId, MovementDirection.Cobrar, 20m, ObligationOneId));
        store.SeedMovement(CreateMovement(OutflowAId, AccountAId, MovementDirection.Pagar, 30m, null));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var response = await service.GetFinancialAccountAsync(OwnerId, AccountAId, cts.Token);

        var lookup = Assert.Single(store.AccountLookups);
        Assert.Equal(AccountAId, lookup.AccountId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        var lookupCall = Assert.Single(store.AccountLookupCalls);
        Assert.Equal(cts.Token, lookupCall.CancellationToken);
        var movementCall = Assert.Single(store.MovementListCalls);
        Assert.Equal(OwnerId, movementCall.OwnerId);
        Assert.Equal(cts.Token, movementCall.CancellationToken);
        Assert.NotNull(response);
        AssertAccountResponseMatches(response, asset, 90m, null, 2);
        AssertNoMovementPersistence(store);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetFinancialAccount_WithInaccessibleAccount_ReturnsNullWithoutQueryingMovements(bool seededElsewhere)
    {
        var store = new FakeFinanceStore();
        if (seededElsewhere)
        {
            store.SeedAccount(CreateAccount(AccountAId, "Checking", ownerId: OtherOwnerId));
        }

        var service = new FinanceService(store);

        var response = await service.GetFinancialAccountAsync(OwnerId, AccountAId, CancellationToken.None);

        Assert.Null(response);
        var lookup = Assert.Single(store.AccountLookups);
        Assert.Equal(AccountAId, lookup.AccountId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Empty(store.MovementListCalls);
        AssertNoMovementPersistence(store);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
    }

    public static TheoryData<FinancialAccountType, decimal, decimal?, decimal, decimal?> CreationCases => new()
    {
        { FinancialAccountType.BankAccount, 100m, null, 100m, null },
        { FinancialAccountType.CreditCard, 50m, 1000m, 50m, 950m },
    };

    private static FinancialAccount CreateAccount(
        Guid id,
        string name,
        FinancialAccountType type = FinancialAccountType.BankAccount,
        decimal openingBalance = 0m,
        string ownerId = OwnerId,
        decimal? creditLimit = null,
        int? statementClosingDay = null,
        int? paymentDueDay = null) => new(
            id,
            ownerId,
            name,
            type,
            "USD",
            openingBalance,
            true,
            CreatedAt,
            creditLimit,
            statementClosingDay,
            paymentDueDay);

    private static MoneyMovement CreateMovement(
        Guid id,
        Guid? accountId,
        MovementDirection direction,
        decimal amount,
        Guid? obligationId,
        string ownerId = OwnerId) => new(
            id,
            ownerId,
            obligationId,
            direction,
            new Money(amount, "USD"),
            new DateOnly(2026, 3, 14),
            null,
            accountId);

    private static void AssertAccountResponseMatches(
        FinancialAccountResponse response,
        FinancialAccount account,
        decimal expectedBalance,
        decimal? expectedAvailableCredit,
        int expectedCount)
    {
        Assert.Equal(account.Id, response.Id);
        Assert.Equal(account.Name, response.Name);
        Assert.Equal(account.Type, response.Type);
        Assert.Equal(account.Currency, response.Currency);
        Assert.Equal(account.OpeningBalance, response.OpeningBalance);
        Assert.Equal(account.IsActive, response.IsActive);
        Assert.Equal(account.CreatedAt, response.CreatedAt);
        Assert.Equal(account.CreditLimit, response.CreditLimit);
        Assert.Equal(account.StatementClosingDay, response.StatementClosingDay);
        Assert.Equal(account.PaymentDueDay, response.PaymentDueDay);
        Assert.Equal(expectedBalance, response.CurrentBalance);
        Assert.Equal(expectedAvailableCredit, response.AvailableCredit);
        Assert.Equal(expectedCount, response.LinkedMovementCount);
    }

    private static void AssertNoMovementPersistence(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }
}
