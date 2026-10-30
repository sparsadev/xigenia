using Xigenia.Application.Finance;
using Xigenia.Domain;
using Xunit.Abstractions;

namespace Xigenia.Tests;

public sealed class FinanceServiceMovementQueryTests
{
    private static readonly Guid MovementAId = new("17171717-1717-1717-1717-171717171717");
    private static readonly Guid MovementBId = new("18181818-1818-1818-1818-181818181818");
    private static readonly Guid MovementCId = new("19191919-1919-1919-1919-191919191919");
    private static readonly Guid OtherOwnerMovementId = new("20202020-2020-2020-2020-202020202020");
    private static readonly Guid ObligationId = new("21212121-2121-2121-2121-212121212121");
    private static readonly Guid AccountAId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AccountCId = new("23232323-2323-2323-2323-232323232323");
    private static readonly Guid CategoryAId = new("24242424-2424-2424-2424-242424242424");
    private static readonly Guid TransferGroupId = new("25252525-2525-2525-2525-252525252525");
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    private readonly ITestOutputHelper _output;

    public FinanceServiceMovementQueryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(FilteredQueryCases))]
    public async Task FilteredList_ForwardsQueryArgumentsToStore(
        string name,
        MovementFilter filter,
        MovementPaging paging,
        MovementSorting sorting)
    {
        _output.WriteLine($"Case: {name}");

        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await service.ListMoneyMovementsAsync(OwnerId, filter, paging, sorting, cts.Token);

        var listCall = Assert.Single(store.FilteredListCalls);
        Assert.Equal(OwnerId, listCall.OwnerId);
        Assert.Equal(filter, listCall.Filter);
        Assert.Equal(paging, listCall.Paging);
        Assert.Equal(sorting, listCall.Sorting);
        Assert.Equal(cts.Token, listCall.CancellationToken);
        var countCall = Assert.Single(store.CountCalls);
        Assert.Equal(OwnerId, countCall.OwnerId);
        Assert.Equal(filter, countCall.Filter);
        Assert.Equal(cts.Token, countCall.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task FilteredList_MapsRowsAndTotalCount()
    {
        var store = new FakeFinanceStore();
        var seeded = SeedOwnerMovements(store);
        store.SeedMovement(CreateOtherOwnerMovement());
        store.ConfiguredTotalCount = 7;
        var service = new FinanceService(store);

        var (movements, totalCount) = await service.ListMoneyMovementsAsync(
            OwnerId,
            new MovementFilter(),
            new MovementPaging(),
            new MovementSorting(),
            CancellationToken.None);

        Assert.Equal(7, totalCount);
        Assert.Equal(new[] { MovementAId, MovementBId, MovementCId }, movements.Select(m => m.Id));
        AssertMovementResponseMatches(movements[0], seeded[0]);
        AssertMovementResponseMatches(movements[1], seeded[1]);
        AssertMovementResponseMatches(movements[2], seeded[2]);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task Summarize_ForwardsOwnerFilterAndToken()
    {
        var store = new FakeFinanceStore();
        store.ConfiguredSummary = new MoneyMovementSummaryResponse(
            [new MoneyMovementCurrencySummary("USD", 100m, 0m, 100m, 1)],
            0,
            0);
        var service = new FinanceService(store);
        var filter = new MovementFilter(
            From: new DateOnly(2026, 3, 1),
            To: new DateOnly(2026, 3, 31),
            Classification: MovementClassification.Income);
        using var cts = new CancellationTokenSource();

        await service.SummarizeMoneyMovementsAsync(OwnerId, filter, cts.Token);

        var call = Assert.Single(store.SummaryCalls);
        Assert.Equal(OwnerId, call.OwnerId);
        Assert.Equal(filter, call.Filter);
        Assert.Equal(cts.Token, call.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task Summarize_ReturnsConfiguredSummaryUnchanged()
    {
        var store = new FakeFinanceStore();
        store.ConfiguredSummary = new MoneyMovementSummaryResponse(
            [
                new MoneyMovementCurrencySummary("USD", 1000m, 250.50m, 749.50m, 5),
                new MoneyMovementCurrencySummary("EUR", 300m, 100m, 200m, 2),
            ],
            InternalTransferCount: 3,
            UnclassifiedCount: 4);
        var service = new FinanceService(store);

        var response = await service.SummarizeMoneyMovementsAsync(
            OwnerId,
            new MovementFilter(),
            CancellationToken.None);

        Assert.Same(store.ConfiguredSummary, response);
        Assert.Equal(2, response.Totals.Count);
        Assert.Equal("USD", response.Totals[0].Currency);
        Assert.Equal(1000m, response.Totals[0].Income);
        Assert.Equal(749.50m, response.Totals[0].Net);
        Assert.Equal("EUR", response.Totals[1].Currency);
        Assert.Equal(200m, response.Totals[1].Net);
        Assert.Equal(3, response.InternalTransferCount);
        Assert.Equal(4, response.UnclassifiedCount);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task UnfilteredList_ReturnsOnlyRequestingOwnerRows()
    {
        var store = new FakeFinanceStore();
        var seeded = SeedOwnerMovements(store);
        store.SeedMovement(CreateOtherOwnerMovement());
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var movements = await service.ListMoneyMovementsAsync(OwnerId, cts.Token);

        var call = Assert.Single(store.MovementListCalls);
        Assert.Equal(OwnerId, call.OwnerId);
        Assert.Equal(cts.Token, call.CancellationToken);
        Assert.Equal(new[] { MovementAId, MovementBId, MovementCId }, movements.Select(m => m.Id));
        Assert.DoesNotContain(movements, m => m.Id == OtherOwnerMovementId);
        AssertMovementResponseMatches(movements[0], seeded[0]);
        AssertMovementResponseMatches(movements[1], seeded[1]);
        AssertMovementResponseMatches(movements[2], seeded[2]);
        AssertNoWrites(store);
    }

    public static TheoryData<string, MovementFilter, MovementPaging, MovementSorting> FilteredQueryCases => new()
    {
        { "Defaults", new MovementFilter(), new MovementPaging(), new MovementSorting() },
        { "Page 1 size 1", new MovementFilter(), new MovementPaging(Page: 1, PageSize: 1), new MovementSorting() },
        { "Page 1 size 200", new MovementFilter(), new MovementPaging(Page: 1, PageSize: 200), new MovementSorting() },
        { "Equal bounds", new MovementFilter(From: new DateOnly(2026, 3, 14), To: new DateOnly(2026, 3, 14)), new MovementPaging(), new MovementSorting() },
        { "Open upper bound", new MovementFilter(From: new DateOnly(2026, 3, 1)), new MovementPaging(), new MovementSorting() },
        { "Open lower bound", new MovementFilter(To: new DateOnly(2026, 3, 31)), new MovementPaging(), new MovementSorting() },
        { "Descending", new MovementFilter(), new MovementPaging(), new MovementSorting(MovementSortField.MovementDate, MovementSortDirection.Desc) },
    };

    private static IReadOnlyList<MoneyMovement> SeedOwnerMovements(FakeFinanceStore store)
    {
        var movements = new List<MoneyMovement>
        {
            new(
                MovementAId,
                OwnerId,
                ObligationId,
                MovementDirection.Cobrar,
                new Money(150.25m, "USD"),
                new DateOnly(2026, 3, 10),
                "  Salary  ",
                AccountAId,
                null,
                MovementClassification.Unclassified,
                null),
            new(
                MovementBId,
                OwnerId,
                null,
                MovementDirection.Pagar,
                new Money(99.99m, "EUR"),
                new DateOnly(2026, 3, 12),
                null,
                null,
                null,
                MovementClassification.Expense,
                null),
            new(
                MovementCId,
                OwnerId,
                null,
                MovementDirection.Pagar,
                new Money(50m, "USD"),
                new DateOnly(2026, 3, 11),
                null,
                AccountCId,
                TransferGroupId,
                MovementClassification.InternalTransfer,
                null),
        };

        foreach (var movement in movements)
        {
            store.SeedMovement(movement);
        }

        return movements;
    }

    private static MoneyMovement CreateOtherOwnerMovement() => new(
        OtherOwnerMovementId,
        OtherOwnerId,
        null,
        MovementDirection.Cobrar,
        new Money(10m, "USD"),
        new DateOnly(2026, 3, 13),
        null,
        null,
        null,
        MovementClassification.Unclassified,
        null);

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

    private static void AssertNoWrites(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
        Assert.Empty(store.RecordedTransfers);
    }
}
