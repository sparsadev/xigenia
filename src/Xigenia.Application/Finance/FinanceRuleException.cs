namespace Xigenia.Application.Finance;

public sealed class FinanceRuleException : Exception
{
    public FinanceRuleException(string message)
        : base(message)
    {
    }
}
