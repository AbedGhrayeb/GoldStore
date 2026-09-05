// <copyright file="SupplierManufacturingPaymentConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.SupplierOperations;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SupplierOperations;

internal sealed class SupplierManufacturingPaymentConfiguration : IEntityTypeConfiguration<SupplierManufacturingPayment>
{
    public void Configure(EntityTypeBuilder<SupplierManufacturingPayment> builder)
    {
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Amount).HasPrecision(18, 3);

        builder.Property(payment => payment.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(payment => payment.Notes).HasMaxLength(1000);

        builder.HasOne<Supplier>(p => p.Supplier)
            .WithMany()
            .HasForeignKey(payment => payment.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cp => cp.Account)
            .WithMany()
            .HasForeignKey(payment => payment.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => new { payment.TenantId, payment.SupplierId, payment.CreatedAtUtc });
    }
}
