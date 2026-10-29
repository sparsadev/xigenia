using System.Security.Claims;
using Xigenia.Application.Finance;

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
        ClaimsPrincipal user,
        FinanceService finance,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var response = await finance.ListMoneyMovementsAsync(ownerId, cancellationToken);
        return Results.Ok(response);
    }

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
