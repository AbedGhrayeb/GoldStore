using Domain.CustomerPurchases;
using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.CustomerPurchases;

internal sealed class CustomerPurchaseInvoiceConfiguration : IEntityTypeConfiguration<CustomerPurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<CustomerPurchaseInvoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(20);

        builder.Property(i => i.SellerName).HasMaxLength(200);
        builder.Property(i => i.SellerIdNumber).HasMaxLength(10);

        builder.Property(i => i.SellerPhone).HasMaxLength(13);
        builder.Property(i => i.SellerYearOfBirth).HasMaxLength(13);
        builder.Property(i => i.SellerAccountNumber).HasMaxLength(20);
        builder.Property(i => i.SellerAddress).HasMaxLength(200);


        builder.Property(i => i.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(i => i.TotalAmount).HasPrecision(18, 3);

        builder.Property(i => i.AmountPaid).HasPrecision(18, 3);

        builder.Property(i => i.PaymentMethod).HasConversion<string>().HasMaxLength(10);

        builder.Property(i => i.Notes).HasMaxLength(1000);

        builder.HasOne(i => i.FinancialAccount)
            .WithMany()
            .HasForeignKey(i => i.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(i => i.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();

        builder.HasIndex(i => new { i.TenantId, i.Date });
    }
}
