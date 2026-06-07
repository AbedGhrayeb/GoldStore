using Domain.Suppliers;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Suppliers;

internal sealed class SupplierGoldLedgerEntryConfiguration : IEntityTypeConfiguration<SupplierGoldLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SupplierGoldLedgerEntry> builder)
    {
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Karat).HasConversion<string>().HasMaxLength(3);

        builder.Property(entry => entry.WeightInGrams).HasPrecision(18, 3);

        builder.Property(entry => entry.Equivalent21KWeightInGrams).HasPrecision(18, 3);

        builder.Property(entry => entry.MovementType).HasConversion<string>().HasMaxLength(20);

        builder.Property(entry => entry.ReferenceType).HasConversion<string>().HasMaxLength(50);

        builder.Property(entry => entry.Notes).HasMaxLength(1000);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(entry => entry.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => new { entry.SupplierId, entry.Date });

        builder.HasIndex(entry => new { entry.ReferenceType, entry.ReferenceId });
    }
}
