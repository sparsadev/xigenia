namespace Xigenia.Web.Models;

public sealed record FinancialCategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    MovementClassificationDto Classification,
    bool IsActive,
    DateTimeOffset CreatedAt,
    bool IsGlobal);
