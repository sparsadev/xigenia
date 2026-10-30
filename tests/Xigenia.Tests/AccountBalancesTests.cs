using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class AccountBalancesTests
{
    [Theory]
    [InlineData(FinancialAccountType.Cash)]
    [InlineData(FinancialAccountType.BankAccount)]
    [InlineData(FinancialAccountType.DigitalWallet)]
    public void CurrentBalance_ForAssetAccounts_AddsInflowsAndSubtractsOutflows(FinancialAccountType type)
    {
        var balance = AccountBalances.CurrentBalance(type, 100m, 20m, 30m);

        Assert.Equal(90m, balance);
    }

    [Fact]
    public void CurrentBalance_ForCreditCard_AddsOutflowsAndSubtractsInflows()
    {
        var balance = AccountBalances.CurrentBalance(FinancialAccountType.CreditCard, 100m, 20m, 30m);

        Assert.Equal(110m, balance);
    }

    [Fact]
    public void CurrentBalance_UsesExactDecimalArithmeticForAssetAccounts()
    {
        var balance = AccountBalances.CurrentBalance(FinancialAccountType.Cash, 0.1m, 0.2m, 0m);

        Assert.Equal(0.3m, balance);
    }

    [Fact]
    public void CurrentBalance_UsesExactDecimalArithmeticForCreditCards()
    {
        var balance = AccountBalances.CurrentBalance(FinancialAccountType.CreditCard, 0.1m, 0.2m, 0m);

        Assert.Equal(-0.1m, balance);
    }

    [Fact]
    public void CurrentBalance_AllowsNegativeCreditCardDebtWithoutClamping()
    {
        var balance = AccountBalances.CurrentBalance(FinancialAccountType.CreditCard, 100m, 150m, 0m);

        Assert.Equal(-50m, balance);
    }

    [Theory]
    [MemberData(nameof(AvailableCreditCases))]
    public void AvailableCredit_SubtractsBalanceFromLimit(decimal? creditLimit, decimal currentBalance, decimal? expected)
    {
        var available = AccountBalances.AvailableCredit(creditLimit, currentBalance);

        Assert.Equal(expected, available);
    }

    public static TheoryData<decimal?, decimal, decimal?> AvailableCreditCases => new()
    {
        { null, 100m, null },
        { 1000m, 250m, 750m },
        { 100m, 150m, -50m },
        { 100m, -20m, 120m },
    };
}
