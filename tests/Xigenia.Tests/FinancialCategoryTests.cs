using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinancialCategoryTests
{
    private static readonly Guid CategoryId = new("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ParentCategoryId = new("66666666-6666-6666-6666-666666666666");
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";

    [Theory]
    [InlineData(MovementClassification.Income)]
    [InlineData(MovementClassification.Expense)]
    public void Constructor_AcceptsIncomeAndExpenseClassifications(MovementClassification classification)
    {
        var category = CreateCategory(classification: classification);

        Assert.Equal(classification, category.Classification);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_AcceptsGlobalAndPersonalOwners(bool isGlobal)
    {
        string? ownerId = isGlobal ? null : OwnerId;

        var category = CreateCategory(ownerId: ownerId);

        Assert.Equal(ownerId, category.OwnerId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_AcceptsNullAndValidParentCategoryId(bool hasParent)
    {
        Guid? parentCategoryId = hasParent ? ParentCategoryId : null;

        var category = CreateCategory(parentCategoryId: parentCategoryId);

        Assert.Equal(parentCategoryId, category.ParentCategoryId);
    }

    [Fact]
    public void Constructor_TrimsNameAndPreservesProperties()
    {
        var category = CreateCategory(name: "  Groceries  ");

        Assert.Equal("Groceries", category.Name);
        Assert.Equal(CategoryId, category.Id);
        Assert.Equal(OwnerId, category.OwnerId);
        Assert.Equal(MovementClassification.Expense, category.Classification);
        Assert.True(category.IsActive);
        Assert.Equal(CreatedAt, category.CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(99)]
    [InlineData(-1)]
    public void Constructor_WithNonIncomeExpenseClassification_ThrowsArgumentOutOfRangeException(int classification)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateCategory(
            classification: (MovementClassification)classification));

        Assert.Equal("classification", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyParentCategoryId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateCategory(parentCategoryId: Guid.Empty));

        Assert.Equal("parentCategoryId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithSelfParentCategoryId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateCategory(parentCategoryId: CategoryId));

        Assert.Equal("parentCategoryId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateCategory(id: Guid.Empty));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankOwner_ThrowsArgumentException(string ownerId)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateCategory(ownerId: ownerId));

        Assert.Equal("ownerId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => CreateCategory(name: null!));

        Assert.Equal("name", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_ThrowsArgumentException(string name)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateCategory(name: name));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Rename_TrimsValidName()
    {
        var category = CreateCategory(name: "Original");

        category.Rename("  Updated  ");

        Assert.Equal("Updated", category.Name);
    }

    [Fact]
    public void Rename_WithNullName_ThrowsArgumentNullException()
    {
        var category = CreateCategory();

        var ex = Assert.Throws<ArgumentNullException>(() => category.Rename(null!));

        Assert.Equal("name", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithBlankName_ThrowsArgumentException(string name)
    {
        var category = CreateCategory();

        var ex = Assert.Throws<ArgumentException>(() => category.Rename(name));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Deactivate_SetsInactiveAndIsIdempotent()
    {
        var active = CreateCategory(isActive: true);
        var inactive = CreateCategory(isActive: false);

        active.Deactivate();
        Assert.False(active.IsActive);

        active.Deactivate();
        Assert.False(active.IsActive);

        inactive.Deactivate();
        Assert.False(inactive.IsActive);
    }

    [Fact]
    public void Reactivate_SetsActiveAndIsIdempotent()
    {
        var inactive = CreateCategory(isActive: false);
        var active = CreateCategory(isActive: true);

        inactive.Reactivate();
        Assert.True(inactive.IsActive);

        inactive.Reactivate();
        Assert.True(inactive.IsActive);

        active.Reactivate();
        Assert.True(active.IsActive);
    }

    private static FinancialCategory CreateCategory(
        Guid? id = null,
        string? ownerId = OwnerId,
        Guid? parentCategoryId = null,
        string name = "Groceries",
        MovementClassification classification = MovementClassification.Expense,
        bool isActive = true,
        DateTimeOffset? createdAt = null) => new(
            id ?? CategoryId,
            ownerId,
            parentCategoryId,
            name,
            classification,
            isActive,
            createdAt ?? CreatedAt);
}
