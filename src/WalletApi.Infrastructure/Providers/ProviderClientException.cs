namespace WalletApi.Infrastructure.Providers;

public class ProviderClientException : Exception
{
    public ProviderClientException(string message, Exception? inner = null) : base(message, inner) { }
}
