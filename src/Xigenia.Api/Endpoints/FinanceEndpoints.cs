using System.Globalization;
using System.Security.Claims;
using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Api.Endpoints;

public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/finance").RequireAuthorization();
        group.MapPost("/obligations", CreateObligation);
        group.MapGet("/obligations", ListObligations);
        group.MapPost("/movements", CreateMoneyMovement);
        group.MapGet("/movements", ListMoneyMovements);
        group.MapGet("/movements/summary", SummarizeMoneyMovements);
        group.MapPost("/accounts", CreateFinancialAccount);
        group.MapGet("/accounts", ListFinancialAccounts);
        group.MapGet("/accounts/{id:guid}", GetFinancialAccount);
        group.MapPost("/transfers", CreateTransfer);
        group.MapGet("/categories", ListFinancialCategories);
        group.MapGet("/categories/{id:guid}", GetFinancialCategory);
        group.MapPost("/categories", CreateFinancialCategory);
        group.MapPut("/categories/{id:guid}", RenameFinancialCategory);
        group.MapPatch("/categories/{id:guid}/activation", SetFinancialCategoryActivation);
        return routes;
    }

    private static async Task<IResult> CreateObligation(
        CreateObligationRequest request,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await finance.CreateObligationAsync(ownerId, request, cancellationToken);
            return Results.Json(response, statusCode: StatusCodes.Status201Created);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid obligation.");
        }
    }

    private static async Task<IResult> ListObligations(
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var response = await finance.ListObligationsAsync(ownerId, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateMoneyMovement(
        CreateMoneyMovementRequest request,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await finance.CreateMoneyMovementAsync(ownerId, request, cancellationToken);
            return Results.Json(response, statusCode: StatusCodes.Status201Created);
        }
        catch (FinanceRuleException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid movement.");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Referenced resource not found.");
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid movement.");
        }
    }

    private static async Task<IResult> ListMoneyMovements(
        string? from,
        string? to,
        string? financialAccountId,
        string? financialCategoryId,
        string? classification,
        string? page,
        string? pageSize,
        string? sort,
        string? sortDir,
        ClaimsPrincipal user,
        FinanceService finance,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var filterError = ParseMovementFilter(from, to, financialAccountId, financialCategoryId, classification, out var filter);
        if (filterError is not null)
        {
            return filterError;
        }

        var pagingError = ParseMovementPaging(page, pageSize, out var paging);
        if (pagingError is not null)
        {
            return pagingError;
        }

        var sortingError = ParseMovementSorting(sort, sortDir, out var sorting);
        if (sortingError is not null)
        {
            return sortingError;
        }

        try
        {
            // No query parameters: preserve the exact legacy response path.
            if (httpContext.Request.Query.Count == 0)
            {
                var unfiltered = await finance.ListMoneyMovementsAsync(ownerId, cancellationToken);
                return Results.Ok(unfiltered);
            }

            var (movements, totalCount) = await finance.ListMoneyMovementsAsync(ownerId, filter, paging, sorting, cancellationToken);

            if (paging.Page.HasValue && paging.PageSize.HasValue)
            {
                httpContext.Response.Headers["X-Total-Count"] = totalCount.ToString(CultureInfo.InvariantCulture);
                httpContext.Response.Headers["X-Page"] = paging.Page.Value.ToString(CultureInfo.InvariantCulture);
                httpContext.Response.Headers["X-Page-Size"] = paging.PageSize.Value.ToString(CultureInfo.InvariantCulture);
            }

            return Results.Ok(movements);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid movement query.");
        }
    }

    private static async Task<IResult> SummarizeMoneyMovements(
        string? from,
        string? to,
        string? financialAccountId,
        string? financialCategoryId,
        string? classification,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var filterError = ParseMovementFilter(from, to, financialAccountId, financialCategoryId, classification, out var filter);
        if (filterError is not null)
        {
            return filterError;
        }

        try
        {
            var response = await finance.SummarizeMoneyMovementsAsync(ownerId, filter, cancellationToken);
            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid movement query.");
        }
    }

    private static IResult? ParseMovementFilter(
        string? from,
        string? to,
        string? financialAccountId,
        string? financialCategoryId,
        string? classification,
        out MovementFilter filter)
    {
        filter = new MovementFilter();

        if (!TryParseDateOnly(from, out var fromDate))
        {
            return MovementQueryProblem("Query parameter 'from' must be a date in yyyy-MM-dd format.");
        }

        if (!TryParseDateOnly(to, out var toDate))
        {
            return MovementQueryProblem("Query parameter 'to' must be a date in yyyy-MM-dd format.");
        }

        if (!TryParseGuid(financialAccountId, out var accountId))
        {
            return MovementQueryProblem("Query parameter 'financialAccountId' must be a valid GUID.");
        }

        if (!TryParseGuid(financialCategoryId, out var categoryId))
        {
            return MovementQueryProblem("Query parameter 'financialCategoryId' must be a valid GUID.");
        }

        MovementClassification? parsedClassification = null;
        if (classification is not null)
        {
            if (!Enum.TryParse<MovementClassification>(classification, ignoreCase: true, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                return MovementQueryProblem("Query parameter 'classification' must be one of: Unclassified, Income, Expense, InternalTransfer.");
            }

            parsedClassification = parsed;
        }

        filter = new MovementFilter(fromDate, toDate, accountId, categoryId, parsedClassification);
        return null;
    }

    private static IResult? ParseMovementPaging(string? page, string? pageSize, out MovementPaging paging)
    {
        paging = new MovementPaging();

        if (!TryParseInt32(page, out var parsedPage))
        {
            return MovementQueryProblem("Query parameter 'page' must be a valid integer.");
        }

        if (!TryParseInt32(pageSize, out var parsedPageSize))
        {
            return MovementQueryProblem("Query parameter 'pageSize' must be a valid integer.");
        }

        paging = new MovementPaging(parsedPage, parsedPageSize);
        return null;
    }

    private static IResult? ParseMovementSorting(string? sort, string? sortDir, out MovementSorting sorting)
    {
        sorting = new MovementSorting();

        if (sort is not null && !string.Equals(sort, "movementDate", StringComparison.OrdinalIgnoreCase))
        {
            return MovementQueryProblem("Query parameter 'sort' must be 'movementDate'.");
        }

        var direction = MovementSortDirection.Asc;
        if (sortDir is not null)
        {
            if (string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase))
            {
                direction = MovementSortDirection.Asc;
            }
            else if (string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase))
            {
                direction = MovementSortDirection.Desc;
            }
            else
            {
                return MovementQueryProblem("Query parameter 'sortDir' must be 'asc' or 'desc'.");
            }
        }

        sorting = new MovementSorting(MovementSortField.MovementDate, direction);
        return null;
    }

    private static bool TryParseDateOnly(string? value, out DateOnly? result)
    {
        result = null;
        if (value is null)
        {
            return true;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseGuid(string? value, out Guid? result)
    {
        result = null;
        if (value is null)
        {
            return true;
        }

        if (Guid.TryParse(value, out var parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseInt32(string? value, out int? result)
    {
        result = null;
        if (value is null)
        {
            return true;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    private static IResult MovementQueryProblem(string detail) =>
        Results.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest, title: "Invalid movement query.");

    private static async Task<IResult> CreateFinancialAccount(
        CreateFinancialAccountRequest request,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await finance.CreateFinancialAccountAsync(ownerId, request, cancellationToken);
            return Results.Created($"/api/finance/accounts/{response.Id}", response);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid account.");
        }
    }

    private static async Task<IResult> ListFinancialAccounts(
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var response = await finance.ListFinancialAccountsAsync(ownerId, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetFinancialAccount(
        Guid id,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var response = await finance.GetFinancialAccountAsync(ownerId, id, cancellationToken);
        if (response is null)
        {
            return Results.Problem(detail: "Account was not found.", statusCode: StatusCodes.Status404NotFound, title: "Account not found.");
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateTransfer(
        CreateTransferRequest request,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await finance.CreateTransferAsync(ownerId, request, cancellationToken);
            return Results.Json(response, statusCode: StatusCodes.Status201Created);
        }
        catch (FinanceRuleException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid transfer.");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Referenced resource not found.");
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid transfer.");
        }
    }

    private static async Task<IResult> ListFinancialCategories(
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var response = await finance.ListFinancialCategoriesAsync(ownerId, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetFinancialCategory(
        Guid id,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var response = await finance.GetFinancialCategoryAsync(ownerId, id, cancellationToken);
        if (response is null)
        {
            return Results.Problem(detail: "Category was not found.", statusCode: StatusCodes.Status404NotFound, title: "Category not found.");
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateFinancialCategory(
        CreateFinancialCategoryRequest request,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await finance.CreateFinancialCategoryAsync(ownerId, request, cancellationToken);
            return Results.Created($"/api/finance/categories/{response.Id}", response);
        }
        catch (FinanceRuleException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid category.");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Referenced resource not found.");
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid category.");
        }
    }

    private static async Task<IResult> RenameFinancialCategory(
        Guid id,
        RenameFinancialCategoryRequest request,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await finance.RenameFinancialCategoryAsync(ownerId, id, request, cancellationToken);
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status403Forbidden, title: "Category is read-only.");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Category not found.");
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid category.");
        }
    }

    private static async Task<IResult> SetFinancialCategoryActivation(
        Guid id,
        SetFinancialCategoryActivationRequest request,
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await finance.SetFinancialCategoryActivationAsync(ownerId, id, request, cancellationToken);
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status403Forbidden, title: "Category is read-only.");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Category not found.");
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid category.");
        }
    }
}
