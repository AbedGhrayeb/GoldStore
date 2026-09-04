using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Suppliers;

internal sealed class SupplierFinancialLedgerEntryConfiguration : IEntityTypeConfiguration<SupplierFinancialLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SupplierFinancialLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 3);

        builder.Property(e => e.MovementType).HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne<SupplierFinancialTransaction>()
            .WithMany()
            .HasForeignKey(e => e.SupplierFinancialTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.SupplierFinancialTransactionId, e.CreatedAtUtc });

    }
}
