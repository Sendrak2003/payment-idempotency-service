using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WalletApi.Application.Abstractions;

namespace WalletApi.Infrastructure.Providers;

public class ProviderPaymentClient : IProviderPaymentClient
{
    private const int MaxAttempts = 5;

    private readonly HttpClient _httpClient;
    private readonly ILogger<ProviderPaymentClient> _logger;

    public ProviderPaymentClient(HttpClient httpClient, ILogger<ProviderPaymentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProviderPaymentResult> SubmitPaymentAsync(
        string operationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        var payload = new PaymentRequestBody(operationId, amount.ToString("F2", CultureInfo.InvariantCulture), currency);
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = new ByteArrayContent(payloadBytes)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Add("Idempotency-Key", operationId);
            request.Headers.Add("X-Correlation-ID", operationId);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < MaxAttempts)
                {
                    await BackoffAsync(attempt, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadFromJsonAsync<PaymentResponseBody>(cancellationToken)
                    ?? throw new ProviderClientException($"Provider returned an empty response for operation {operationId}.");

                return new ProviderPaymentResult(body.ProviderPaymentId, body.Status);
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Network error calling provider for operation {OperationId}, attempt {Attempt}/{MaxAttempts}", operationId, attempt, MaxAttempts);
                await BackoffAsync(attempt, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Timeout calling provider for operation {OperationId}, attempt {Attempt}/{MaxAttempts}", operationId, attempt, MaxAttempts);
                await BackoffAsync(attempt, cancellationToken);
            }
        }

        throw new ProviderClientException($"Failed to submit payment for operation {operationId} after {MaxAttempts} attempts.");
    }

    private static Task BackoffAsync(int attempt, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
        return Task.Delay(delay, cancellationToken);
    }

    private record PaymentRequestBody(
        [property: JsonPropertyName("operationId")] string OperationId,
        [property: JsonPropertyName("amount")] string Amount,
        [property: JsonPropertyName("currency")] string Currency);

    private record PaymentResponseBody(
        [property: JsonPropertyName("providerPaymentId")] string ProviderPaymentId,
        [property: JsonPropertyName("status")] string Status);
}
