using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class MovementRulesTests
{
    private static readonly Guid SourceAccountId = new("88888888-8888-8888-8888-888888888888");
    private static readonly Guid DestinationAccountId = new("99999999-9999-9999-9999-999999999999");

    [Theory]
    [InlineData(ObligationKind.PorPagar, MovementDirection.Pagar)]
    [InlineData(ObligationKind.PorCobrar, MovementDirection.Cobrar)]
    public void AssertCompatible_WithMatchingKindAndDirection_DoesNotThrow(
        ObligationKind kind,
        MovementDirection direction)
    {
        var exception = Record.Exception(() => MovementRules.AssertCompatible(kind, direction));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(ObligationKind.PorPagar, MovementDirection.Cobrar)]
    [InlineData(ObligationKind.PorCobrar, MovementDirection.Pagar)]
    public void AssertCompatible_WithMismatchedKindAndDirection_ThrowsFinanceRuleException(
        ObligationKind kind,
        MovementDirection direction)
    {
        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertCompatible(kind, direction));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void AssertCompatible_WithUndefinedKind_ThrowsFinanceRuleException(int kind)
    {
        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertCompatible(
            (ObligationKind)kind,
            MovementDirection.Pagar));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void AssertCompatible_WithUndefinedDirection_ThrowsFinanceRuleException(int direction)
    {
        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertCompatible(
            ObligationKind.PorPagar,
            (MovementDirection)direction));
    }

    [Fact]
    public void AssertSameCurrency_WithEqualCodes_DoesNotThrow()
    {
        var exception = Record.Exception(() => MovementRules.AssertSameCurrency("USD", "USD"));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("USD", "EUR")]
    [InlineData("usd", "USD")]
    [InlineData(" USD ", "USD")]
    public void AssertSameCurrency_WithDifferentCodes_ThrowsFinanceRuleException(
        string obligationCurrency,
        string movementCurrency)
    {
        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertSameCurrency(obligationCurrency, movementCurrency));
    }

    [Fact]
    public void AssertSameAccountCurrency_WithEqualCodes_DoesNotThrow()
    {
        var exception = Record.Exception(() => MovementRules.AssertSameAccountCurrency("USD", "USD"));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("USD", "EUR")]
    [InlineData("usd", "USD")]
    [InlineData(" USD ", "USD")]
    public void AssertSameAccountCurrency_WithDifferentCodes_ThrowsFinanceRuleException(
        string accountCurrency,
        string movementCurrency)
    {
        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertSameAccountCurrency(accountCurrency, movementCurrency));
    }

    [Fact]
    public void AssertWithinBalance_WithAmountsInsideBalance_DoesNotThrow()
    {
        var first = Record.Exception(() => MovementRules.AssertWithinBalance(1m, 0.7m, 0.29m));
        var boundary = Record.Exception(() => MovementRules.AssertWithinBalance(1m, 0.7m, 0.30m));

        Assert.Null(first);
        Assert.Null(boundary);
    }

    [Fact]
    public void AssertWithinBalance_WithAmountAboveBalance_ThrowsFinanceRuleException()
    {
        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertWithinBalance(1m, 0.7m, 0.31m));
    }

    [Theory]
    [InlineData(100, 20, 80)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 120, -20)]
    public void Outstanding_SubtractsSettledSumFromTotal(int total, int settledSum, int expected)
    {
        var outstanding = MovementRules.Outstanding(total, settledSum);

        Assert.Equal((decimal)expected, outstanding);
    }

    [Fact]
    public void AssertAccountActive_WithActiveAccount_DoesNotThrow()
    {
        var account = CreateAccount(isActive: true);

        var exception = Record.Exception(() => MovementRules.AssertAccountActive(account));

        Assert.Null(exception);
    }

    [Fact]
    public void AssertAccountActive_WithInactiveAccount_ThrowsFinanceRuleException()
    {
        var account = CreateAccount(isActive: false);

        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertAccountActive(account));
    }

    [Fact]
    public void AssertAccountActive_WithNullAccount_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => MovementRules.AssertAccountActive(null!));

        Assert.Equal("account", ex.ParamName);
    }

    [Fact]
    public void AssertDistinctAccounts_WithDistinctIds_DoesNotThrow()
    {
        var exception = Record.Exception(() => MovementRules.AssertDistinctAccounts(SourceAccountId, DestinationAccountId));

        Assert.Null(exception);
    }

    [Fact]
    public void AssertDistinctAccounts_WithIdenticalIds_ThrowsFinanceRuleException()
    {
        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertDistinctAccounts(SourceAccountId, SourceAccountId));
    }

    [Theory]
    [InlineData(FinancialAccountType.Cash)]
    [InlineData(FinancialAccountType.BankAccount)]
    [InlineData(FinancialAccountType.DigitalWallet)]
    public void AssertAssetTransferSource_WithAssetType_DoesNotThrow(FinancialAccountType sourceType)
    {
        var exception = Record.Exception(() => MovementRules.AssertAssetTransferSource(sourceType));

        Assert.Null(exception);
    }

    [Fact]
    public void AssertAssetTransferSource_WithCreditCard_ThrowsFinanceRuleException()
    {
        Assert.Throws<FinanceRuleException>(() => MovementRules.AssertAssetTransferSource(FinancialAccountType.CreditCard));
    }

    private static FinancialAccount CreateAccount(bool isActive) => new(
        new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        "owner-1",
        "Checking",
        FinancialAccountType.BankAccount,
        "USD",
        0m,
        isActive,
        new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero));
}
