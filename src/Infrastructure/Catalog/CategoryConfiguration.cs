using Microsoft.EntityFrameworkCore;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name).HasMaxLength(200);

        builder.Property(category => category.Description).HasMaxLength(500);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => new { category.ParentCategoryId, category.Name }).IsUnique();

        builder.HasIndex(category => category.Name);
    }
}

