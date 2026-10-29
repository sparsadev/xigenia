using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public sealed record CreateMoneyMovementRequest(
    MovementDirection Direction,
    decimal Amount,
    string Currency,
    DateOnly MovementDate,
    string? Note,
    Guid? ObligationId,
    Guid? FinancialAccountId,
    MovementClassification Classification = MovementClassification.Unclassified);

public sealed record MoneyMovementResponse(
    Guid Id,
    Guid? ObligationId,
    Guid? FinancialAccountId,
    Guid? TransferGroupId,
    MovementDirection Direction,
    decimal Amount,
    string Currency,
    DateOnly MovementDate,
    string? Note,
    MovementClassification Classification);
