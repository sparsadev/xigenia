using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public sealed record MovementFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? FinancialAccountId = null,
    Guid? FinancialCategoryId = null,
    MovementClassification? Classification = null);

public sealed record MovementPaging(
    int? Page = null,
    int? PageSize = null)
{
    public const int MaxPageSize = 200;
}

public enum MovementSortField
{
    MovementDate = 1,
}

public enum MovementSortDirection
{
    Asc = 1,
    Desc = 2,
}

public sealed record MovementSorting(
    MovementSortField Sort = MovementSortField.MovementDate,
    MovementSortDirection SortDirection = MovementSortDirection.Asc);

public sealed record MoneyMovementCurrencySummary(
    string Currency,
    decimal Income,
    decimal Expenses,
    decimal Net,
    int MovementCount);

public sealed record MoneyMovementSummaryResponse(
    IReadOnlyList<MoneyMovementCurrencySummary> Totals,
    int InternalTransferCount,
    int UnclassifiedCount);
