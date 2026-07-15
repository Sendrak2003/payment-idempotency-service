namespace WalletApi.Domain.Exceptions;

public class OperationAlreadyExistsException : DomainException
{
    public OperationAlreadyExistsException(string operationId)
        : base($"Operation '{operationId}' already exists.") { }
}
