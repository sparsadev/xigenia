namespace Xigenia.Domain;

public sealed class FinancialAccount
{
    private FinancialAccount()
    {
        // EF Core materialization only.
        OwnerId = null!;
        Name = null!;
        Currency = null!;
    }

    public FinancialAccount(
        Guid id,
        string ownerId,
        string name,
        FinancialAccountType type,
        string currency,
        decimal openingBalance,
        bool isActive,
        DateTimeOffset createdAt,
        decimal? creditLimit = null,
        int? statementClosingDay = null,
        int? paymentDueDay = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        // Reuse Money as the ISO 4217 authority; the zero amount is discarded.
        var normalizedCurrency = new Money(0, currency).Currency;

        if (type != FinancialAccountType.CreditCard)
        {
            if (creditLimit.HasValue)
            {
                throw new ArgumentException("Credit limit is only valid for credit cards.", nameof(creditLimit));
            }

            if (statementClosingDay.HasValue)
            {
                throw new ArgumentException("Statement closing day is only valid for credit cards.", nameof(statementClosingDay));
            }

            if (paymentDueDay.HasValue)
            {
                throw new ArgumentException("Payment due day is only valid for credit cards.", nameof(paymentDueDay));
            }
        }
        else
        {
            if (openingBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(openingBalance), "Credit card opening balance must be nonnegative and represents opening debt.");
            }
        }

        if (creditLimit is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creditLimit), "Credit limit must be positive.");
        }

        if (statementClosingDay is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(statementClosingDay), "Statement closing day must be between 1 and 31.");
        }

        if (paymentDueDay is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(paymentDueDay), "Payment due day must be between 1 and 31.");
        }

        Id = id;
        OwnerId = ownerId;
        Name = name.Trim();
        Type = type;
        Currency = normalizedCurrency;
        OpeningBalance = openingBalance;
        IsActive = isActive;
        CreatedAt = createdAt;
        CreditLimit = creditLimit;
        StatementClosingDay = statementClosingDay;
        PaymentDueDay = paymentDueDay;
    }

    public Guid Id { get; private set; }

    public string OwnerId { get; private set; }

    public string Name { get; private set; }

    public FinancialAccountType Type { get; private set; }

    public string Currency { get; private set; }

    public decimal OpeningBalance { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public decimal? CreditLimit { get; private set; }

    public int? StatementClosingDay { get; private set; }

    public int? PaymentDueDay { get; private set; }
}
