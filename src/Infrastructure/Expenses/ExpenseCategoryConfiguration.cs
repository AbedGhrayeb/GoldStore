using Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Expenses;

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}