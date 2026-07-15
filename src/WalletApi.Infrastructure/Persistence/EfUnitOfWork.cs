using Microsoft.EntityFrameworkCore;
using WalletApi.Application.Abstractions;

namespace WalletApi.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly WalletDbContext _db;

    public EfUnitOfWork(WalletDbContext db) => _db = db;

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var result = await action(cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
