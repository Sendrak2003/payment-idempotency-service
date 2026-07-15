namespace WalletApi.Api.Contracts;

public record ReceiptRequest(
    string? OperationId,
    string? ProviderPaymentId,
    string? Result,
    string? Message,
    DateTimeOffset? OccurredAt);
