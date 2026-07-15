using Microsoft.EntityFrameworkCore;
using WalletApi.Domain.Operations;

namespace WalletApi.Infrastructure.Persistence;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    public DbSet<Operation> Operations => Set<Operation>();
    public DbSet<OperationEvent> OperationEvents => Set<OperationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
    }
}
