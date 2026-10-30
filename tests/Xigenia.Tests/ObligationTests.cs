using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class ObligationTests
{
    private static readonly Guid ObligationId = new("77777777-7777-7777-7777-777777777777");
    private static readonly DateOnly IssueDate = new(2026, 3, 14);
    private const string OwnerId = "owner-1";

    [Fact]
    public void Constructor_WithNullTotal_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Obligation(
            ObligationId,
            OwnerId,
            ObligationKind.PorPagar,
            "Power bill",
            null!,
            IssueDate));

        Assert.Equal("total", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-250)]
    public void Constructor_WithNonPositiveTotal_ThrowsArgumentOutOfRangeException(int total)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateObligation(total: total));

        Assert.Equal("total", ex.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2500)]
    public void Constructor_AcceptsPositiveTotal(int total)
    {
        var obligation = CreateObligation(total: total);

        Assert.Equal((decimal)total, obligation.Total.Amount);
        Assert.Equal("USD", obligation.Total.Currency);
    }

    [Fact]
    public void Constructor_AcceptsMinimumPositiveTotal()
    {
        var obligation = CreateObligation(total: 0.01m);

        Assert.Equal(0.01m, obligation.Total.Amount);
    }

    [Fact]
    public void Constructor_WithDueDateBeforeIssueDate_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateObligation(dueDate: new DateOnly(2026, 3, 13)));

        Assert.Equal("dueDate", ex.ParamName);
    }

    [Theory]
    [InlineData(2026, 3, 14)]
    [InlineData(2026, 3, 15)]
    [InlineData(2026, 12, 31)]
    public void Constructor_AcceptsEqualOrLaterDueDate(int year, int month, int day)
    {
        var dueDate = new DateOnly(year, month, day);

        var obligation = CreateObligation(dueDate: dueDate);

        Assert.Equal(dueDate, obligation.DueDate);
    }

    [Fact]
    public void Constructor_AcceptsNullDueDate()
    {
        var obligation = CreateObligation(dueDate: null);

        Assert.Null(obligation.DueDate);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateObligation(id: Guid.Empty));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_WithUndefinedKind_ThrowsArgumentOutOfRangeException(int kind)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateObligation(kind: (ObligationKind)kind));

        Assert.Equal("kind", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOwner_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => CreateObligation(ownerId: null!));

        Assert.Equal("ownerId", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankOwner_ThrowsArgumentException(string ownerId)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateObligation(ownerId: ownerId));

        Assert.Equal("ownerId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTitle_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => CreateObligation(title: null!));

        Assert.Equal("title", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTitle_ThrowsArgumentException(string title)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateObligation(title: title));

        Assert.Equal("title", ex.ParamName);
    }

    [Fact]
    public void Constructor_TrimsTitleAndPreservesProperties()
    {
        var dueDate = new DateOnly(2026, 4, 13);

        var obligation = CreateObligation(title: "  Power bill  ", total: 250.75m, dueDate: dueDate);

        Assert.Equal("Power bill", obligation.Title);
        Assert.Equal(ObligationId, obligation.Id);
        Assert.Equal(OwnerId, obligation.OwnerId);
        Assert.Equal(ObligationKind.PorPagar, obligation.Kind);
        Assert.Equal(250.75m, obligation.Total.Amount);
        Assert.Equal(IssueDate, obligation.IssueDate);
        Assert.Equal(dueDate, obligation.DueDate);
    }

    private static Obligation CreateObligation(
        Guid? id = null,
        string ownerId = OwnerId,
        ObligationKind kind = ObligationKind.PorPagar,
        string title = "Power bill",
        decimal total = 250m,
        DateOnly? issueDate = null,
        DateOnly? dueDate = null) => new(
            id ?? ObligationId,
            ownerId,
            kind,
            title,
            new Money(total, "USD"),
            issueDate ?? IssueDate,
            dueDate);
}
