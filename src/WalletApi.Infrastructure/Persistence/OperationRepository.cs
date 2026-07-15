using Microsoft.EntityFrameworkCore;
using Npgsql;
using WalletApi.Application.Abstractions;
using WalletApi.Domain.Exceptions;
using WalletApi.Domain.Operations;

namespace WalletApi.Infrastructure.Persistence;

public class OperationRepository : IOperationRepository
{
    private readonly WalletDbContext _db;

    public OperationRepository(WalletDbContext db) => _db = db;

    public async Task<Operation?> GetByOperationIdAsync(string operationId, bool forUpdate, CancellationToken cancellationToken)
    {
        if (forUpdate)
        {
            var lockedQuery = _db.Operations
                .FromSqlInterpolated($"SELECT *, xmin FROM operations WHERE \"OperationId\" = {operationId} FOR UPDATE")
                .Include(o => o.Events);

            return await lockedQuery.FirstOrDefaultAsync(cancellationToken);
        }

        return await _db.Operations
            .Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.OperationId == operationId, cancellationToken);
    }

    public async Task AddAsync(Operation operation, CancellationToken cancellationToken)
    {
        _db.Operations.Add(operation);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new OperationAlreadyExistsException(operation.OperationId);
        }
    }

    public async Task<IReadOnlyList<Operation>> GetProcessingAsync(CancellationToken cancellationToken)
    {
        return await _db.Operations
            .Where(o => o.Status == OperationStatus.Processing)
            .ToListAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
