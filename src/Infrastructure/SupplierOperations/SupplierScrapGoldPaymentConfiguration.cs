// <copyright file="SupplierScrapGoldPaymentConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.SupplierOperations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SupplierOperations;

internal sealed class SupplierScrapGoldPaymentConfiguration : IEntityTypeConfiguration<SupplierScrapGoldPayment>
{
    public void Configure(EntityTypeBuilder<SupplierScrapGoldPayment> builder)
    {
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Karat).HasConversion<string>().HasMaxLength(3);

        builder.Property(payment => payment.WeightInGrams).HasPrecision(18, 3);

        builder.Property(payment => payment.Equivalent21KWeightInGrams).HasPrecision(18, 3);

        builder.Property(payment => payment.Notes).HasMaxLength(1000);

        builder.HasOne(s => s.Supplier)
            .WithMany()
            .HasForeignKey(payment => payment.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => new { payment.TenantId, payment.SupplierId, payment.CreatedAtUtc });
    }
}
