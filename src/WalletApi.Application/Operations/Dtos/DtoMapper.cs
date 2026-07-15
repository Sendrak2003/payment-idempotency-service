using WalletApi.Domain.Operations;

namespace WalletApi.Application.Operations.Dtos;

public static class DtoMapper
{
    public static OperationDto ToDto(this Operation operation) => new(
        operation.OperationId,
        operation.Amount,
        operation.Currency,
        operation.Description,
        operation.Status.ToString().ToUpperInvariant(),
        operation.ProviderPaymentId,
        operation.CreatedAt,
        operation.UpdatedAt);

    public static IReadOnlyList<OperationEventDto> ToDtos(this IEnumerable<OperationEvent> events) =>
        events
            .OrderBy(e => e.SequenceNumber)
            .Select(e => new OperationEventDto(
                e.SequenceNumber,
                e.Type.ToString().ToUpperInvariant(),
                e.FromStatus?.ToString().ToUpperInvariant(),
                e.ToStatus.ToString().ToUpperInvariant(),
                e.Message,
                e.OccurredAt))
            .ToList();
}
