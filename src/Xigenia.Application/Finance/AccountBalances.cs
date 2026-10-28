using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public static class AccountBalances
{
    public static decimal CurrentBalance(
        FinancialAccountType type,
        decimal openingBalance,
        decimal cobrarTotal,
        decimal pagarTotal) =>
        type == FinancialAccountType.CreditCard
            ? openingBalance + pagarTotal - cobrarTotal
            : openingBalance + cobrarTotal - pagarTotal;

    public static decimal? AvailableCredit(decimal? creditLimit, decimal currentBalance) =>
        creditLimit.HasValue ? creditLimit.Value - currentBalance : null;
}
