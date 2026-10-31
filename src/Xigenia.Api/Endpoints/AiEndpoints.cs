using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xigenia.Application.Finance;
using Xigenia.Domain;
using Xigenia.Infrastructure.MetaAi;

namespace Xigenia.Api.Endpoints;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/ai").RequireAuthorization();
        group.MapPost("/financial-explanation", HandleFinancialExplanation);
        return routes;
    }

    private sealed record FinancialExplanationRequest(
        string Question,
        DateOnly? From = null,
        DateOnly? To = null,
        Guid? FinancialAccountId = null);

    private sealed record FinancialExplanationResponse(
        string Explanation,
        string Model,
        string Provider,
        DateTimeOffset ContextTimestamp,
        DateTimeOffset GeneratedAt,
        bool IsAuthoritativeFinancialCalculation,
        string Disclaimer);

    private static async Task<IResult> HandleFinancialExplanation(
        FinancialExplanationRequest request,
        ClaimsPrincipal user,
        FinanceService finance,
        IMetaAiClient meta,
        IOptions<MetaAiOptions> metaOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Xigenia.Api.Ai");

        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return Results.Problem(detail: "Question is required.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid request.");
        }

        var question = request.Question.Trim();
        if (question.Length < 3 || question.Length > 1000)
        {
            return Results.Problem(detail: "Question must be between 3 and 1000 characters.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid question.");
        }

        // Validate account id if supplied
        if (request.FinancialAccountId.HasValue && request.FinancialAccountId.Value == Guid.Empty)
        {
            return Results.Problem(detail: "Financial account id is invalid.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid request.");
        }

        // Validate period
        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            return Results.Problem(detail: "'from' must not be after 'to'.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid period.");
        }

        // Build deterministic filter reusing existing contract
        var filter = new MovementFilter(request.From, request.To, request.FinancialAccountId, null, null);

        // Distinguish missing local configuration from upstream 503 before provider call
        if (string.IsNullOrWhiteSpace(metaOptions.Value.ApiKey))
        {
            logger.LogWarning("AI provider not configured (missing ApiKey)");
            return Results.Problem(detail: "AI provider is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable, title: "Provider not configured.");
        }

        IReadOnlyList<FinancialAccountResponse> accounts;
        MoneyMovementSummaryResponse summary;
        IReadOnlyList<FinancialCategoryResponse> categories = Array.Empty<FinancialCategoryResponse>();
        IReadOnlyList<MoneyMovementResponse> allMovements = Array.Empty<MoneyMovementResponse>();
        IReadOnlyList<MoneyMovementResponse> recentMovements = Array.Empty<MoneyMovementResponse>();
        var contextTimestamp = DateTimeOffset.UtcNow;

        try
        {
            accounts = await finance.ListFinancialAccountsAsync(ownerId, cancellationToken);
            summary = await finance.SummarizeMoneyMovementsAsync(ownerId, filter, cancellationToken);
            // Category information required for A2/A3 where contracts support it
            categories = await finance.ListFinancialCategoriesAsync(ownerId, cancellationToken);

            // Complete category aggregation: retrieve all filtered movements via bounded pagination
            const int pageSize = 100;
            const int maxMovements = 1000;
            const int maxPages = 10;
            var aggregated = new List<MoneyMovementResponse>();
            int totalCount = 0;
            bool complete = false;
            for (int page = 1; page <= maxPages; page++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (pageItems, count) = await finance.ListMoneyMovementsAsync(ownerId, filter, new MovementPaging(page, pageSize), new MovementSorting(MovementSortField.MovementDate, MovementSortDirection.Asc), cancellationToken);
                if (page == 1) totalCount = count;
                aggregated.AddRange(pageItems);
                if (aggregated.Count >= totalCount)
                {
                    complete = true;
                    break;
                }
                if (pageItems.Count == 0)
                {
                    break;
                }
                if (aggregated.Count > maxMovements)
                {
                    break;
                }
            }
            if (!complete)
            {
                // Explicit recoverable failure rather than labeling partial sums as period totals
                if (totalCount > maxMovements)
                {
                    logger.LogWarning("AI aggregation period too large: {TotalCount} > {Max}", totalCount, maxMovements);
                    return Results.Problem(detail: "Financial period too large to aggregate category totals. Narrow date range or account filter.", statusCode: StatusCodes.Status422UnprocessableEntity, title: "Period too large.");
                }
                logger.LogWarning("AI aggregation incomplete: fetched {Fetched}/{Total}", aggregated.Count, totalCount);
                return Results.Problem(detail: "Unable to aggregate complete category totals for the requested period. Try again or narrow the filter.", statusCode: StatusCodes.Status422UnprocessableEntity, title: "Incomplete aggregation");
            }
            allMovements = aggregated;
            // Keep detailed examples bounded independently (50 most recent for prompt)
            recentMovements = allMovements.OrderByDescending(m => m.MovementDate).Take(50).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid financial query.");
        }

        // Filter accounts for prompt if specific account requested
        var relevantAccounts = request.FinancialAccountId.HasValue
            ? accounts.Where(a => a.Id == request.FinancialAccountId.Value).ToList()
            : accounts;

        string prompt;
        try
        {
            prompt = BuildPrompt(question, relevantAccounts, summary, categories, allMovements, recentMovements, filter, contextTimestamp, metaOptions.Value.Model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Essential financial aggregates"))
        {
            logger.LogWarning(ex, "Essential aggregates exceed prompt budget");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity, title: "Prompt budget exceeded");
        }

        MetaAiResult metaResult;
        try
        {
            metaResult = await meta.CreateResponseAsync(prompt, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Results.Problem(detail: "Request was cancelled.", statusCode: 499, title: "Cancelled.");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "AI request timeout (OperationCanceled)");
            return Results.Problem(detail: "AI provider timed out.", statusCode: StatusCodes.Status504GatewayTimeout, title: "Provider timeout.");
        }
        catch (MetaAiException ex) when (ex.StatusCode == 504)
        {
            logger.LogWarning(ex, "AI provider timeout (504)");
            return Results.Problem(detail: "AI provider timed out.", statusCode: StatusCodes.Status504GatewayTimeout, title: "Provider timeout.");
        }
        catch (MetaAiException ex)
        {
            // Distinguish origins explicitly via status code, not message: upstream 503 must map to 502 (pre-call missing-config already handled as 503)
            logger.LogWarning(ex, "AI provider failure {Status}", ex.StatusCode);
            return Results.Problem(detail: "AI provider error.", statusCode: StatusCodes.Status502BadGateway, title: "Provider error.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected AI error");
            return Results.Problem(detail: "AI provider error.", statusCode: StatusCodes.Status502BadGateway, title: "Provider error.");
        }

        if (metaResult.IsIncomplete)
        {
            logger.LogWarning("AI response incomplete: {Status}", metaResult.Status);
            return Results.Problem(detail: "AI provider returned incomplete response.", statusCode: StatusCodes.Status502BadGateway, title: "Provider incomplete.");
        }

        if (string.IsNullOrWhiteSpace(metaResult.Text))
        {
            return Results.Problem(detail: "AI provider returned empty response.", statusCode: StatusCodes.Status502BadGateway, title: "Provider error.");
        }

        var response = new FinancialExplanationResponse(
            Explanation: metaResult.Text.Trim(),
            Model: metaResult.Model,
            Provider: "meta",
            ContextTimestamp: contextTimestamp,
            GeneratedAt: DateTimeOffset.UtcNow,
            IsAuthoritativeFinancialCalculation: false,
            Disclaimer: "This AI explanation is not an authoritative financial calculation. Deterministic backend values are authoritative. Verify with your financial records."
        );

        return Results.Ok(response);
    }

    private static string BuildPrompt(
        string question,
        IReadOnlyList<FinancialAccountResponse> accounts,
        MoneyMovementSummaryResponse summary,
        IReadOnlyList<FinancialCategoryResponse> categories,
        IReadOnlyList<MoneyMovementResponse> allMovements,
        IReadOnlyList<MoneyMovementResponse> recentMovements,
        MovementFilter filter,
        DateTimeOffset contextTimestamp,
        string model)
    {
        var sb = new StringBuilder(4096);
        var header = new StringBuilder();
        header.AppendLine("You are Xigenia, a helpful personal finance assistant. Explain only from verified records below. Do not invent numbers. Keep explanation concise, grounded, and separate facts from suggestions.");
        header.AppendLine();
        header.AppendLine($"Context timestamp: {contextTimestamp:O}");
        header.AppendLine($"Deterministic financial context (owner-scoped, authoritative):");

        var essentialBody = new StringBuilder();
        var optionalBody = new StringBuilder();

        if (accounts.Count == 0)
        {
            essentialBody.AppendLine("- No financial accounts recorded.");
        }
        else
        {
            essentialBody.AppendLine($"- Accounts ({accounts.Count}):");
            foreach (var a in accounts.Take(20))
            {
                // Preserve account-type and debt semantics: CreditCard outstanding is debt, not available funds
                if (a.Type == FinancialAccountType.CreditCard)
                {
                    essentialBody.AppendLine($"  * {a.Name} [{a.Currency}] CreditCard Limit={a.CreditLimit:0.00} OutstandingDebt={a.CurrentBalance:0.00} AvailableCredit={a.AvailableCredit:0.00} OpeningDebt={a.OpeningBalance:0.00} Active={a.IsActive}");
                }
                else
                {
                    essentialBody.AppendLine($"  * {a.Name} [{a.Currency}] {a.Type} Opening={a.OpeningBalance:0.00} Current={a.CurrentBalance:0.00} Active={a.IsActive}");
                }
            }
            if (accounts.Count > 20) essentialBody.AppendLine($"  * ... and {accounts.Count - 20} more");
        }

        essentialBody.AppendLine("- Summary (income/expense/net from deterministic summary, transfers excluded):");
        if (summary.Totals.Count == 0)
        {
            essentialBody.AppendLine("  * No income/expense movements in selected period.");
        }
        else
        {
            foreach (var t in summary.Totals)
            {
                essentialBody.AppendLine($"  * {t.Currency}: Income={t.Income:0.00} Expenses={t.Expenses:0.00} Net={t.Net:0.00} Count={t.MovementCount}");
            }
        }
        essentialBody.AppendLine($"  Internal transfers excluded: {summary.InternalTransferCount}, Unclassified: {summary.UnclassifiedCount}");

        // Essential per-category aggregates (must not be silently omitted)
        if (allMovements.Count > 0)
        {
            var catByIdForTotals = categories.ToDictionary(c => c.Id, c => c.Name);
            var expenseByCat = allMovements
                .Where(m => m.Classification == MovementClassification.Expense && m.FinancialCategoryId.HasValue && !m.ObligationId.HasValue)
                .GroupBy(m => new { m.FinancialCategoryId, m.Currency })
                .Select(g => (Name: catByIdForTotals.TryGetValue(g.Key.FinancialCategoryId!.Value, out var name) ? name : g.Key.FinancialCategoryId.ToString(), Total: g.Sum(x => x.Amount), Currency: g.Key.Currency))
                .OrderByDescending(x => x.Total)
                .ToList();
            if (expenseByCat.Count > 0)
            {
                essentialBody.AppendLine("- Per-category expense totals (deterministic, from movements in period, settlement-excluded, by currency):");
                foreach (var e in expenseByCat.Take(10))
                {
                    essentialBody.AppendLine($"  * {e.Name} [{e.Currency}]: {e.Total:0.00} {e.Currency}");
                }
            }
        }

        if (filter.From.HasValue || filter.To.HasValue || filter.FinancialAccountId.HasValue)
        {
            essentialBody.AppendLine($"- Period filter: from={filter.From?.ToString("yyyy-MM-dd") ?? "none"} to={filter.To?.ToString("yyyy-MM-dd") ?? "none"} account={filter.FinancialAccountId?.ToString() ?? "all"}");
        }

        // Optional: category descriptions and recent movement examples (prioritized after essential aggregates)
        if (categories.Count > 0)
        {
            optionalBody.AppendLine($"- Categories ({categories.Count} deterministic, owner + global):");
            foreach (var c in categories.Take(30))
            {
                var scope = c.IsGlobal ? "global" : "owner";
                optionalBody.AppendLine($"  * {c.Name} [{c.Classification}] ({scope})");
            }
            if (categories.Count > 30) optionalBody.AppendLine($"  * ... and {categories.Count - 30} more");
        }

        if (recentMovements.Count > 0)
        {
            optionalBody.AppendLine($"- Recent movements ({recentMovements.Count} deterministic, with category linkage):");
            var catById = categories.ToDictionary(c => c.Id, c => c.Name);
            foreach (var m in recentMovements.Take(50))
            {
                var catName = m.FinancialCategoryId.HasValue && catById.TryGetValue(m.FinancialCategoryId.Value, out var n) ? n : "unclassified";
                optionalBody.AppendLine($"  * {m.MovementDate:yyyy-MM-dd} {m.Classification} {catName} {m.Amount:0.00} {m.Currency} [{m.Direction}]");
            }
        }

        var footer = new StringBuilder();
        footer.AppendLine();
        footer.AppendLine($"User question: {question}");
        footer.AppendLine();
        footer.AppendLine("Instructions: Use only the numbers above. Do not recalculate balances. State that figures are from deterministic backend. If no records, say so. Keep answer under 500 words. Clearly separate facts from recommendations and label AI output as not authoritative.");

        // Preserve essential aggregates: prioritize header + essentialBody + footer over optionalBody
        var headerStr = header.ToString();
        var essentialStr = essentialBody.ToString();
        var optionalStr = optionalBody.ToString();
        var footerStr = footer.ToString();
        const int maxLen = 6000;
        const string truncationMarker = "\n...[financial context truncated]...\n";
        var essentialLen = headerStr.Length + essentialStr.Length + footerStr.Length;
        // If essential facts alone exceed budget, return explicit failure rather than misleading partial
        if (essentialLen > maxLen)
        {
            throw new InvalidOperationException($"Essential financial aggregates ({essentialLen} chars) exceed prompt budget ({maxLen}). Narrow filter or reduce categories.");
        }
        var totalLen = essentialLen + optionalStr.Length;
        string bodyStr;
        if (totalLen > maxLen)
        {
            var availableForOptional = maxLen - essentialLen - truncationMarker.Length;
            if (availableForOptional < 0) availableForOptional = 0;
            if (optionalStr.Length > availableForOptional)
            {
                optionalStr = optionalStr[..availableForOptional] + truncationMarker;
            }
            bodyStr = essentialStr + optionalStr;
        }
        else
        {
            bodyStr = essentialStr + optionalStr;
        }

        sb.Append(headerStr);
        sb.Append(bodyStr);
        sb.Append(footerStr);
        return sb.ToString();
    }
}
