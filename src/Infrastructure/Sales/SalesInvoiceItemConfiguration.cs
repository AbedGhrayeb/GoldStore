using Domain.Catalog;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Sales;

internal sealed class SalesInvoiceItemConfiguration : IEntityTypeConfiguration<SalesInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Karat).HasConversion<string>().HasMaxLength(3);

        builder.Property(i => i.WeightInGrams).HasPrecision(18, 3);

        builder.Property(i => i.Equivalent21KWeightInGrams).HasPrecision(18, 3);

        builder.Property(i => i.PricePerGram).HasPrecision(18, 3);

        builder.Property(i => i.GoldAmount).HasPrecision(18, 3);

        builder.HasOne<SalesInvoice>()
            .WithMany()
            .HasForeignKey(i => i.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(i => i.SalesInvoiceId);
    }
}
