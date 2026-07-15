using MediatR;
using WalletApi.Application.Abstractions;
using WalletApi.Application.Operations.Dtos;
using WalletApi.Domain.Exceptions;

namespace WalletApi.Application.Operations.SubmitOperation;

public class SubmitOperationHandler : IRequestHandler<SubmitOperationCommand, SubmitOperationResult>
{
    private readonly IOperationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentSubmissionQueue _submissionQueue;
    private readonly IDateTimeProvider _clock;

    public SubmitOperationHandler(
        IOperationRepository repository,
        IUnitOfWork unitOfWork,
        IPaymentSubmissionQueue submissionQueue,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _submissionQueue = submissionQueue;
        _clock = clock;
    }

    public async Task<SubmitOperationResult> Handle(SubmitOperationCommand request, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var operation = await _repository.GetByOperationIdAsync(request.OperationId, forUpdate: true, ct)
                ?? throw new OperationNotFoundException(request.OperationId);

            var started = operation.TryStartProcessing(_clock.UtcNow);
            return new SubmitOperationResult(operation.ToDto(), started);
        }, cancellationToken);

        if (result.WasNewlySubmitted)
        {
            _submissionQueue.Enqueue(request.OperationId);
        }

        return result;
    }
}
