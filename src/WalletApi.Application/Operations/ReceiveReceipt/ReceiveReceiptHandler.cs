using MediatR;
using Microsoft.Extensions.Logging;
using WalletApi.Application.Abstractions;
using WalletApi.Domain.Exceptions;
using WalletApi.Domain.Operations;

namespace WalletApi.Application.Operations.ReceiveReceipt;

public class ReceiveReceiptHandler : IRequestHandler<ReceiveReceiptCommand>
{
    private static readonly IReadOnlyDictionary<string, OperationStatus> AllowedResults =
        new Dictionary<string, OperationStatus>(StringComparer.OrdinalIgnoreCase)
        {
            ["COMPLETED"] = OperationStatus.Completed,
            ["REJECTED"] = OperationStatus.Rejected
        };

    private readonly IOperationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ReceiveReceiptHandler> _logger;

    public ReceiveReceiptHandler(
        IOperationRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<ReceiveReceiptHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public Task Handle(ReceiveReceiptCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedResults.TryGetValue(request.Result, out var receiptStatus))
        {
            throw new InvalidReceiptStatusException(request.Result);
        }

        return _unitOfWork.ExecuteInTransactionAsync<object?>(async ct =>
        {
            var operation = await _repository.GetByOperationIdAsync(request.OperationId, forUpdate: true, ct)
                ?? throw new OperationNotFoundException(request.OperationId);

            var occurredAt = request.OccurredAt ?? _clock.UtcNow;

            var outcome = operation.ApplyReceipt(request.ProviderPaymentId, receiptStatus, request.Message, occurredAt);

            if (outcome == ReceiptOutcome.IgnoredContradictory)
            {
                _logger.LogWarning(
                    "Ignored contradictory receipt for operation {OperationId}: already {CurrentStatus}, receipt says {ReceiptStatus}",
                    request.OperationId, operation.Status, receiptStatus);
            }

            return null;
        }, cancellationToken);
    }
}
