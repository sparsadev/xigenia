using Xigenia.Application.Finance;
using Xigenia.Domain;
using Xunit.Abstractions;

namespace Xigenia.Tests;

public sealed class FinanceServiceMovementQueryValidationTests
{
    private const string OwnerId = "owner-1";

    private readonly ITestOutputHelper _output;

    public FinanceServiceMovementQueryValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(FilterValidationOperations))]
    public async Task MovementQuery_WithFromGreaterThanTo_ThrowsArgumentException(
        string operation,
        Func<FinanceService, MovementFilter, CancellationToken, Task> invoke)
    {
        _output.WriteLine($"Operation: {operation}");

        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        var filter = new MovementFilter(From: new DateOnly(2026, 3, 14), To: new DateOnly(2026, 3, 13));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => invoke(service, filter, CancellationToken.None));

        Assert.Equal("filter", ex.ParamName);
        AssertNoStoreCalls(store);
    }

    [Theory]
    [MemberData(nameof(ClassificationValidationCases))]
    public async Task MovementQuery_WithUndefinedClassification_ThrowsArgumentOutOfRangeException(
        string operation,
        int classification,
        Func<FinanceService, MovementFilter, CancellationToken, Task> invoke)
    {
        _output.WriteLine($"Operation: {operation}");

        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        var filter = new MovementFilter(Classification: (MovementClassification)classification);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => invoke(service, filter, CancellationToken.None));

        Assert.Equal("filter", ex.ParamName);
        AssertNoStoreCalls(store);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FilteredList_WithIncompletePagingPair_ThrowsArgumentException(bool pageOnly)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        var paging = pageOnly ? new MovementPaging(Page: 1) : new MovementPaging(PageSize: 10);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ListMoneyMovementsAsync(
            OwnerId,
            new MovementFilter(),
            paging,
            new MovementSorting(),
            CancellationToken.None));

        Assert.Equal("paging", ex.ParamName);
        AssertNoStoreCalls(store);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task FilteredList_WithInvalidPage_ThrowsArgumentOutOfRangeException(int page)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ListMoneyMovementsAsync(
            OwnerId,
            new MovementFilter(),
            new MovementPaging(Page: page, PageSize: 10),
            new MovementSorting(),
            CancellationToken.None));

        Assert.Equal("paging", ex.ParamName);
        AssertNoStoreCalls(store);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task FilteredList_WithInvalidPageSize_ThrowsArgumentOutOfRangeException(int pageSize)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ListMoneyMovementsAsync(
            OwnerId,
            new MovementFilter(),
            new MovementPaging(Page: 1, PageSize: pageSize),
            new MovementSorting(),
            CancellationToken.None));

        Assert.Equal("paging", ex.ParamName);
        AssertNoStoreCalls(store);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task FilteredList_WithUnsupportedSortField_ThrowsArgumentOutOfRangeException(int sort)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ListMoneyMovementsAsync(
            OwnerId,
            new MovementFilter(),
            new MovementPaging(),
            new MovementSorting((MovementSortField)sort, MovementSortDirection.Asc),
            CancellationToken.None));

        Assert.Equal("sorting", ex.ParamName);
        AssertNoStoreCalls(store);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task FilteredList_WithUnsupportedSortDirection_ThrowsArgumentOutOfRangeException(int sortDirection)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ListMoneyMovementsAsync(
            OwnerId,
            new MovementFilter(),
            new MovementPaging(),
            new MovementSorting(MovementSortField.MovementDate, (MovementSortDirection)sortDirection),
            CancellationToken.None));

        Assert.Equal("sorting", ex.ParamName);
        AssertNoStoreCalls(store);
    }

    public static TheoryData<string, Func<FinanceService, MovementFilter, CancellationToken, Task>> FilterValidationOperations => new()
    {
        { "ListMoneyMovementsAsync", static (s, f, ct) => s.ListMoneyMovementsAsync(OwnerId, f, new MovementPaging(), new MovementSorting(), ct) },
        { "SummarizeMoneyMovementsAsync", static (s, f, ct) => s.SummarizeMoneyMovementsAsync(OwnerId, f, ct) },
    };

    public static TheoryData<string, int, Func<FinanceService, MovementFilter, CancellationToken, Task>> ClassificationValidationCases => new()
    {
        { "ListMoneyMovementsAsync", -1, static (s, f, ct) => s.ListMoneyMovementsAsync(OwnerId, f, new MovementPaging(), new MovementSorting(), ct) },
        { "ListMoneyMovementsAsync", 99, static (s, f, ct) => s.ListMoneyMovementsAsync(OwnerId, f, new MovementPaging(), new MovementSorting(), ct) },
        { "SummarizeMoneyMovementsAsync", -1, static (s, f, ct) => s.SummarizeMoneyMovementsAsync(OwnerId, f, ct) },
        { "SummarizeMoneyMovementsAsync", 99, static (s, f, ct) => s.SummarizeMoneyMovementsAsync(OwnerId, f, ct) },
    };

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
