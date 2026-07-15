namespace WalletApi.Application.Abstractions;

public interface IPaymentSubmissionQueue
{
    void Enqueue(string operationId);
}
