namespace WalletApi.Application.Operations.Dtos;

public record OperationDto(
    string OperationId,
    decimal Amount,
    string Currency,
    string? Description,
    string Status,
    string? ProviderPaymentId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
