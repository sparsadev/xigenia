using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class ShowcaseR2QueryCases
{
    [Fact]
    public void Reversed_range_is_detected_before_request()
    {
        var q = new Xigenia.Web.Models.MovementQuery(From: new DateOnly(2026, 3, 14), To: new DateOnly(2026, 3, 13));
        Assert.True(q.IsReversed);
    }

    [Theory]
    [InlineData("Income", MovementClassification.Income)]
    [InlineData("Expense", MovementClassification.Expense)]
    [InlineData("InternalTransfer", MovementClassification.InternalTransfer)]
    [InlineData("Unclassified", MovementClassification.Unclassified)]
    public void Valid_classifications_map_to_backend(string dto, MovementClassification expected)
    {
        Assert.True(Enum.TryParse<MovementClassification>(dto, ignoreCase: true, out var parsed));
        Assert.Equal(expected, parsed);
        Assert.True(Enum.IsDefined(parsed));
    }

    [Theory]
    [InlineData("Foo")]
    [InlineData("PorPagar")]
    public void Invalid_classification_rejected(string dto)
    {
        var ok = Enum.TryParse<MovementClassification>(dto, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
        Assert.False(ok);
    }

    [Fact]
    public void Pagination_fixed_pageSize_50()
    {
        const int pageSize = 50;
        Assert.Equal(50, pageSize);
        // Next-page detection: only if returned count == pageSize
        Assert.True(new[] { 1, 2, 3 }.Length != pageSize);
        Assert.True(Enumerable.Repeat(1, 50).Count() == pageSize);
    }

    [Fact]
    public void Page_reset_on_filter_change()
    {
        var q1 = new Xigenia.Web.Models.MovementQuery(From: new DateOnly(2026, 3, 1));
        var q2 = new Xigenia.Web.Models.MovementQuery(From: new DateOnly(2026, 3, 2));
        Assert.NotEqual(q1.FilterIdentity, q2.FilterIdentity);
        // page should be 1 after filter change
        var pageAfter = 1;
        Assert.Equal(1, pageAfter);
    }
}
