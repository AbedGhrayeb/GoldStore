using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class TenantSettings : Entity, ITenantEntity
{
    public const string DefaultLocale = "ar-JO";
    public const string DefaultTheme = "light";

    public Guid TenantId { get; private set; }

    public string DisplayName { get; private set; }

    public string? LogoUrl { get; private set; }

    public string TimeZoneId { get; private set; }

    public string Locale { get; private set; }

    public string Theme { get; private set; }

    public string? InvoiceNumberPrefix { get; private set; }

    public List<string> EnabledFeatures { get; private set; }

    private TenantSettings()
    {
        DisplayName = string.Empty;
        TimeZoneId = string.Empty;
        Locale = DefaultLocale;
        Theme = DefaultTheme;
        EnabledFeatures = [];
    }

    private TenantSettings(Guid id, Guid tenantId, string displayName, string? logoUrl, string timeZoneId,
        string locale, string theme, string? invoiceNumberPrefix, List<string> enabledFeatures) : base(id)
    {
        TenantId = tenantId;
        DisplayName = displayName;
        LogoUrl = logoUrl;
        TimeZoneId = timeZoneId;
        Locale = locale;
        Theme = theme;
        InvoiceNumberPrefix = invoiceNumberPrefix;
        EnabledFeatures = enabledFeatures;
    }

    public static Result<TenantSettings> Create(Guid tenantId, string displayName, string? logoUrl, string timeZoneId,
        string? locale = null, string? theme = null, string? invoiceNumberPrefix = null, List<string>? enabledFeatures = null)
    {
        if (tenantId == Guid.Empty)
        {
            return TenantErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return TenantErrors.DisplayNameRequired;
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TenantErrors.TimeZoneRequired;
        }

        return new TenantSettings(Guid.CreateVersion7(), tenantId, displayName.Trim(), logoUrl?.Trim(), timeZoneId.Trim(),
            string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale.Trim(),
            string.IsNullOrWhiteSpace(theme) ? DefaultTheme : theme.Trim(),
            invoiceNumberPrefix?.Trim(),
            enabledFeatures ?? []);
    }

    public Result<Updated> Update(string displayName, string? logoUrl, string timeZoneId,
        string locale, string theme, string? invoiceNumberPrefix, List<string> enabledFeatures)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return TenantErrors.DisplayNameRequired;
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TenantErrors.TimeZoneRequired;
        }

        DisplayName = displayName.Trim();
        LogoUrl = logoUrl?.Trim();
        TimeZoneId = timeZoneId.Trim();
        Locale = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale.Trim();
        Theme = string.IsNullOrWhiteSpace(theme) ? DefaultTheme : theme.Trim();
        InvoiceNumberPrefix = invoiceNumberPrefix?.Trim();
        EnabledFeatures = enabledFeatures ?? [];

        return Result.Updated;
    }
}
