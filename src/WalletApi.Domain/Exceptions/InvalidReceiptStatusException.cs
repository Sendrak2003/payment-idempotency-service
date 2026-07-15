namespace WalletApi.Domain.Exceptions;

public class InvalidReceiptStatusException : DomainException
{
    public InvalidReceiptStatusException(string status)
        : base($"Receipt status '{status}' is not one of COMPLETED, REJECTED.") { }
}
