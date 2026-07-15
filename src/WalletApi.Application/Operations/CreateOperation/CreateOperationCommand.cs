using MediatR;
using WalletApi.Application.Operations.Dtos;

namespace WalletApi.Application.Operations.CreateOperation;

public record CreateOperationCommand(
    string OperationId,
    decimal Amount,
    string Currency,
    string? Description) : IRequest<OperationDto>;
