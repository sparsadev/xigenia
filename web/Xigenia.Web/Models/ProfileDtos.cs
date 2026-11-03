namespace Xigenia.Web.Models;

public sealed record ProfileResponse(
    string Email,
    bool IsEmailConfirmed);
