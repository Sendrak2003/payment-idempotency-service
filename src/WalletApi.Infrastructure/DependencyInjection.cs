using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletApi.Application.Abstractions;
using WalletApi.Infrastructure.Messaging;
using WalletApi.Infrastructure.Persistence;
using WalletApi.Infrastructure.Providers;
using WalletApi.Infrastructure.Recovery;

namespace WalletApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<WalletDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        var providerBaseUrl = configuration["PROVIDER_URL"]
            ?? throw new InvalidOperationException("PROVIDER_URL is not configured.");

        services.AddHttpClient<IProviderPaymentClient, ProviderPaymentClient>(client =>
        {
            client.BaseAddress = new Uri(providerBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<InMemorySubmissionQueue>();
        services.AddSingleton<IPaymentSubmissionQueue>(sp => sp.GetRequiredService<InMemorySubmissionQueue>());

        services.AddHostedService<PaymentSubmissionWorker>();
        services.AddHostedService<PendingOperationsRecoveryService>();

        return services;
    }
}
