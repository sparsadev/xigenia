using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class MoneyMovementTests
{
    private static readonly Guid MovementId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TransferGroupId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CategoryId = new("33333333-3333-3333-3333-333333333333");
    private static readonly DateOnly MovementDate = new(2026, 3, 14);
    private const string OwnerId = "owner-1";

    [Fact]
    public void Constructor_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateMovement(amount: 0m));

        Assert.Equal("amount", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateMovement(amount: -5.25m));

        Assert.Equal("amount", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullAmount_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new MoneyMovement(
            MovementId,
            OwnerId,
            null,
            MovementDirection.Pagar,
            null!,
            MovementDate));

        Assert.Equal("amount", ex.ParamName);
    }

    [Fact]
    public void Constructor_PreservesPositiveDecimalPrecision()
    {
        var movement = CreateMovement(amount: 1020.30m);
        var trailing = CreateMovement(amount: 1.100m);

        Assert.Equal(1020.30m, movement.Amount.Amount);
        Assert.Equal("USD", movement.Amount.Currency);
        Assert.Equal(1.100m, trailing.Amount.Amount);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    [InlineData(1.2345)]
    public void Constructor_WithExcessivePrecisionAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateMovement(amount: amount));

        Assert.Equal("amount", ex.ParamName);
    }

    public static TheoryData<decimal> AmountExceedingDecimal182RangeData => new()
    {
        10000000000000000.00m,
        99999999999999999.99m
    };

    [Theory]
    [MemberData(nameof(AmountExceedingDecimal182RangeData))]
    public void Constructor_WithAmountExceedingDecimal182Range_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateMovement(amount: amount));

        Assert.Equal("amount", ex.ParamName);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(0)]
    public void Constructor_WithUndefinedDirection_ThrowsArgumentOutOfRangeException(int direction)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateMovement(direction: (MovementDirection)direction));

        Assert.Equal("direction", ex.ParamName);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    public void Constructor_WithUndefinedClassification_ThrowsArgumentOutOfRangeException(int classification)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateMovement(classification: (MovementClassification)classification));

        Assert.Equal("classification", ex.ParamName);
    }

    [Theory]
    [InlineData(MovementClassification.Unclassified, MovementDirection.Pagar)]
    [InlineData(MovementClassification.Unclassified, MovementDirection.Cobrar)]
    [InlineData(MovementClassification.Income, MovementDirection.Pagar)]
    [InlineData(MovementClassification.Income, MovementDirection.Cobrar)]
    [InlineData(MovementClassification.Expense, MovementDirection.Pagar)]
    [InlineData(MovementClassification.Expense, MovementDirection.Cobrar)]
    public void Constructor_AcceptsNonTransferClassificationDirectionCombinations(
        MovementClassification classification,
        MovementDirection direction)
    {
        var movement = CreateMovement(direction: direction, classification: classification);

        Assert.Equal(classification, movement.Classification);
        Assert.Equal(direction, movement.Direction);
        Assert.Null(movement.TransferGroupId);
    }

    [Theory]
    [InlineData(MovementClassification.Unclassified)]
    [InlineData(MovementClassification.Income)]
    [InlineData(MovementClassification.Expense)]
    public void Constructor_WithTransferGroupAndNonTransferClassification_ThrowsArgumentException(
        MovementClassification classification)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateMovement(
            classification: classification,
            transferGroupId: TransferGroupId));

        Assert.Equal("classification", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithInternalTransferAndNoGroup_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateMovement(
            classification: MovementClassification.InternalTransfer));

        Assert.Equal("classification", ex.ParamName);
    }

    [Theory]
    [InlineData(MovementDirection.Pagar)]
    [InlineData(MovementDirection.Cobrar)]
    public void Constructor_AcceptsInternalTransferWithGroupForBothDirections(MovementDirection direction)
    {
        var movement = CreateMovement(
            direction: direction,
            classification: MovementClassification.InternalTransfer,
            transferGroupId: TransferGroupId);

        Assert.Equal(MovementClassification.InternalTransfer, movement.Classification);
        Assert.Equal(TransferGroupId, movement.TransferGroupId);
        Assert.Equal(direction, movement.Direction);
    }

    [Theory]
    [InlineData(MovementClassification.Income)]
    [InlineData(MovementClassification.Expense)]
    public void Constructor_AcceptsCategoryWithIncomeOrExpense(MovementClassification classification)
    {
        var movement = CreateMovement(classification: classification, categoryId: CategoryId);

        Assert.Equal(CategoryId, movement.FinancialCategoryId);
    }

    [Fact]
    public void Constructor_WithCategoryAndUnclassified_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateMovement(
            classification: MovementClassification.Unclassified,
            categoryId: CategoryId));

        Assert.Equal("financialCategoryId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithCategoryAndInternalTransferGroup_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateMovement(
            classification: MovementClassification.InternalTransfer,
            transferGroupId: TransferGroupId,
            categoryId: CategoryId));

        Assert.Equal("financialCategoryId", ex.ParamName);
    }

    private static MoneyMovement CreateMovement(
        MovementDirection direction = MovementDirection.Pagar,
        decimal amount = 100m,
        MovementClassification classification = MovementClassification.Unclassified,
        Guid? transferGroupId = null,
        Guid? categoryId = null) => new(
            MovementId,
            OwnerId,
            null,
            direction,
            new Money(amount, "USD"),
            MovementDate,
            null,
            null,
            transferGroupId,
            classification,
            categoryId);
}
