using MediatR;
using WalletApi.Application.Abstractions;
using WalletApi.Application.Operations.Dtos;
using WalletApi.Domain.Exceptions;

namespace WalletApi.Application.Operations.GetOperationEvents;

public class GetOperationEventsHandler : IRequestHandler<GetOperationEventsQuery, IReadOnlyList<OperationEventDto>>
{
    private readonly IOperationRepository _repository;

    public GetOperationEventsHandler(IOperationRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<OperationEventDto>> Handle(GetOperationEventsQuery request, CancellationToken cancellationToken)
    {
        var operation = await _repository.GetByOperationIdAsync(request.OperationId, forUpdate: false, cancellationToken)
            ?? throw new OperationNotFoundException(request.OperationId);

        return operation.Events.ToDtos();
    }
}
