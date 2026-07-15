using System.Globalization;
using WalletApi.Application.Operations.Dtos;

namespace WalletApi.Api.Contracts;

public record OperationResponse(
    string OperationId,
    string Amount,
    string Currency,
    string? Description,
    string Status,
    string? ProviderPaymentId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class OperationResponseMapper
{
    public static OperationResponse ToResponse(this OperationDto dto) => new(
        dto.OperationId,
        dto.Amount.ToString("F2", CultureInfo.InvariantCulture),
        dto.Currency,
        dto.Description,
        dto.Status,
        dto.ProviderPaymentId,
        dto.CreatedAt,
        dto.UpdatedAt);
}
