using Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Expenses;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 3);

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e=>e.FinancialAccount)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e=>e.ExpenseCategory)
            .WithMany(c=>c.Expenses)
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ExpenseDate });
        builder.HasIndex(e => new { e.AccountId, e.ExpenseDate });
        builder.HasIndex(e => new { e.ExpenseCategoryId, e.ExpenseDate });
    }
}
