using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WalletApi.Application.Abstractions;

namespace WalletApi.Infrastructure.Recovery;

public class PendingOperationsRecoveryService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MinAgeNoProviderAck = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MinAgeAwaitingCallback = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPaymentSubmissionQueue _queue;
    private readonly ILogger<PendingOperationsRecoveryService> _logger;

    public PendingOperationsRecoveryService(IServiceScopeFactory scopeFactory, IPaymentSubmissionQueue queue, ILogger<PendingOperationsRecoveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recovery pass failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOperationRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var processing = await repository.GetProcessingAsync(cancellationToken);
        var now = clock.UtcNow;

        foreach (var operation in processing)
        {
            var minAge = operation.ProviderPaymentId is null ? MinAgeNoProviderAck : MinAgeAwaitingCallback;
            if (now - operation.UpdatedAt < minAge)
            {
                continue;
            }

            _logger.LogInformation("Re-queueing submission for pending operation {OperationId}", operation.OperationId);
            _queue.Enqueue(operation.OperationId);
        }
    }
}
