using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinancialAccountTests
{
    private static readonly Guid AccountId = new("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";

    [Theory]
    [InlineData(FinancialAccountType.Cash, -10)]
    [InlineData(FinancialAccountType.Cash, 0)]
    [InlineData(FinancialAccountType.Cash, 10)]
    [InlineData(FinancialAccountType.BankAccount, -10)]
    [InlineData(FinancialAccountType.BankAccount, 0)]
    [InlineData(FinancialAccountType.BankAccount, 10)]
    [InlineData(FinancialAccountType.DigitalWallet, -10)]
    [InlineData(FinancialAccountType.DigitalWallet, 0)]
    [InlineData(FinancialAccountType.DigitalWallet, 10)]
    public void Constructor_AcceptsAssetOpeningBalances(FinancialAccountType type, int openingBalance)
    {
        var account = CreateAccount(type: type, openingBalance: openingBalance);

        Assert.Equal(type, account.Type);
        Assert.Equal((decimal)openingBalance, account.OpeningBalance);
        Assert.Null(account.CreditLimit);
        Assert.Null(account.StatementClosingDay);
        Assert.Null(account.PaymentDueDay);
    }

    [Fact]
    public void Constructor_WithNegativeCardOpeningBalance_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(
            type: FinancialAccountType.CreditCard,
            openingBalance: -0.01m));

        Assert.Equal("openingBalance", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Constructor_AcceptsNonnegativeCardOpeningBalance(int openingBalance)
    {
        var account = CreateAccount(type: FinancialAccountType.CreditCard, openingBalance: openingBalance);

        Assert.Equal((decimal)openingBalance, account.OpeningBalance);
    }

    [Fact]
    public void Constructor_AcceptsNullCardCreditLimit()
    {
        var account = CreateAccount(type: FinancialAccountType.CreditCard, creditLimit: null);

        Assert.Null(account.CreditLimit);
    }

    [Fact]
    public void Constructor_AcceptsMinimumPositiveCardCreditLimit()
    {
        var account = CreateAccount(type: FinancialAccountType.CreditCard, creditLimit: 0.01m);

        Assert.Equal(0.01m, account.CreditLimit);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    public void Constructor_AcceptsPositiveCardCreditLimit(int creditLimit)
    {
        var account = CreateAccount(type: FinancialAccountType.CreditCard, creditLimit: creditLimit);

        Assert.Equal((decimal)creditLimit, account.CreditLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveCardCreditLimit_ThrowsArgumentOutOfRangeException(int creditLimit)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(
            type: FinancialAccountType.CreditCard,
            creditLimit: creditLimit));

        Assert.Equal("creditLimit", ex.ParamName);
    }

    [Theory]
    [InlineData(FinancialAccountType.Cash)]
    [InlineData(FinancialAccountType.BankAccount)]
    [InlineData(FinancialAccountType.DigitalWallet)]
    public void Constructor_WithCreditLimitOnAssetAccount_ThrowsArgumentException(FinancialAccountType type)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(type: type, creditLimit: 1000m));

        Assert.Equal("creditLimit", ex.ParamName);
    }

    [Theory]
    [InlineData(FinancialAccountType.Cash)]
    [InlineData(FinancialAccountType.BankAccount)]
    [InlineData(FinancialAccountType.DigitalWallet)]
    public void Constructor_WithStatementClosingDayOnAssetAccount_ThrowsArgumentException(FinancialAccountType type)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(type: type, statementClosingDay: 15));

        Assert.Equal("statementClosingDay", ex.ParamName);
    }

    [Theory]
    [InlineData(FinancialAccountType.Cash)]
    [InlineData(FinancialAccountType.BankAccount)]
    [InlineData(FinancialAccountType.DigitalWallet)]
    public void Constructor_WithPaymentDueDayOnAssetAccount_ThrowsArgumentException(FinancialAccountType type)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(type: type, paymentDueDay: 15));

        Assert.Equal("paymentDueDay", ex.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsNullStatementClosingDay()
    {
        var account = CreateAccount(type: FinancialAccountType.CreditCard, statementClosingDay: null);

        Assert.Null(account.StatementClosingDay);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    public void Constructor_AcceptsBoundaryStatementClosingDay(int day)
    {
        var account = CreateAccount(type: FinancialAccountType.CreditCard, statementClosingDay: day);

        Assert.Equal(day, account.StatementClosingDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Constructor_WithOutOfRangeStatementClosingDay_ThrowsArgumentOutOfRangeException(int day)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(
            type: FinancialAccountType.CreditCard,
            statementClosingDay: day));

        Assert.Equal("statementClosingDay", ex.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsNullPaymentDueDay()
    {
        var account = CreateAccount(type: FinancialAccountType.CreditCard, paymentDueDay: null);

        Assert.Null(account.PaymentDueDay);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    public void Constructor_AcceptsBoundaryPaymentDueDay(int day)
    {
        var account = CreateAccount(type: FinancialAccountType.CreditCard, paymentDueDay: day);

        Assert.Equal(day, account.PaymentDueDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Constructor_WithOutOfRangePaymentDueDay_ThrowsArgumentOutOfRangeException(int day)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(
            type: FinancialAccountType.CreditCard,
            paymentDueDay: day));

        Assert.Equal("paymentDueDay", ex.ParamName);
    }

    [Fact]
    public void Constructor_TrimsAccountName()
    {
        var account = CreateAccount(name: "  Savings  ");

        Assert.Equal("Savings", account.Name);
    }

    [Fact]
    public void Constructor_NormalizesCurrency()
    {
        var account = CreateAccount(currency: " usd ");

        Assert.Equal("USD", account.Currency);
    }

    [Fact]
    public void Constructor_PreservesTimestampInactiveStateAndOpeningBalance()
    {
        var account = CreateAccount(openingBalance: 1.23m, isActive: false);

        Assert.Equal(CreatedAt, account.CreatedAt);
        Assert.False(account.IsActive);
        Assert.Equal(1.23m, account.OpeningBalance);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    [InlineData(1.2345)]
    [InlineData(10.999)]
    public void Constructor_WithExcessivePrecisionOpeningBalance_ThrowsArgumentOutOfRangeException(decimal openingBalance)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(openingBalance: openingBalance));

        Assert.Equal("openingBalance", ex.ParamName);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    [InlineData(100.999)]
    public void Constructor_WithExcessivePrecisionCreditLimit_ThrowsArgumentOutOfRangeException(decimal creditLimit)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(
            type: FinancialAccountType.CreditCard,
            creditLimit: creditLimit));

        Assert.Equal("creditLimit", ex.ParamName);
    }

    [Theory]
    [InlineData(10000000000000000.00)]
    [InlineData(-10000000000000000.00)]
    public void Constructor_WithOpeningBalanceExceedingDecimal182Range_ThrowsArgumentOutOfRangeException(decimal openingBalance)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(openingBalance: openingBalance));

        Assert.Equal("openingBalance", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithTrailingZerosOpeningBalance_PreservesAmount()
    {
        var account = CreateAccount(openingBalance: 1.100m);

        Assert.Equal(1.100m, account.OpeningBalance);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(id: Guid.Empty));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_WithUndefinedType_ThrowsArgumentOutOfRangeException(int type)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(type: (FinancialAccountType)type));

        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOwner_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => CreateAccount(ownerId: null!));

        Assert.Equal("ownerId", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankOwner_ThrowsArgumentException(string ownerId)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(ownerId: ownerId));

        Assert.Equal("ownerId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => CreateAccount(name: null!));

        Assert.Equal("name", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_ThrowsArgumentException(string name)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(name: name));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCurrency_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => CreateAccount(currency: null!));

        Assert.Equal("currency", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("XX")]
    [InlineData("XXX")]
    public void Constructor_WithInvalidCurrency_ThrowsArgumentException(string currency)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(currency: currency));

        Assert.Equal("currency", ex.ParamName);
    }

    private static FinancialAccount CreateAccount(
        Guid? id = null,
        string ownerId = OwnerId,
        string name = "Savings",
        FinancialAccountType type = FinancialAccountType.Cash,
        string currency = "USD",
        decimal openingBalance = 0m,
        bool isActive = true,
        DateTimeOffset? createdAt = null,
        decimal? creditLimit = null,
        int? statementClosingDay = null,
        int? paymentDueDay = null) => new(
            id ?? AccountId,
            ownerId,
            name,
            type,
            currency,
            openingBalance,
            isActive,
            createdAt ?? CreatedAt,
            creditLimit,
            statementClosingDay,
            paymentDueDay);
}
