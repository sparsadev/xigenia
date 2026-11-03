namespace Xigenia.Web.Models;

public sealed record MovementQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? FinancialAccountId = null,
    Guid? FinancialCategoryId = null,
    string? Classification = null,
    string Sort = "movementDate",
    string SortDir = "desc")
{
    public bool IsReversed => From.HasValue && To.HasValue && From.Value > To.Value;

    public bool HasClassificationFilter => !string.IsNullOrWhiteSpace(Classification);

    public static IReadOnlySet<string> AllowedClassifications { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Unclassified",
        "Income",
        "Expense",
        "InternalTransfer"
    };

    public bool IsClassificationValid => !HasClassificationFilter || AllowedClassifications.Contains(Classification!);

    public string ToQueryString(int page, int pageSize)
    {
        var parts = new List<string>();
        if (From.HasValue) parts.Add($"from={From.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}");
        if (To.HasValue) parts.Add($"to={To.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}");
        if (FinancialAccountId.HasValue && FinancialAccountId.Value != Guid.Empty) parts.Add($"financialAccountId={FinancialAccountId.Value:D}");
        if (FinancialCategoryId.HasValue && FinancialCategoryId.Value != Guid.Empty) parts.Add($"financialCategoryId={FinancialCategoryId.Value:D}");
        if (HasClassificationFilter) parts.Add($"classification={Uri.EscapeDataString(Classification!)}");
        parts.Add($"page={page}");
        parts.Add($"pageSize={pageSize}");
        parts.Add($"sort={Uri.EscapeDataString(Sort)}");
        parts.Add($"sortDir={Uri.EscapeDataString(SortDir)}");
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    public string ToSummaryQueryString()
    {
        var parts = new List<string>();
        if (From.HasValue) parts.Add($"from={From.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}");
        if (To.HasValue) parts.Add($"to={To.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}");
        if (FinancialAccountId.HasValue && FinancialAccountId.Value != Guid.Empty) parts.Add($"financialAccountId={FinancialAccountId.Value:D}");
        if (FinancialCategoryId.HasValue && FinancialCategoryId.Value != Guid.Empty) parts.Add($"financialCategoryId={FinancialCategoryId.Value:D}");
        if (HasClassificationFilter) parts.Add($"classification={Uri.EscapeDataString(Classification!)}");
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    public string Identity(int page, int pageSize) =>
        $"{From?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}|{To?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}|{FinancialAccountId}|{FinancialCategoryId}|{Classification}|{Sort}|{SortDir}|{page}|{pageSize}";

    public string FilterIdentity =>
        $"{From?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}|{To?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}|{FinancialAccountId}|{FinancialCategoryId}|{Classification}";
}
