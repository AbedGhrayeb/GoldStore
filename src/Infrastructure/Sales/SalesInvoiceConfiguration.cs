using Domain.Employees;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Sales;

internal sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(20);

        builder.Property(i => i.CustomerName).HasMaxLength(200);

        builder.Property(i => i.CustomerPhone).HasMaxLength(20);

        builder.Property(i => i.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(i => i.TotalAmount).HasPrecision(18, 3);

        builder.Property(i => i.AmountPaid).HasPrecision(18, 3);

        builder.Property(i => i.RemainingBalance).HasPrecision(18, 3);

        builder.Property(i => i.PaymentMethod).HasConversion<string>().HasMaxLength(10).IsRequired(false);

        builder.Property(i => i.CustomerAccountNumber).HasMaxLength(50);

        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(i => i.Notes).HasMaxLength(1000);

        builder.HasOne(i => i.FinancialAccount)
            .WithMany()
            .HasForeignKey(i => i.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(i => i.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.SaleInvoiceItems)
            .WithOne()
            .HasForeignKey(i => i.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.HasIndex(i => i.Date);

    }
}
