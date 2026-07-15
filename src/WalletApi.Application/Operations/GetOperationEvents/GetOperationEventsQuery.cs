using MediatR;
using WalletApi.Application.Operations.Dtos;

namespace WalletApi.Application.Operations.GetOperationEvents;

public record GetOperationEventsQuery(string OperationId) : IRequest<IReadOnlyList<OperationEventDto>>;
