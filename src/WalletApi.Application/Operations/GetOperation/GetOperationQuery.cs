using MediatR;
using WalletApi.Application.Operations.Dtos;

namespace WalletApi.Application.Operations.GetOperation;

public record GetOperationQuery(string OperationId) : IRequest<OperationDto>;
