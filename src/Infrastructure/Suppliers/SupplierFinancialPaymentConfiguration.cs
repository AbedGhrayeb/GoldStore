using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Suppliers;

internal sealed class SupplierFinancialPaymentConfiguration : IEntityTypeConfiguration<SupplierFinancialPayment>
{
    public void Configure(EntityTypeBuilder<SupplierFinancialPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(18, 3);

        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasOne(s => s.SupplierFinancialTransaction)
            .WithMany()
            .HasForeignKey(p => p.SupplierFinancialTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.FinancialAccount)
            .WithMany()
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.TenantId, p.SupplierFinancialTransactionId, p.CreatedAtUtc });
    }
}
