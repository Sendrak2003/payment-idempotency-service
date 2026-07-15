namespace WalletApi.Domain.Exceptions;

public class ReceiptConflictException : DomainException
{
    public ReceiptConflictException(string message) : base(message) { }
}
