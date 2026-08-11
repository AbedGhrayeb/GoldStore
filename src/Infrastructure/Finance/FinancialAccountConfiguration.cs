using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Finance;

internal sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.HasKey(account => account.Id);

        builder.Property(account => account.Name).HasMaxLength(200);

        builder.Property(account => account.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(account => account.AccountType).HasConversion<string>().HasMaxLength(20);

        builder.Property(account => account.AccountNumber).HasMaxLength(50);

        builder.Property(account => account.Notes).HasMaxLength(500);

        builder.HasIndex(account => new { account.TenantId, account.Name, account.Currency }).IsUnique();
    }
}
