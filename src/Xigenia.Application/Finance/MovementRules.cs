using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public static class MovementRules
{
    public static void AssertCompatible(ObligationKind kind, MovementDirection direction)
    {
        var compatible = (kind, direction) switch
        {
            (ObligationKind.PorPagar, MovementDirection.Pagar) => true,
            (ObligationKind.PorCobrar, MovementDirection.Cobrar) => true,
            _ => false,
        };

        if (!compatible)
        {
            throw new FinanceRuleException($"Direction '{direction}' is not compatible with obligation kind '{kind}'.");
        }
    }

    public static void AssertSameCurrency(string obligationCurrency, string movementCurrency)
    {
        if (!string.Equals(obligationCurrency, movementCurrency, StringComparison.Ordinal))
        {
            throw new FinanceRuleException($"Movement currency '{movementCurrency}' does not match obligation currency '{obligationCurrency}'.");
        }
    }

    public static void AssertWithinBalance(decimal total, decimal settledSum, decimal newAmount)
    {
        if (settledSum + newAmount > total)
        {
            throw new FinanceRuleException("Movement amount exceeds the outstanding obligation balance.");
        }
    }

    public static decimal Outstanding(decimal total, decimal settledSum) => total - settledSum;

    public static void AssertAccountActive(FinancialAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!account.IsActive)
        {
            throw new FinanceRuleException($"Financial account '{account.Name}' is not active.");
        }
    }

    public static void AssertSameAccountCurrency(string accountCurrency, string movementCurrency)
    {
        if (!string.Equals(accountCurrency, movementCurrency, StringComparison.Ordinal))
        {
            throw new FinanceRuleException($"Movement currency '{movementCurrency}' does not match account currency '{accountCurrency}'.");
        }
    }

    public static void AssertDistinctAccounts(Guid sourceAccountId, Guid destinationAccountId)
    {
        if (sourceAccountId == destinationAccountId)
        {
            throw new FinanceRuleException("Source and destination accounts must be different.");
        }
    }

    public static void AssertAssetTransferSource(FinancialAccountType sourceType)
    {
        if (sourceType == FinancialAccountType.CreditCard)
        {
            throw new FinanceRuleException("Credit cards cannot be used as transfer sources.");
        }
    }
}
