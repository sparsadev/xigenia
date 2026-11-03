namespace Xigenia.Web.Models;

public sealed record FinancialCategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    MovementClassificationDto Classification,
    bool IsActive,
    DateTimeOffset CreatedAt,
    bool IsGlobal);

public sealed record CreateFinancialCategoryRequest(
    string Name,
    MovementClassificationDto Classification,
    Guid? ParentCategoryId);

public sealed record RenameFinancialCategoryRequest(
    string Name);

public sealed record SetFinancialCategoryActivationRequest(
    bool IsActive);
