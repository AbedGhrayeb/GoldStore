// <copyright file="TenantSettings.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
        this.DisplayName = string.Empty;
        this.TimeZoneId = string.Empty;
        this.Locale = DefaultLocale;
        this.Theme = DefaultTheme;
        this.EnabledFeatures = [];
    }

    private TenantSettings(Guid id, Guid tenantId, string displayName, string? logoUrl, string timeZoneId,
        string locale, string theme, string? invoiceNumberPrefix, List<string> enabledFeatures)
        : base(id)
    {
        this.TenantId = tenantId;
        this.DisplayName = displayName;
        this.LogoUrl = logoUrl;
        this.TimeZoneId = timeZoneId;
        this.Locale = locale;
        this.Theme = theme;
        this.InvoiceNumberPrefix = invoiceNumberPrefix;
        this.EnabledFeatures = enabledFeatures;
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

        this.DisplayName = displayName.Trim();
        this.LogoUrl = logoUrl?.Trim();
        this.TimeZoneId = timeZoneId.Trim();
        this.Locale = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale.Trim();
        this.Theme = string.IsNullOrWhiteSpace(theme) ? DefaultTheme : theme.Trim();
        this.InvoiceNumberPrefix = invoiceNumberPrefix?.Trim();
        this.EnabledFeatures = enabledFeatures ?? [];

        return Result.Updated;
    }
}
