namespace Xigenia.Web.Models;

public sealed record FinancialExplanationRequest(
    string Question,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? FinancialAccountId = null);

public sealed record FinancialExplanationResponse(
    string Explanation,
    string Model,
    string Provider,
    DateTimeOffset ContextTimestamp,
    DateTimeOffset GeneratedAt,
    bool IsAuthoritativeFinancialCalculation,
    string Disclaimer);
