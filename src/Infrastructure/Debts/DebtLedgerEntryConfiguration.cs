// <copyright file="DebtLedgerEntryConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Debts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Debts;

internal sealed class DebtLedgerEntryConfiguration : IEntityTypeConfiguration<DebtLedgerEntry>
{
    public void Configure(EntityTypeBuilder<DebtLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 3);

        builder.Property(e => e.MovementType).HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Debt)
            .WithMany()
            .HasForeignKey(e => e.DebtId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.DebtId, e.CreatedAtUtc });
    }
}
