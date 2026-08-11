using Domain.Debts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Debts;

internal sealed class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(200);

        builder.Property(d => d.Phone).HasMaxLength(20);

        builder.Property(d => d.Direction).HasConversion<string>().HasMaxLength(20);

        builder.Property(d => d.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(d => d.Notes).HasMaxLength(1000);

        builder.HasOne(d => d.FinancialAccount)
            .WithMany()
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(d => new { d.TenantId, d.CreatedAtUtc });

        builder.HasIndex(d => new { d.TenantId, d.Name });
    }
}
