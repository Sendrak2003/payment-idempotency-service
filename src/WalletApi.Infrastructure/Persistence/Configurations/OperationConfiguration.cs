using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletApi.Domain.Operations;

namespace WalletApi.Infrastructure.Persistence.Configurations;

public class OperationConfiguration : IEntityTypeConfiguration<Operation>
{
    public void Configure(EntityTypeBuilder<Operation> builder)
    {
        builder.ToTable("operations");

        builder.HasKey(o => o.OperationId);
        builder.Property(o => o.OperationId).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Amount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(1000);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.ProviderPaymentId).HasMaxLength(200);
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        builder.Property(o => o.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(o => o.ProviderPaymentId);

        builder.HasMany(o => o.Events)
            .WithOne()
            .HasForeignKey(e => e.OperationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Events).UsePropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
    }
}
