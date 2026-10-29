using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public sealed record CreateFinancialCategoryRequest(
    string Name,
    MovementClassification Classification,
    Guid? ParentCategoryId);

public sealed record RenameFinancialCategoryRequest(
    string Name);

public sealed record SetFinancialCategoryActivationRequest(
    bool IsActive);

public sealed record FinancialCategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    MovementClassification Classification,
    bool IsActive,
    DateTimeOffset CreatedAt,
    bool IsGlobal);
