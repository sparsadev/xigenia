using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceSettlementSummaryTests
{
    private static readonly Guid ObligationPayableId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ObligationReceivableId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid AccountId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid AccountBId = new("cccccccc-cccc-cccc-cccc-cccccccccccd");
    private static readonly Guid OtherOwnerObligationId = new("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid CategoryIncomeId = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid CategoryExpenseId = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly DateOnly IssueDate = new(2026, 3, 1);
    private static readonly DateOnly MovementDate = new(2026, 3, 14);
    private static readonly DateOnly EarlyDate = new(2026, 3, 5);
    private static readonly DateOnly LateDate = new(2026, 3, 20);
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    [Fact]
    public async Task PayableSettlement_Valid_Unclassified_PersistsAndExcludedFromSummary()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(500m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 0m);
        store.SeedAccount(new FinancialAccount(AccountId, OwnerId, "Checking", FinancialAccountType.BankAccount, "USD", 0m, true, CreatedAt));
        var service = new FinanceService(store);

        var response = await service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Pagar,
            Amount: 120m,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: AccountId,
            Classification: MovementClassification.Unclassified), CancellationToken.None);

        Assert.Equal(ObligationPayableId, response.ObligationId);
        Assert.Equal(MovementClassification.Unclassified, response.Classification);
        var linked = Assert.Single(store.LinkedMovements);
        Assert.Equal(120m, linked.Amount.Amount);

        // Balance: account should reflect settlement
        store.SeedMovement(linked);
        var accounts = await service.ListFinancialAccountsAsync(OwnerId, CancellationToken.None);
        var account = Assert.Single(accounts);
        Assert.Equal(-120m, account.CurrentBalance);

        // Outstanding: should be 380
        var obligations = await service.ListObligationsAsync(OwnerId, CancellationToken.None);
        var payable = obligations.Single(o => o.Id == ObligationPayableId);
        Assert.Equal(380m, payable.OutstandingAmount);

        // Summary: settlement excluded from income/expenses but counted in MovementCount and UnclassifiedCount
        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(), CancellationToken.None);
        var usd = summary.Totals.SingleOrDefault(t => t.Currency == "USD");
        Assert.NotNull(usd);
        Assert.Equal(0m, usd!.Income);
        Assert.Equal(0m, usd.Expenses);
        Assert.Equal(0m, usd.Net);
        Assert.Equal(1, usd.MovementCount);
        Assert.Equal(0, summary.InternalTransferCount);
        Assert.Equal(1, summary.UnclassifiedCount);
    }

    [Fact]
    public async Task ReceivableSettlement_Valid_Unclassified_PersistsAndExcludedFromSummary()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationReceivableId, OwnerId, ObligationKind.PorCobrar, "Receivable", new Money(300m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationReceivableId, OwnerId, 0m);
        store.SeedAccount(new FinancialAccount(AccountId, OwnerId, "Checking", FinancialAccountType.BankAccount, "USD", 0m, true, CreatedAt));
        var service = new FinanceService(store);

        var response = await service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Cobrar,
            Amount: 80m,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationReceivableId,
            FinancialAccountId: AccountId,
            Classification: MovementClassification.Unclassified), CancellationToken.None);

        Assert.Equal(ObligationReceivableId, response.ObligationId);
        var linked = Assert.Single(store.LinkedMovements);
        Assert.Equal(80m, linked.Amount.Amount);

        store.SeedMovement(linked);
        var accounts = await service.ListFinancialAccountsAsync(OwnerId, CancellationToken.None);
        Assert.Equal(80m, Assert.Single(accounts).CurrentBalance);

        var obligations = await service.ListObligationsAsync(OwnerId, CancellationToken.None);
        Assert.Equal(220m, obligations.Single(o => o.Id == ObligationReceivableId).OutstandingAmount);

        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(), CancellationToken.None);
        var usd = Assert.Single(summary.Totals);
        Assert.Equal(0m, usd.Income);
        Assert.Equal(0m, usd.Expenses);
        Assert.Equal(1, usd.MovementCount);
        Assert.Equal(1, summary.UnclassifiedCount);
    }

    [Fact]
    public async Task CreateLinkedMovement_WithIncomeClassification_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(500m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 0m);
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Pagar,
            Amount: 50m,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: null,
            Classification: MovementClassification.Income), CancellationToken.None));

        Assert.Contains("Income", ex.Message);
        Assert.Empty(store.LinkedMovements);
        Assert.Empty(store.AddedMovements);
    }

    [Fact]
    public async Task CreateLinkedMovement_WithExpenseClassification_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationReceivableId, OwnerId, ObligationKind.PorCobrar, "Receivable", new Money(400m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationReceivableId, OwnerId, 0m);
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Cobrar,
            Amount: 30m,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationReceivableId,
            FinancialAccountId: null,
            Classification: MovementClassification.Expense), CancellationToken.None));

        Assert.Contains("Expense", ex.Message);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public void Domain_MoneyMovement_LinkedIncomeOrExpense_ThrowsArgumentException()
    {
        var ex1 = Assert.Throws<ArgumentException>(() => new MoneyMovement(
            Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(10m, "USD"), MovementDate, null, null, null, MovementClassification.Income));
        Assert.Equal("classification", ex1.ParamName);

        var ex2 = Assert.Throws<ArgumentException>(() => new MoneyMovement(
            Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(10m, "USD"), MovementDate, null, null, null, MovementClassification.Expense));
        Assert.Equal("classification", ex2.ParamName);
    }

    [Fact]
    public async Task HistoricalLinkedMovements_WithIncomeOrExpense_ExcludedFromSummary()
    {
        var store = new FakeFinanceStore();
        var historicalIncome = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(200m, "USD"), MovementDate, null, null, null, MovementClassification.Unclassified);
        var historicalExpense = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationReceivableId, MovementDirection.Cobrar, new Money(150m, "EUR"), MovementDate, null, null, null, MovementClassification.Unclassified);
        // Simulate dirty historical data bypassing domain validation
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(historicalIncome, MovementClassification.Income);
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(historicalExpense, MovementClassification.Expense);

        // Explicitly verify fixtures represent intended invalid state
        Assert.NotNull(historicalIncome.ObligationId);
        Assert.Equal(MovementClassification.Income, historicalIncome.Classification);
        Assert.NotNull(historicalExpense.ObligationId);
        Assert.Equal(MovementClassification.Expense, historicalExpense.Classification);
        // And would be rejected if created via constructor
        Assert.Throws<ArgumentException>(() => new MoneyMovement(Guid.NewGuid(), OwnerId, historicalIncome.ObligationId, MovementDirection.Pagar, new Money(10m, "USD"), MovementDate, null, null, null, MovementClassification.Income));

        store.SeedMovement(historicalIncome);
        store.SeedMovement(historicalExpense);
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(500m, "USD"), MovementDate, null, null, null, MovementClassification.Income));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Pagar, new Money(120m, "EUR"), MovementDate, null, null, null, MovementClassification.Expense));

        var service = new FinanceService(store);
        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(), CancellationToken.None);

        var usd = summary.Totals.Single(t => t.Currency == "USD");
        Assert.Equal(500m, usd.Income);
        Assert.Equal(0m, usd.Expenses);
        Assert.Equal(500m, usd.Net);
        Assert.Equal(2, usd.MovementCount); // 1 historical linked + 1 legitimate

        var eur = summary.Totals.Single(t => t.Currency == "EUR");
        Assert.Equal(0m, eur.Income);
        Assert.Equal(120m, eur.Expenses);
        Assert.Equal(-120m, eur.Net);
        Assert.Equal(2, eur.MovementCount);

        Assert.Equal(0, summary.InternalTransferCount);
        // UnclassifiedCount counts only Unclassified movements; historical linked Income/Expense are not Unclassified, so 0
        Assert.Equal(0, summary.UnclassifiedCount);
    }

    [Fact]
    public async Task UnlinkedLegitimateIncomeAndExpense_IncludedInSummary()
    {
        var store = new FakeFinanceStore();
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(1000m, "USD"), MovementDate, null, null, null, MovementClassification.Income));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Pagar, new Money(400m, "USD"), MovementDate, null, null, null, MovementClassification.Expense));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(300m, "EUR"), MovementDate, null, null, null, MovementClassification.Income));
        var service = new FinanceService(store);

        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(), CancellationToken.None);

        Assert.Equal(2, summary.Totals.Count);
        var usd = summary.Totals.Single(t => t.Currency == "USD");
        Assert.Equal(1000m, usd.Income);
        Assert.Equal(400m, usd.Expenses);
        Assert.Equal(600m, usd.Net);
        Assert.Equal(2, usd.MovementCount);
        var eur = summary.Totals.Single(t => t.Currency == "EUR");
        Assert.Equal(300m, eur.Income);
        Assert.Equal(0m, eur.Expenses);
        Assert.Equal(1, eur.MovementCount);
        Assert.Equal(0, summary.InternalTransferCount);
        Assert.Equal(0, summary.UnclassifiedCount);
    }

    [Fact]
    public async Task Transfers_RemainExcludedFromSummary()
    {
        var store = new FakeFinanceStore();
        var transferGroup = Guid.NewGuid();
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Pagar, new Money(200m, "USD"), MovementDate, null, AccountId, transferGroup, MovementClassification.InternalTransfer));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(200m, "USD"), MovementDate, null, AccountId, transferGroup, MovementClassification.InternalTransfer));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(500m, "USD"), MovementDate, null, null, null, MovementClassification.Income));
        var service = new FinanceService(store);

        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(), CancellationToken.None);

        var usd = Assert.Single(summary.Totals);
        Assert.Equal(500m, usd.Income);
        Assert.Equal(0m, usd.Expenses);
        Assert.Equal(2, summary.InternalTransferCount);
        Assert.Equal(3, usd.MovementCount);
        Assert.Equal(0, summary.UnclassifiedCount);
    }

    [Fact]
    public async Task Summary_PreservesCurrencySeparationAndOwnerIsolation()
    {
        var store = new FakeFinanceStore();
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(100m, "USD"), MovementDate, null, null, null, MovementClassification.Income));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(200m, "EUR"), MovementDate, null, null, null, MovementClassification.Income));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OtherOwnerId, null, MovementDirection.Cobrar, new Money(999m, "USD"), MovementDate, null, null, null, MovementClassification.Income));
        var otherIncomeLinked = new MoneyMovement(Guid.NewGuid(), OtherOwnerId, OtherOwnerObligationId, MovementDirection.Pagar, new Money(50m, "USD"), MovementDate, null, null, null, MovementClassification.Unclassified);
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(otherIncomeLinked, MovementClassification.Income);
        store.SeedMovement(otherIncomeLinked);

        var service = new FinanceService(store);
        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(), CancellationToken.None);

        Assert.Equal(2, summary.Totals.Count);
        Assert.Equal(100m, summary.Totals.Single(t => t.Currency == "USD").Income);
        Assert.Equal(200m, summary.Totals.Single(t => t.Currency == "EUR").Income);
        Assert.DoesNotContain(summary.Totals, t => t.Income == 999m);
    }

    // Finding 1: precedence – direction, currency, outstanding, missing-date before settlement classification

    [Fact]
    public async Task CreateLinkedMovement_WithBothDirectionMismatchAndIncomeClassification_ThrowsDirectionFirst()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(500m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 0m);
        var service = new FinanceService(store);

        // PorPagar expects Pagar, but request is Cobrar + Income (both invalid). Direction should win.
        var ex = await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Cobrar,
            Amount: 10m,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: null,
            Classification: MovementClassification.Income), CancellationToken.None));

        Assert.Contains("not compatible", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Income", ex.Message);
    }

    [Fact]
    public async Task CreateLinkedMovement_WithBothCurrencyMismatchAndIncomeClassification_ThrowsCurrencyFirst()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(500m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 0m);
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Pagar,
            Amount: 10m,
            Currency: "EUR",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: null,
            Classification: MovementClassification.Expense), CancellationToken.None));

        Assert.Contains("currency", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Expense", ex.Message);
    }

    [Fact]
    public async Task CreateLinkedMovement_WithBothOutstandingExceededAndIncomeClassification_ThrowsOutstandingFirst()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(100m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 95m);
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Pagar,
            Amount: 10m,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: null,
            Classification: MovementClassification.Income), CancellationToken.None));

        Assert.Contains("outstanding", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateLinkedMovement_WithBothZeroAmountAndIncomeClassification_ThrowsAmountFirst()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(500m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 0m);
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Pagar,
            Amount: 0m,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: null,
            Classification: MovementClassification.Income), CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateLinkedMovement_WithBothMinDateAndIncomeClassification_ThrowsDateFirst()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(500m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 0m);
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Pagar,
            Amount: 10m,
            Currency: "USD",
            MovementDate: DateOnly.MinValue,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: null,
            Classification: MovementClassification.Expense), CancellationToken.None));

        Assert.Equal("movementDate", ex.ParamName);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateLinkedMovement_WithUndefinedClassificationAndZeroAmount_ThrowsClassificationFirst()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(500m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 0m);
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Pagar,
            Amount: 0m,
            Currency: "USD",
            MovementDate: MovementDate,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: null,
            Classification: (MovementClassification)99), CancellationToken.None));

        Assert.Equal("classification", ex.ParamName);
        Assert.Null(ex.ActualValue);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateLinkedMovement_WithInternalTransferWithoutGroupAndMinDate_ThrowsClassificationFirst()
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationPayableId, OwnerId, ObligationKind.PorPagar, "Payable", new Money(500m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationPayableId, OwnerId, 0m);
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(
            Direction: MovementDirection.Pagar,
            Amount: 10m,
            Currency: "USD",
            MovementDate: DateOnly.MinValue,
            Note: null,
            ObligationId: ObligationPayableId,
            FinancialAccountId: null,
            Classification: MovementClassification.InternalTransfer), CancellationToken.None));

        Assert.Equal("classification", ex.ParamName);
        Assert.Contains("InternalTransfer", ex.Message);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public void Domain_MoneyMovement_MissingDateTakesPrecedenceOverSettlementClassification()
    {
        // DateOnly.MinValue is checked before settlement classification in domain ctor after reordering
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MoneyMovement(
            Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(10m, "USD"), DateOnly.MinValue, null, null, null, MovementClassification.Income));
        Assert.Equal("movementDate", ex.ParamName);
    }

    // Finding 2: generic persistence protection via AddMoneyMovementAsync

    [Fact]
    public async Task AddMoneyMovementAsync_WithLinkedIncome_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        var linkedIncome = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(10m, "USD"), MovementDate, null, null, null, MovementClassification.Unclassified);
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(linkedIncome, MovementClassification.Income);

        var ex = await Assert.ThrowsAsync<FinanceRuleException>(() => store.AddMoneyMovementAsync(linkedIncome, CancellationToken.None));
        Assert.Contains("Income", ex.Message);
        Assert.Empty(store.AddedMovements);
    }

    [Fact]
    public async Task AddMoneyMovementAsync_WithLinkedExpense_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        var linkedExpense = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(10m, "USD"), MovementDate, null, null, null, MovementClassification.Unclassified);
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(linkedExpense, MovementClassification.Expense);

        var ex = await Assert.ThrowsAsync<FinanceRuleException>(() => store.AddMoneyMovementAsync(linkedExpense, CancellationToken.None));
        Assert.Contains("Expense", ex.Message);
    }

    [Fact]
    public async Task AddMoneyMovementAsync_WithUnlinkedIncome_Succeeds()
    {
        var store = new FakeFinanceStore();
        var unlinked = new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(10m, "USD"), MovementDate, null, null, null, MovementClassification.Income);
        await store.AddMoneyMovementAsync(unlinked, CancellationToken.None);
        Assert.Single(store.AddedMovements);
    }

    // Finding 3: filtered summary cases

    [Fact]
    public async Task Summary_FilteredByDate_IncludesOnlyRangeAndStillExcludesLinked()
    {
        var store = new FakeFinanceStore();
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(100m, "USD"), EarlyDate, null, null, null, MovementClassification.Income));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Pagar, new Money(50m, "USD"), MovementDate, null, null, null, MovementClassification.Expense));
        var linked = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(200m, "USD"), EarlyDate, null, null, null, MovementClassification.Unclassified);
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(linked, MovementClassification.Income);
        store.SeedMovement(linked);
        var lateLinked = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(30m, "USD"), LateDate, null, null, null, MovementClassification.Unclassified);
        store.SeedMovement(lateLinked);

        var service = new FinanceService(store);
        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(From: MovementDate, To: LateDate), CancellationToken.None);

        var usd = Assert.Single(summary.Totals);
        Assert.Equal(0m, usd.Income); // early Income 100 outside range, linked Income 200 outside range
        Assert.Equal(50m, usd.Expenses);
        Assert.Equal(2, usd.MovementCount); // Expense 50 + lateLinked 30
        Assert.Equal(1, summary.UnclassifiedCount); // lateLinked
    }

    [Fact]
    public async Task Summary_FilteredByAccount_IncludesOnlyAccountAndStillExcludesLinked()
    {
        var store = new FakeFinanceStore();
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(300m, "USD"), MovementDate, null, AccountId, null, MovementClassification.Income));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(400m, "USD"), MovementDate, null, AccountBId, null, MovementClassification.Income));
        var linkedOnA = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(100m, "USD"), MovementDate, null, AccountId, null, MovementClassification.Unclassified);
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(linkedOnA, MovementClassification.Income);
        store.SeedMovement(linkedOnA);

        var service = new FinanceService(store);
        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(FinancialAccountId: AccountId), CancellationToken.None);

        var usd = Assert.Single(summary.Totals);
        Assert.Equal(300m, usd.Income); // 400 on B excluded by account filter, linked 100 excluded by ObligationId
        Assert.Equal(2, usd.MovementCount); // 300 + linked 100
        Assert.Equal(0, summary.UnclassifiedCount); // linked is Income, not Unclassified
    }

    [Fact]
    public async Task Summary_FilteredByCategory_IncludesOnlyCategoryAndStillExcludesLinked()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(new FinancialCategory(CategoryIncomeId, OwnerId, null, "Salary", MovementClassification.Income, true, CreatedAt));
        store.SeedCategory(new FinancialCategory(CategoryExpenseId, OwnerId, null, "Food", MovementClassification.Expense, true, CreatedAt));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(600m, "USD"), MovementDate, null, null, null, MovementClassification.Income, CategoryIncomeId));
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Pagar, new Money(200m, "USD"), MovementDate, null, null, null, MovementClassification.Expense, CategoryExpenseId));
        var linkedWithCategory = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(999m, "USD"), MovementDate, null, null, null, MovementClassification.Unclassified);
        // Linked cannot have category, so category-filtered linked will not appear anyway; test that category filter still excludes linked Income even if it had same currency
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(linkedWithCategory, MovementClassification.Income);
        // Need to bypass category check – set FinancialCategoryId via reflection as well to simulate dirty historical with category
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.FinancialCategoryId))!.SetValue(linkedWithCategory, CategoryIncomeId);
        store.SeedMovement(linkedWithCategory);

        var service = new FinanceService(store);
        var summaryIncome = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(FinancialCategoryId: CategoryIncomeId), CancellationToken.None);
        var usdIncome = Assert.Single(summaryIncome.Totals);
        Assert.Equal(600m, usdIncome.Income);
        Assert.Equal(2, usdIncome.MovementCount); // 600 + linked 999 (both filtered by category, but Income sum excludes linked)

        var summaryExpense = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(FinancialCategoryId: CategoryExpenseId), CancellationToken.None);
        var usdExpense = Assert.Single(summaryExpense.Totals);
        Assert.Equal(200m, usdExpense.Expenses);
    }

    [Fact]
    public async Task Summary_FilteredByClassification_Income_ExcludesLinkedIncome()
    {
        var store = new FakeFinanceStore();
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Cobrar, new Money(700m, "USD"), MovementDate, null, null, null, MovementClassification.Income));
        var linkedIncome = new MoneyMovement(Guid.NewGuid(), OwnerId, ObligationPayableId, MovementDirection.Pagar, new Money(999m, "USD"), MovementDate, null, null, null, MovementClassification.Unclassified);
        typeof(MoneyMovement).GetProperty(nameof(MoneyMovement.Classification))!.SetValue(linkedIncome, MovementClassification.Income);
        store.SeedMovement(linkedIncome);
        store.SeedMovement(new MoneyMovement(Guid.NewGuid(), OwnerId, null, MovementDirection.Pagar, new Money(100m, "USD"), MovementDate, null, null, null, MovementClassification.Expense));

        var service = new FinanceService(store);
        var summary = await service.SummarizeMoneyMovementsAsync(OwnerId, new MovementFilter(Classification: MovementClassification.Income), CancellationToken.None);

        var usd = Assert.Single(summary.Totals);
        Assert.Equal(700m, usd.Income);
        Assert.Equal(0m, usd.Expenses);
        Assert.Equal(2, usd.MovementCount); // 700 + 999 both match Classification=Income, but Income sum excludes linked
        Assert.Equal(0, summary.UnclassifiedCount);
        // Verify historical fixture state
        Assert.Equal(MovementClassification.Income, linkedIncome.Classification);
        Assert.NotNull(linkedIncome.ObligationId);
    }
}
