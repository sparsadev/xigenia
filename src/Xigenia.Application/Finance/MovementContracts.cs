using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public sealed record CreateMoneyMovementRequest(
    MovementDirection Direction,
    decimal Amount,
    string Currency,
    DateOnly MovementDate,
    string? Note,
    Guid? ObligationId);

public sealed record MoneyMovementResponse(
    Guid Id,
    Guid? ObligationId,
    MovementDirection Direction,
    decimal Amount,
    string Currency,
    DateOnly MovementDate,
    string? Note);
