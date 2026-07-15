namespace WalletApi.Application.Abstractions;

public record ProviderPaymentResult(string ProviderPaymentId, string Status);

public interface IProviderPaymentClient
{
    Task<ProviderPaymentResult> SubmitPaymentAsync(
        string operationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken);
}
