using System.Text.Json.Serialization;

namespace Xigenia.Web.Models;

public enum FinancialAccountTypeDto
{
    Cash = 1,
    BankAccount = 2,
    DigitalWallet = 3,
    CreditCard = 4
}

public enum MovementDirectionDto
{
    Pagar = 1,
    Cobrar = 2
}

public enum MovementClassificationDto
{
    Unclassified = 0,
    Income = 1,
    Expense = 2,
    InternalTransfer = 3
}

public sealed record CreateFinancialAccountRequest(
    string Name,
    FinancialAccountTypeDto Type,
    string Currency,
    decimal OpeningBalance,
    decimal? CreditLimit = null,
    int? StatementClosingDay = null,
    int? PaymentDueDay = null);

public sealed record FinancialAccountResponse(
    Guid Id,
    string Name,
    FinancialAccountTypeDto Type,
    string Currency,
    decimal OpeningBalance,
    bool IsActive,
    DateTimeOffset CreatedAt,
    decimal? CreditLimit,
    int? StatementClosingDay,
    int? PaymentDueDay,
    decimal CurrentBalance,
    decimal? AvailableCredit,
    int LinkedMovementCount);

public sealed record CreateMoneyMovementRequest(
    MovementDirectionDto Direction,
    decimal Amount,
    string Currency,
    DateOnly MovementDate,
    string? Note,
    Guid? ObligationId,
    Guid? FinancialAccountId,
    MovementClassificationDto Classification = MovementClassificationDto.Unclassified,
    Guid? FinancialCategoryId = null);

public sealed record MoneyMovementResponse(
    Guid Id,
    Guid? ObligationId,
    Guid? FinancialAccountId,
    Guid? TransferGroupId,
    MovementDirectionDto Direction,
    decimal Amount,
    string Currency,
    DateOnly MovementDate,
    string? Note,
    MovementClassificationDto Classification,
    Guid? FinancialCategoryId);

public sealed record MoneyMovementCurrencySummary(
    string Currency,
    decimal Income,
    decimal Expenses,
    decimal Net,
    int MovementCount);

public sealed record MoneyMovementSummaryResponse(
    IReadOnlyList<MoneyMovementCurrencySummary> Totals,
    int InternalTransferCount,
    int UnclassifiedCount);

public sealed record ApiError(
    string Title,
    string? Detail,
    int StatusCode,
    Dictionary<string, string[]>? ValidationErrors);
