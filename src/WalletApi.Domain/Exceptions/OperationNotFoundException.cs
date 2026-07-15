namespace WalletApi.Domain.Exceptions;

public class OperationNotFoundException : DomainException
{
    public OperationNotFoundException(string operationId)
        : base($"Operation '{operationId}' was not found.") { }
}
