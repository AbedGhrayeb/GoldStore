using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Platform;

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(100);

        builder.Property(p => p.Description).HasMaxLength(500);

        builder.Property(p => p.MonthlyPrice).HasPrecision(18, 3);

        builder.Property(p => p.AnnualPrice).HasPrecision(18, 3);

        builder.Property(p => p.Currency).HasConversion<string>().HasMaxLength(3);

        builder.HasIndex(p => p.Name).IsUnique();
    }
}
