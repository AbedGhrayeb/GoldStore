using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Invoices;

internal sealed class InvoiceNumberSequenceConfiguration : IEntityTypeConfiguration<InvoiceNumberSequence>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberSequence> builder)
    {
        builder.HasKey(sequence => sequence.Id);

        builder.Property(sequence => sequence.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(sequence => sequence.Period)
            .HasMaxLength(7);

        builder.Property(sequence => sequence.Version)
            .IsConcurrencyToken();

        builder.HasIndex(sequence => new
        {
            sequence.TenantId,
            sequence.DocumentType,
            sequence.Period,
        }).IsUnique();
    }
}
