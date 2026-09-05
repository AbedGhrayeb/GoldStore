// <copyright file="GoldLedgerEntryConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Inventory;

internal sealed class GoldLedgerEntryConfiguration : IEntityTypeConfiguration<GoldLedgerEntry>
{
    public void Configure(EntityTypeBuilder<GoldLedgerEntry> builder)
    {
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Karat).HasConversion<string>().HasMaxLength(3);

        builder.Property(entry => entry.WeightInGrams).HasPrecision(18, 3);

        builder.Property(entry => entry.Equivalent21KWeightInGrams).HasPrecision(18, 3);

        builder.Property(entry => entry.MovementType).HasConversion<string>().HasMaxLength(20);

        builder.Property(entry => entry.ReferenceType).HasConversion<string>().HasMaxLength(50);

        builder.Property(entry => entry.Notes).HasMaxLength(1000);

        builder.HasIndex(entry => new { entry.TenantId, entry.Karat, entry.CreatedAtUtc });

        builder.HasIndex(entry => new { entry.TenantId, entry.ReferenceType, entry.ReferenceId });
    }
}
