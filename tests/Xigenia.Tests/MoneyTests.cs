using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_PreservesExactDecimalPrecision()
    {
        var money = new Money(1234.56m, "USD");
        var sum = new Money(0.1m + 0.2m, "USD");
        var twoDecimals = new Money(0.20m, "USD");
        var trailingZeros = new Money(1.100m, "USD");

        Assert.Equal(1234.56m, money.Amount);
        Assert.Equal(0.3m, sum.Amount);
        Assert.Equal(0.20m, twoDecimals.Amount);
        Assert.Equal(1.100m, trailingZeros.Amount);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    [InlineData(1.001)]
    [InlineData(1.2345)]
    public void Constructor_WithExcessivePrecision_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, "USD"));

        Assert.Equal("amount", ex.ParamName);
    }

    [Theory]
    [InlineData(10000000000000000.00)]
    [InlineData(-10000000000000000.00)]
    public void Constructor_WithAmountExceedingDecimal182Range_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, "USD"));

        Assert.Equal("amount", ex.ParamName);
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
