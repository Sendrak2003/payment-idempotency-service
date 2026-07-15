using MediatR;

namespace WalletApi.Application.Operations.ReceiveReceipt;

public record ReceiveReceiptCommand(
    string OperationId,
    string ProviderPaymentId,
    string Result,
    string? Message,
    DateTimeOffset? OccurredAt) : IRequest;
