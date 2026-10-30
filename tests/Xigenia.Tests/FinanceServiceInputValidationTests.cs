using Xigenia.Application.Finance;
using Xigenia.Domain;
using Xunit.Abstractions;

namespace Xigenia.Tests;

public sealed class FinanceServiceInputValidationTests
{
    private static readonly Guid ResourceId = new("12121212-1212-1212-1212-121212121212");
    private static readonly Guid SourceAccountId = new("13131313-1313-1313-1313-131313131313");
    private static readonly Guid DestinationAccountId = new("14141414-1414-1414-1414-141414141414");
    private static readonly DateOnly MovementDate = new(2026, 3, 14);
    private const string OwnerId = "owner-1";

    private readonly ITestOutputHelper _output;

    public FinanceServiceInputValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(InvalidOwnerCases))]
    public async Task Operations_WithInvalidOwner_ThrowArgumentException(
        string operation,
        string? ownerId,
        Func<FinanceService, string?, CancellationToken, Task> invoke)
    {
        _output.WriteLine($"Operation: {operation}");

        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        if (ownerId is null)
        {
            var nullException = await Assert.ThrowsAsync<ArgumentNullException>(() => invoke(service, null, CancellationToken.None));
            Assert.Equal("ownerId", nullException.ParamName);
        }
        else
        {
            var blankException = await Assert.ThrowsAsync<ArgumentException>(() => invoke(service, ownerId, CancellationToken.None));
            Assert.Equal("ownerId", blankException.ParamName);
            Assert.IsType<ArgumentException>(blankException);
        }

        AssertNoStoreCalls(store);
    }

    [Theory]
    [MemberData(nameof(NullRequestCases))]
    public async Task Operations_WithNullRequest_ThrowArgumentNullException(
        string operation,
        string paramName,
        Func<FinanceService, CancellationToken, Task> invoke)
    {
        _output.WriteLine($"Operation: {operation}");

        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => invoke(service, CancellationToken.None));

        Assert.Equal(paramName, ex.ParamName);
        AssertNoStoreCalls(store);
    }

    public static TheoryData<string, string?, Func<FinanceService, string?, CancellationToken, Task>> InvalidOwnerCases
    {
        get
        {
            var data = new TheoryData<string, string?, Func<FinanceService, string?, CancellationToken, Task>>();
            foreach (var (operation, invoke) in OwnerValidationOperations())
            {
                data.Add(operation, null, invoke);
                data.Add(operation, string.Empty, invoke);
                data.Add(operation, "   ", invoke);
            }

            return data;
        }
    }

    public static TheoryData<string, string, Func<FinanceService, CancellationToken, Task>> NullRequestCases => new()
    {
        { "CreateObligationAsync", "request", static (s, ct) => s.CreateObligationAsync(OwnerId, null!, ct) },
        { "CreateMoneyMovementAsync", "request", static (s, ct) => s.CreateMoneyMovementAsync(OwnerId, null!, ct) },
        { "ListMoneyMovementsAsync filter", "filter", static (s, ct) => s.ListMoneyMovementsAsync(OwnerId, null!, new MovementPaging(), new MovementSorting(), ct) },
        { "ListMoneyMovementsAsync paging", "paging", static (s, ct) => s.ListMoneyMovementsAsync(OwnerId, new MovementFilter(), null!, new MovementSorting(), ct) },
        { "ListMoneyMovementsAsync sorting", "sorting", static (s, ct) => s.ListMoneyMovementsAsync(OwnerId, new MovementFilter(), new MovementPaging(), null!, ct) },
        { "SummarizeMoneyMovementsAsync", "filter", static (s, ct) => s.SummarizeMoneyMovementsAsync(OwnerId, null!, ct) },
        { "CreateFinancialAccountAsync", "request", static (s, ct) => s.CreateFinancialAccountAsync(OwnerId, null!, ct) },
        { "CreateTransferAsync", "request", static (s, ct) => s.CreateTransferAsync(OwnerId, null!, ct) },
        { "CreateFinancialCategoryAsync", "request", static (s, ct) => s.CreateFinancialCategoryAsync(OwnerId, null!, ct) },
        { "RenameFinancialCategoryAsync", "request", static (s, ct) => s.RenameFinancialCategoryAsync(OwnerId, ResourceId, null!, ct) },
        { "SetFinancialCategoryActivationAsync", "request", static (s, ct) => s.SetFinancialCategoryActivationAsync(OwnerId, ResourceId, null!, ct) },
    };

    private static (string Operation, Func<FinanceService, string?, CancellationToken, Task> Invoke)[] OwnerValidationOperations() =>
    [
        ("CreateObligationAsync", static (s, o, ct) => s.CreateObligationAsync(o!, ValidObligationRequest(), ct)),
        ("ListObligationsAsync", static (s, o, ct) => s.ListObligationsAsync(o!, ct)),
        ("CreateMoneyMovementAsync", static (s, o, ct) => s.CreateMoneyMovementAsync(o!, ValidMovementRequest(), ct)),
        ("ListMoneyMovementsAsync unfiltered", static (s, o, ct) => s.ListMoneyMovementsAsync(o!, ct)),
        ("ListMoneyMovementsAsync filtered", static (s, o, ct) => s.ListMoneyMovementsAsync(o!, new MovementFilter(), new MovementPaging(), new MovementSorting(), ct)),
        ("SummarizeMoneyMovementsAsync", static (s, o, ct) => s.SummarizeMoneyMovementsAsync(o!, new MovementFilter(), ct)),
        ("CreateFinancialAccountAsync", static (s, o, ct) => s.CreateFinancialAccountAsync(o!, ValidAccountRequest(), ct)),
        ("ListFinancialAccountsAsync", static (s, o, ct) => s.ListFinancialAccountsAsync(o!, ct)),
        ("GetFinancialAccountAsync", static (s, o, ct) => s.GetFinancialAccountAsync(o!, ResourceId, ct)),
        ("CreateTransferAsync", static (s, o, ct) => s.CreateTransferAsync(o!, ValidTransferRequest(), ct)),
        ("ListFinancialCategoriesAsync", static (s, o, ct) => s.ListFinancialCategoriesAsync(o!, ct)),
        ("GetFinancialCategoryAsync", static (s, o, ct) => s.GetFinancialCategoryAsync(o!, ResourceId, ct)),
        ("CreateFinancialCategoryAsync", static (s, o, ct) => s.CreateFinancialCategoryAsync(o!, ValidCategoryRequest(), ct)),
        ("RenameFinancialCategoryAsync", static (s, o, ct) => s.RenameFinancialCategoryAsync(o!, ResourceId, ValidRenameRequest(), ct)),
        ("SetFinancialCategoryActivationAsync", static (s, o, ct) => s.SetFinancialCategoryActivationAsync(o!, ResourceId, ValidActivationRequest(), ct)),
    ];

    private static CreateObligationRequest ValidObligationRequest() => new(
        Kind: ObligationKind.PorPagar,
        Title: "Invoice",
        Amount: 250m,
        Currency: "USD",
        IssueDate: MovementDate,
        DueDate: null);

    private static CreateMoneyMovementRequest ValidMovementRequest() => new(
        Direction: MovementDirection.Cobrar,
        Amount: 100m,
        Currency: "USD",
        MovementDate: MovementDate,
        Note: null,
        ObligationId: null,
        FinancialAccountId: null,
        Classification: MovementClassification.Unclassified,
        FinancialCategoryId: null);

    private static CreateFinancialAccountRequest ValidAccountRequest() => new(
        Name: "Checking",
        Type: FinancialAccountType.BankAccount,
        Currency: "USD",
        OpeningBalance: 0m,
        CreditLimit: null,
        StatementClosingDay: null,
        PaymentDueDay: null);

    private static CreateTransferRequest ValidTransferRequest() => new(
        SourceAccountId: SourceAccountId,
        DestinationAccountId: DestinationAccountId,
        Amount: 100m,
        Currency: "USD",
        MovementDate: MovementDate,
        Note: null);

    private static CreateFinancialCategoryRequest ValidCategoryRequest() => new(
        Name: "Groceries",
        Classification: MovementClassification.Expense,
        ParentCategoryId: null);

    private static RenameFinancialCategoryRequest ValidRenameRequest() => new(Name: "Updated");

    private static SetFinancialCategoryActivationRequest ValidActivationRequest() => new(IsActive: false);

    private static void AssertNoStoreCalls(FakeFinanceStore store)
    {
        Assert.Empty(store.AccountLookups);
        Assert.Empty(store.CategoryLookups);
        Assert.Empty(store.ObligationLookups);
        Assert.Empty(store.SettlementLookups);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }
}
