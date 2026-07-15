using MediatR;
using WalletApi.Application.Operations.Dtos;

namespace WalletApi.Application.Operations.SubmitOperation;

public record SubmitOperationCommand(string OperationId) : IRequest<SubmitOperationResult>;

public record SubmitOperationResult(OperationDto Operation, bool WasNewlySubmitted);
