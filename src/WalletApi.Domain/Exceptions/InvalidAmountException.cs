namespace WalletApi.Domain.Exceptions;

public class InvalidAmountException : DomainException
{
    public InvalidAmountException(string message) : base(message) { }
}
