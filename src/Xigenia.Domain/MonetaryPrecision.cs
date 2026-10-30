namespace Xigenia.Domain;

public static class MonetaryPrecision
{
    public const decimal MaxValue = 9999999999999999.99m;
    public const decimal MinValue = -9999999999999999.99m;

    public static void Validate(decimal amount, string paramName)
    {
        if (amount < MinValue || amount > MaxValue)
        {
            throw new ArgumentOutOfRangeException(paramName, amount, "Monetary amount exceeds the supported precision (decimal(18,2)).");
        }

        if (amount != decimal.Round(amount, 2, MidpointRounding.AwayFromZero))
        {
            throw new ArgumentOutOfRangeException(paramName, amount, "Monetary amount must have at most two decimal places.");
        }
    }
}
