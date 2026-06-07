using Domain.SupplierOperations;
using Domain.Suppliers;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SupplierOperations;

internal sealed class SupplierDeliveryConfiguration : IEntityTypeConfiguration<SupplierDelivery>
{
    public void Configure(EntityTypeBuilder<SupplierDelivery> builder)
    {
        builder.HasKey(delivery => delivery.Id);

        builder.Property(delivery => delivery.Karat).HasConversion<string>().HasMaxLength(3);

        builder.Property(delivery => delivery.WeightInGrams).HasPrecision(18, 3);

        builder.Property(delivery => delivery.Equivalent21KWeightInGrams).HasPrecision(18, 3);

        builder.Property(delivery => delivery.ManufacturingFeePerGram).HasPrecision(18, 3);

        builder.Property(delivery => delivery.TotalManufacturingFee).HasPrecision(18, 3);

        builder.Property(delivery => delivery.ManufacturingFeeCurrency).HasConversion<string>().HasMaxLength(3);

        builder.Property(delivery => delivery.Notes).HasMaxLength(1000);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(delivery => delivery.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(delivery => delivery.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(delivery => new { delivery.SupplierId, delivery.Date });
    }
}
