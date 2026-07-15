using WalletApi.Application.Abstractions;

namespace WalletApi.Infrastructure;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
