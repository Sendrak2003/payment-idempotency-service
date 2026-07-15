using MediatR;
using WalletApi.Application.Abstractions;
using WalletApi.Application.Operations.Dtos;
using WalletApi.Domain.Exceptions;

namespace WalletApi.Application.Operations.GetOperation;

public class GetOperationHandler : IRequestHandler<GetOperationQuery, OperationDto>
{
    private readonly IOperationRepository _repository;

    public GetOperationHandler(IOperationRepository repository) => _repository = repository;

    public async Task<OperationDto> Handle(GetOperationQuery request, CancellationToken cancellationToken)
    {
        var operation = await _repository.GetByOperationIdAsync(request.OperationId, forUpdate: false, cancellationToken)
            ?? throw new OperationNotFoundException(request.OperationId);

        return operation.ToDto();
    }
}
