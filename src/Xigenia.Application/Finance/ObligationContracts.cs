using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public sealed record CreateObligationRequest(
    ObligationKind Kind,
    string Title,
    decimal Amount,
    string Currency,
    DateOnly IssueDate,
    DateOnly? DueDate);

public sealed record ObligationResponse(
    Guid Id,
    ObligationKind Kind,
    string Title,
    decimal Amount,
    string Currency,
    DateOnly IssueDate,
    DateOnly? DueDate);
