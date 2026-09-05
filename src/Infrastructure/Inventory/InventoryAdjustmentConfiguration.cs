// <copyright file="InventoryAdjustmentConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Inventory;

internal sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);

        builder.Property(a => a.Karat).HasConversion<string>().HasMaxLength(3);

        builder.Property(a => a.WeightInGrams).HasPrecision(18, 3);

        builder.Property(a => a.Equivalent21KWeightInGrams).HasPrecision(18, 3);

        builder.Property(a => a.Reason).HasMaxLength(200);

        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder.HasIndex(a => new { a.TenantId, a.Type, a.CreatedAtUtc });

        builder.HasIndex(a => new { a.TenantId, a.CreatedAtUtc });
    }
}
