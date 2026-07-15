using MediatR;
using WalletApi.Application.Abstractions;
using WalletApi.Application.Operations.Dtos;
using WalletApi.Domain.Operations;

namespace WalletApi.Application.Operations.CreateOperation;

public class CreateOperationHandler : IRequestHandler<CreateOperationCommand, OperationDto>
{
    private readonly IOperationRepository _repository;
    private readonly IDateTimeProvider _clock;

    public CreateOperationHandler(IOperationRepository repository, IDateTimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<OperationDto> Handle(CreateOperationCommand request, CancellationToken cancellationToken)
    {
        var operation = Operation.Create(request.OperationId, request.Amount, request.Currency, request.Description, _clock.UtcNow);

        await _repository.AddAsync(operation, cancellationToken);

        return operation.ToDto();
    }
}
