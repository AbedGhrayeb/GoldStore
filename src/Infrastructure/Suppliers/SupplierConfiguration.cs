using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Suppliers;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasKey(supplier => supplier.Id);

        builder.Property(supplier => supplier.Name).HasMaxLength(200);

        builder.Property(supplier => supplier.PrimaryPhone).HasMaxLength(30);

        builder.Property(supplier => supplier.SecondaryPhone).HasMaxLength(30);

        builder.Property(supplier => supplier.BankAccountNumber).HasMaxLength(100);

        builder.Property(supplier => supplier.Notes).HasMaxLength(1000);

        builder.HasIndex(supplier => supplier.Name).IsUnique();
    }
}
