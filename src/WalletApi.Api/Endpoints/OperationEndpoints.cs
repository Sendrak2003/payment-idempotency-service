using MediatR;
using WalletApi.Api.Contracts;
using WalletApi.Application.Operations.CreateOperation;
using WalletApi.Application.Operations.GetOperation;
using WalletApi.Application.Operations.GetOperationEvents;
using WalletApi.Application.Operations.SubmitOperation;

namespace WalletApi.Api.Endpoints;

public static class OperationEndpoints
{
    public static IEndpointRouteBuilder MapOperationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/operations", CreateOperation);
        app.MapPost("/operations/{id}/submit", SubmitOperation);
        app.MapGet("/operations/{id}", GetOperation);
        app.MapGet("/operations/{id}/events", GetOperationEvents);

        return app;
    }

    private static async Task<IResult> CreateOperation(CreateOperationRequest request, ISender sender, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            return Results.BadRequest(new { error = "operationId is required." });
        }

        if (!AmountValidator.TryParse(request.Amount, out var amount))
        {
            return Results.BadRequest(new { error = "amount must be a positive decimal string with at most two decimal places." });
        }

        if (!string.Equals(request.Currency, "RUB", StringComparison.Ordinal))
        {
            return Results.BadRequest(new { error = "currency must be RUB." });
        }

        var dto = await sender.Send(
            new CreateOperationCommand(request.OperationId, amount, request.Currency!, request.Description),
            cancellationToken);

        return Results.Created($"/operations/{dto.OperationId}", dto.ToResponse());
    }

    private static async Task<IResult> SubmitOperation(string id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SubmitOperationCommand(id), cancellationToken);

        return result.WasNewlySubmitted
            ? Results.Accepted($"/operations/{id}", result.Operation.ToResponse())
            : Results.Ok(result.Operation.ToResponse());
    }

    private static async Task<IResult> GetOperation(string id, ISender sender, CancellationToken cancellationToken)
    {
        var dto = await sender.Send(new GetOperationQuery(id), cancellationToken);
        return Results.Ok(dto.ToResponse());
    }

    private static async Task<IResult> GetOperationEvents(string id, ISender sender, CancellationToken cancellationToken)
    {
        var events = await sender.Send(new GetOperationEventsQuery(id), cancellationToken);
        return Results.Ok(events);
    }
}
