using Xigenia.Web.Models;

namespace Xigenia.Tests;

public sealed class ShowcaseR2ReadTests
{
    [Fact]
    public void MovementQuery_ToQueryString_builds_expected_params()
    {
        var q = new MovementQuery(
            From: new DateOnly(2026, 3, 1),
            To: new DateOnly(2026, 3, 31),
            FinancialAccountId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FinancialCategoryId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Classification: "Income");
        var qs = q.ToQueryString(1, 50);
        Assert.Contains("from=2026-03-01", qs);
        Assert.Contains("to=2026-03-31", qs);
        Assert.Contains("financialAccountId=11111111-1111-1111-1111-111111111111", qs);
        Assert.Contains("financialCategoryId=22222222-2222-2222-2222-222222222222", qs);
        Assert.Contains("classification=Income", qs);
        Assert.Contains("page=1", qs);
        Assert.Contains("pageSize=50", qs);
    }

    [Fact]
    public void MovementQuery_ToQueryString_omits_null_filters()
    {
        var q = new MovementQuery();
        var qs = q.ToQueryString(1, 50);
        Assert.DoesNotContain("from=", qs);
        Assert.DoesNotContain("to=", qs);
        Assert.DoesNotContain("financialAccountId=", qs);
        Assert.DoesNotContain("financialCategoryId=", qs);
        Assert.DoesNotContain("classification=", qs);
        Assert.Contains("page=1", qs);
        Assert.Contains("pageSize=50", qs);
    }

    [Fact]
    public void MovementQuery_ToSummaryQueryString_excludes_paging()
    {
        var q = new MovementQuery(From: new DateOnly(2026, 3, 1), Classification: "Expense");
        var qs = q.ToSummaryQueryString();
        Assert.Contains("from=2026-03-01", qs);
        Assert.Contains("classification=Expense", qs);
        Assert.DoesNotContain("page=", qs);
        Assert.DoesNotContain("pageSize=", qs);
    }

    [Theory]
    [InlineData("2026-03-14", "2026-03-13", true)]
    [InlineData("2026-03-14", "2026-03-14", false)]
    [InlineData("2026-03-01", null, false)]
    public void MovementQuery_IsReversed_detects_reversed_ranges(string? from, string? to, bool expected)
    {
        DateOnly? f = from is null ? null : DateOnly.Parse(from);
        DateOnly? t = to is null ? null : DateOnly.Parse(to);
        var q = new MovementQuery(From: f, To: t);
        Assert.Equal(expected, q.IsReversed);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Income", true)]
    [InlineData("Expense", true)]
    [InlineData("InternalTransfer", true)]
    [InlineData("Unclassified", true)]
    [InlineData("income", true)]
    [InlineData("Invalid", false)]
    [InlineData("PORPAGAR", false)]
    public void MovementQuery_Classification_validation(string? classification, bool expectedValid)
    {
        var q = new MovementQuery(Classification: classification);
        Assert.Equal(expectedValid, q.IsClassificationValid);
    }

    [Fact]
    public void MovementQuery_Date_format_is_yyyy_MM_dd()
    {
        var q = new MovementQuery(From: new DateOnly(2026, 1, 5), To: new DateOnly(2026, 12, 31));
        var qs = q.ToQueryString(2, 50);
        Assert.Contains("from=2026-01-05", qs);
        Assert.Contains("to=2026-12-31", qs);
        Assert.DoesNotContain("01/05/2026", qs);
    }

    [Fact]
    public void MovementQuery_Identity_differs_by_page()
    {
        var q = new MovementQuery(From: new DateOnly(2026, 3, 1));
        var id1 = q.Identity(1, 50);
        var id2 = q.Identity(2, 50);
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void MovementQuery_FilterIdentity_ignores_page()
    {
        var q = new MovementQuery(From: new DateOnly(2026, 3, 1));
        var f = q.FilterIdentity;
        Assert.Equal(q.FilterIdentity, new MovementQuery(From: new DateOnly(2026, 3, 1)).FilterIdentity);
        Assert.NotEqual(f, new MovementQuery(From: new DateOnly(2026, 3, 2)).FilterIdentity);
    }
}
