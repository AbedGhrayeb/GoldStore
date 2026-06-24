using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Finance;

internal sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(transaction => transaction.Amount).HasPrecision(18, 3);

        builder.Property(transaction => transaction.TransactionType).HasConversion<string>().HasMaxLength(20);

        builder.Property(transaction => transaction.ReferenceType).HasConversion<string>().HasMaxLength(50);

        builder.Property(transaction => transaction.Notes).HasMaxLength(500);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(transaction => new { transaction.AccountId, transaction.Date });

        builder.HasIndex(transaction => new { transaction.ReferenceType, transaction.ReferenceId });
    }
}
