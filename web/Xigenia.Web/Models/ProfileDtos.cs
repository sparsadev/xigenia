namespace Xigenia.Web.Models;

public sealed record ProfileResponse(
    string Email,
    bool IsEmailConfirmed);

public sealed record ChangePasswordRequest(
    string OldPassword,
    string NewPassword);

public sealed record RequestEmailChangeRequest(
    string NewEmail);
