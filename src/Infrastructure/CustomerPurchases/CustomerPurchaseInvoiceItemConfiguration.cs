using Domain.Catalog;
using Domain.CustomerPurchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.CustomerPurchases;

internal sealed class CustomerPurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<CustomerPurchaseInvoiceItem>
{
    public void Configure(EntityTypeBuilder<CustomerPurchaseInvoiceItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Karat).HasConversion<string>().HasMaxLength(3);

        builder.Property(i => i.WeightInGrams).HasPrecision(18, 3);

        builder.Property(i => i.Equivalent21KWeightInGrams).HasPrecision(18, 3);

        builder.Property(i => i.PricePerGram).HasPrecision(18, 3);

        builder.Property(i => i.GoldAmount).HasPrecision(18, 3);

        builder.HasOne<CustomerPurchaseInvoice>()
            .WithMany()
            .HasForeignKey(i => i.CustomerPurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(i => i.CustomerPurchaseInvoiceId);
    }
}
