using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Suppliers;

internal sealed class SupplierManufacturingLedgerEntryConfiguration : IEntityTypeConfiguration<SupplierManufacturingLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SupplierManufacturingLedgerEntry> builder)
    {
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Amount).HasPrecision(18, 3);

        builder.Property(entry => entry.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(entry => entry.MovementType).HasConversion<string>().HasMaxLength(20);

        builder.Property(entry => entry.ReferenceType).HasConversion<string>().HasMaxLength(50);

        builder.Property(entry => entry.Notes).HasMaxLength(1000);

        builder.HasOne(sm => sm.Supplier)
            .WithMany(s => s.SupplierManufacturingLedgerEntries)
            .HasForeignKey(entry => entry.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => new { entry.TenantId, entry.SupplierId, entry.CreatedAtUtc });

    }
}
