using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tenants;

internal sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(settings => settings.LogoUrl).HasMaxLength(2048);
        builder.Property(settings => settings.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(settings => settings.Locale).HasMaxLength(20).IsRequired();
        builder.Property(settings => settings.Theme).HasMaxLength(20).IsRequired();
        builder.Property(settings => settings.InvoiceNumberPrefix).HasMaxLength(20);

        // The Tenant relationship and a TenantId index are applied centrally
        // by TenantOwnershipModelBuilderExtensions; keep the one-settings-per-tenant uniqueness here.
        builder.HasIndex(settings => settings.TenantId).IsUnique();
    }
}
