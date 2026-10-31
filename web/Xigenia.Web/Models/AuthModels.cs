namespace Xigenia.Web.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(string Email, string Password);

public sealed record AccessTokenResponse(
    string TokenType,
    string AccessToken,
    int ExpiresIn,
    string RefreshToken);

public sealed record ProblemDetails(
    string? Title,
    string? Detail,
    int? Status,
    Dictionary<string, string[]>? Errors);
