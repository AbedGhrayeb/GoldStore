using Domain.Finance;
using Domain.SupplierOperations;
using Domain.Suppliers;
using Domain.Users;
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

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(payment => payment.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(payment => payment.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(payment => payment.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => new { payment.SupplierId, payment.Date });
    }
}
