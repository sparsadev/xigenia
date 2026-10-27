namespace Xigenia.Tests;

public sealed class DecimalPrecisionTests
{
    [Fact]
    public void Sum_UsesDecimalExactly()
    {
        decimal a = 0.1m;
        decimal b = 0.2m;

        Assert.Equal(0.3m, a + b);
    }
}
