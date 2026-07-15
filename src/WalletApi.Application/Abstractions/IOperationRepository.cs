using WalletApi.Domain.Operations;

namespace WalletApi.Application.Abstractions;

public interface IOperationRepository
{
    Task<Operation?> GetByOperationIdAsync(string operationId, bool forUpdate, CancellationToken cancellationToken);

    Task AddAsync(Operation operation, CancellationToken cancellationToken);

    Task<IReadOnlyList<Operation>> GetProcessingAsync(CancellationToken cancellationToken);
}
