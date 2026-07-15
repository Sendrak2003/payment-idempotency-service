using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletApi.Domain.Operations;

namespace WalletApi.Infrastructure.Persistence.Configurations;

public class OperationEventConfiguration : IEntityTypeConfiguration<OperationEvent>
{
    public void Configure(EntityTypeBuilder<OperationEvent> builder)
    {
        builder.ToTable("operation_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.OperationId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.SequenceNumber).IsRequired();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => new { e.OperationId, e.SequenceNumber }).IsUnique();
    }
}
