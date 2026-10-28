using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public sealed record CreateFinancialAccountRequest(
    string Name,
    FinancialAccountType Type,
    string Currency,
    decimal OpeningBalance,
    decimal? CreditLimit,
    int? StatementClosingDay,
    int? PaymentDueDay);

public sealed record FinancialAccountResponse(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    string Currency,
    decimal OpeningBalance,
    bool IsActive,
    DateTimeOffset CreatedAt,
    decimal? CreditLimit,
    int? StatementClosingDay,
    int? PaymentDueDay);
