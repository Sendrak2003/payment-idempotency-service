namespace WalletApi.Application.Operations.Dtos;

public record OperationEventDto(
    int EventId,
    string Type,
    string? FromStatus,
    string ToStatus,
    string Message,
    DateTimeOffset OccurredAt);
