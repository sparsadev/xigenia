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
        catch (KeyNotFoundException)
        {
            return Results.Problem(detail: "Obligation was not found.", statusCode: StatusCodes.Status404NotFound, title: "Obligation not found.");
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
}
