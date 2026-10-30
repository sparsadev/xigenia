using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceCategoryMovementTests
{
    private static readonly Guid CategoryId = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly DateOnly MovementDate = new(2026, 3, 14);
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    [Fact]
    public async Task CreateMovement_WithMissingCategory_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(classification: MovementClassification.Income, categoryId: CategoryId),
            CancellationToken.None));

        var lookup = Assert.Single(store.CategoryLookups);
        Assert.Equal(CategoryId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateMovement_WithOtherOwnerCategory_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(ownerId: OtherOwnerId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(classification: MovementClassification.Income, categoryId: CategoryId),
            CancellationToken.None));

        var lookup = Assert.Single(store.CategoryLookups);
        Assert.Equal(CategoryId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateMovement_WithEmptyCategoryId_ThrowsArgumentException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(classification: MovementClassification.Income, categoryId: Guid.Empty),
            CancellationToken.None));

        Assert.Equal("categoryId", ex.ParamName);
        Assert.Empty(store.CategoryLookups);
        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateMovement_WithInactiveCategory_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(isActive: false));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(classification: MovementClassification.Income, categoryId: CategoryId),
            CancellationToken.None));

        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateMovement_WithClassificationMismatch_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(classification: MovementClassification.Expense));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(classification: MovementClassification.Income, categoryId: CategoryId),
            CancellationToken.None));

        AssertNoPersistence(store);
    }

    [Theory]
    [InlineData(true, MovementClassification.Income)]
    [InlineData(true, MovementClassification.Expense)]
    [InlineData(false, MovementClassification.Income)]
    [InlineData(false, MovementClassification.Expense)]
    public async Task CreateMovement_WithValidCategory_PreservesCategoryId(
        bool isGlobal,
        MovementClassification classification)
    {
        string? ownerId = isGlobal ? null : OwnerId;
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(ownerId: ownerId, classification: classification));
        var service = new FinanceService(store);

        var response = await service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(classification: classification, categoryId: CategoryId),
            CancellationToken.None);

        var lookup = Assert.Single(store.CategoryLookups);
        Assert.Equal(CategoryId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        var captured = Assert.Single(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Equal(OwnerId, captured.OwnerId);
        Assert.Equal(classification, captured.Classification);
        Assert.Equal(CategoryId, captured.FinancialCategoryId);
        AssertMovementResponseMatches(response, captured);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateMovement_WithNonPositiveAmount_ThrowsArgumentOutOfRangeException(int amount)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(amount: amount),
            CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateMovement_WithMinimumDate_ThrowsArgumentOutOfRangeException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(movementDate: DateOnly.MinValue),
            CancellationToken.None));

        Assert.Equal("movementDate", ex.ParamName);
        AssertNoPersistence(store);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task CreateMovement_WithUndefinedDirection_ThrowsArgumentOutOfRangeException(int direction)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(direction: (MovementDirection)direction),
            CancellationToken.None));

        Assert.Equal("direction", ex.ParamName);
        AssertNoPersistence(store);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task CreateMovement_WithUndefinedClassification_ThrowsArgumentOutOfRangeException(int classification)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(classification: (MovementClassification)classification),
            CancellationToken.None));

        Assert.Equal("classification", ex.ParamName);
        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateMovement_WithInternalTransferAndNoGroup_ThrowsArgumentException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateMoneyMovementAsync(
            OwnerId,
            CreateRequest(classification: MovementClassification.InternalTransfer),
            CancellationToken.None));

        Assert.Equal("classification", ex.ParamName);
        AssertNoPersistence(store);
    }

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

    private static CreateMoneyMovementRequest CreateRequest(
        decimal amount = 100m,
        DateOnly? movementDate = null,
        MovementDirection direction = MovementDirection.Cobrar,
        MovementClassification classification = MovementClassification.Unclassified,
        Guid? categoryId = null) => new(
            Direction: direction,
            Amount: amount,
            Currency: "USD",
            MovementDate: movementDate ?? MovementDate,
            Note: null,
            ObligationId: null,
            FinancialAccountId: null,
            Classification: classification,
            FinancialCategoryId: categoryId);

    private static FinancialCategory CreateCategory(
        string? ownerId = OwnerId,
        MovementClassification classification = MovementClassification.Income,
        bool isActive = true) => new(
            CategoryId,
            ownerId,
            null,
            "Salary",
            classification,
            isActive,
            CreatedAt);

    private static void AssertNoPersistence(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }
}
