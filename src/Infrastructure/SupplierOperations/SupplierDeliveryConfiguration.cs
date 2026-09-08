// <copyright file="SupplierDeliveryConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.SupplierOperations;
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

        builder.HasOne(delivery => delivery.Category)
            .WithMany()
            .HasForeignKey(delivery => delivery.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(delivery => delivery.Notes).HasMaxLength(1000);

        builder.HasOne(ii => ii.Supplier)
            .WithMany()
            .HasForeignKey(delivery => delivery.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(delivery => new { delivery.TenantId, delivery.SupplierId, delivery.CreatedAtUtc });

        builder.HasIndex(delivery => new { delivery.TenantId, delivery.CategoryId });
    }
}
