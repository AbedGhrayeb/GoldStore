using Domain.Finance;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Suppliers;

internal sealed class SupplierFinancialTransactionConfiguration : IEntityTypeConfiguration<SupplierFinancialTransaction>
{
    public void Configure(EntityTypeBuilder<SupplierFinancialTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Direction).HasConversion<string>().HasMaxLength(20);

        builder.Property(t => t.Amount).HasPrecision(18, 3);

        builder.Property(t => t.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(t => t.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.SupplierId, t.CreatedAt });
    }
}
