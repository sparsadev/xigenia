using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_PreservesExactDecimalPrecision()
    {
        var money = new Money(1234.56m, "USD");
        var sum = new Money(0.1m + 0.2m, "USD");
        var precise = new Money(0.123456m, "USD");

        Assert.Equal(1234.56m, money.Amount);
        Assert.Equal(0.3m, sum.Amount);
        Assert.Equal(0.123456m, precise.Amount);
    }

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData(" eur ", "EUR")]
    [InlineData("Jpy", "JPY")]
    public void Constructor_NormalizesCurrencyCode(string currency, string expected)
    {
        var money = new Money(10.00m, currency);

        Assert.Equal(expected, money.Currency);
    }

    [Fact]
    public void Constructor_WithNullCurrency_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Money(10m, null!));

        Assert.Equal("currency", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankCurrency_ThrowsArgumentException(string currency)
    {
        var ex = Assert.Throws<ArgumentException>(() => new Money(10m, currency));

        Assert.Equal("currency", ex.ParamName);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("ZZZ")]
    [InlineData("USDX")]
    [InlineData("US")]
    [InlineData("XTS")]
    [InlineData("XXX")]
    public void Constructor_WithUnsupportedCurrency_ThrowsArgumentException(string currency)
    {
        var ex = Assert.Throws<ArgumentException>(() => new Money(10m, currency));

        Assert.Equal("currency", ex.ParamName);
    }
}
