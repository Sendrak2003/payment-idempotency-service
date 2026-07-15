using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WalletApi.Application.Abstractions;
using WalletApi.Domain.Operations;

namespace WalletApi.Infrastructure.Messaging;

public class PaymentSubmissionWorker : BackgroundService
{
    private readonly InMemorySubmissionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentSubmissionWorker> _logger;

    public PaymentSubmissionWorker(InMemorySubmissionQueue queue, IServiceScopeFactory scopeFactory, ILogger<PaymentSubmissionWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var operationId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(operationId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing submission for operation {OperationId}", operationId);
            }
        }
    }

    private async Task ProcessAsync(string operationId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOperationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var providerClient = scope.ServiceProvider.GetRequiredService<IProviderPaymentClient>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var snapshot = await repository.GetByOperationIdAsync(operationId, forUpdate: false, cancellationToken);
        if (snapshot is null)
        {
            _logger.LogWarning("Submission queued for unknown operation {OperationId}", operationId);
            return;
        }

        if (snapshot.Status is OperationStatus.Completed or OperationStatus.Rejected)
        {
            return;
        }

        ProviderPaymentResult result;
        try
        {
            result = await providerClient.SubmitPaymentAsync(operationId, snapshot.Amount, snapshot.Currency, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider submission failed for operation {OperationId}; will be retried by recovery", operationId);
            return;
        }

        await unitOfWork.ExecuteInTransactionAsync<object?>(async token =>
        {
            var operation = await repository.GetByOperationIdAsync(operationId, forUpdate: true, token);
            operation?.RecordProviderAcceptance(result.ProviderPaymentId, clock.UtcNow);
            return null;
        }, cancellationToken);
    }
}
