namespace Xigenia.Web.Models;

public enum ObligationKindDto
{
    PorPagar = 1,
    PorCobrar = 2
}

public sealed record CreateObligationRequest(
    ObligationKindDto Kind,
    string Title,
    decimal Amount,
    string Currency,
    DateOnly IssueDate,
    DateOnly? DueDate);

public sealed record ObligationResponse(
    Guid Id,
    ObligationKindDto Kind,
    string Title,
    decimal Amount,
    string Currency,
    DateOnly IssueDate,
    DateOnly? DueDate,
    decimal OutstandingAmount);
