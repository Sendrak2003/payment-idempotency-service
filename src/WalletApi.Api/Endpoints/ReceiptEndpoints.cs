using MediatR;
using WalletApi.Api.Contracts;
using WalletApi.Application.Operations.ReceiveReceipt;

namespace WalletApi.Api.Endpoints;

public static class ReceiptEndpoints
{
    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/receipts", ReceiveReceipt);
        return app;
    }

    private static async Task<IResult> ReceiveReceipt(ReceiptRequest request, ISender sender, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OperationId) ||
            string.IsNullOrWhiteSpace(request.ProviderPaymentId) ||
            string.IsNullOrWhiteSpace(request.Result))
        {
            return Results.BadRequest(new { error = "operationId, providerPaymentId and result are required." });
        }

        await sender.Send(
            new ReceiveReceiptCommand(request.OperationId, request.ProviderPaymentId, request.Result, request.Message, request.OccurredAt),
            cancellationToken);

        return Results.NoContent();
    }
}
