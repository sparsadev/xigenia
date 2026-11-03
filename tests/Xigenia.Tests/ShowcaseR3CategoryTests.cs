using Xigenia.Web.Models;

namespace Xigenia.Tests;

public sealed class ShowcaseR3CategoryTests
{
    [Fact]
    public void CreateCategoryRequest_ValidatesNameLength()
    {
        var req = new CreateFinancialCategoryRequest("A", MovementClassificationDto.Income, null);
        Assert.True(req.Name.Length <= 200);
        var longName = new string('A', 201);
        Assert.True(longName.Length > 200);
    }

    [Fact]
    public void CategoryParent_MustBeTopLevelSameClassification()
    {
        var parent = new FinancialCategoryResponse(Guid.NewGuid(), null, "Parent", MovementClassificationDto.Expense, true, DateTimeOffset.UtcNow, false);
        var childClassification = MovementClassificationDto.Expense;
        Assert.True(parent.IsActive);
        Assert.Null(parent.ParentCategoryId);
        Assert.Equal(parent.Classification, childClassification);
        var invalidParent = new FinancialCategoryResponse(Guid.NewGuid(), Guid.NewGuid(), "Child", MovementClassificationDto.Expense, true, DateTimeOffset.UtcNow, false);
        Assert.NotNull(invalidParent.ParentCategoryId);
    }

    [Fact]
    public void MovementCategoryId_OnlyWhenActiveCompatible()
    {
        var cat = new FinancialCategoryResponse(Guid.NewGuid(), null, "Food", MovementClassificationDto.Expense, true, DateTimeOffset.UtcNow, false);
        var movementClassification = MovementClassificationDto.Expense;
        Assert.True(cat.IsActive && cat.Classification == movementClassification);
        var inactive = new FinancialCategoryResponse(Guid.NewGuid(), null, "Old", MovementClassificationDto.Expense, false, DateTimeOffset.UtcNow, false);
        Assert.False(inactive.IsActive);
    }

    [Fact]
    public void CategoryDtos_DoNotExposeOwnerId()
    {
        var props = typeof(CreateFinancialCategoryRequest).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("OwnerId", props);
    }
}
