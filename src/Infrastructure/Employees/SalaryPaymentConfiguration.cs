// <copyright file="SalaryPaymentConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Employees;

internal sealed class SalaryPaymentConfiguration : IEntityTypeConfiguration<SalaryPayment>
{
    public void Configure(EntityTypeBuilder<SalaryPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.SalaryAmount).HasPrecision(18, 3);
        builder.Property(p => p.DiscountAmount).HasPrecision(18, 3);
        builder.Property(p => p.Amount).HasPrecision(18, 3);

        builder.Property(p => p.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Employee)
            .WithMany(e => e.SalaryPayments)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.FinancialAccount)
            .WithMany()
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.TenantId, p.EmployeeId, p.PaymentDate });
        builder.HasIndex(p => new { p.TenantId, p.PaymentDate });
    }
}
