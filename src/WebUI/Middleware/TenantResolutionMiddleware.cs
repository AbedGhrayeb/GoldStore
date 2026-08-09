using Application.Abstractions.Tenancy;
using Domain.Tenants;
using Infrastructure.Tenancy;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace WebUI.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task Invoke(
        HttpContext context,
        ITenantContextSetter tenantContextSetter,
        ITenantResolver tenantResolver,
        IOptions<TenancyOptions> tenancyOptions,
        IDateTimeProvider dateTimeProvider)
    {
        // Platform backoffice endpoints are tenant-agnostic.
        if (context.Request.Path.StartsWithSegments("/api/platform", StringComparison.OrdinalIgnoreCase))
        {
            await next.Invoke(context);
            return;
        }

        string? subdomain = ExtractSubdomain(context.Request.Host.Host, tenancyOptions.Value.BaseDomain);

        // Bare base domain (or www / foreign host): no tenant context. Tenant-specific
        // endpoints fail fast later when they access ITenantContext.
        if (subdomain is null)
        {
            await next.Invoke(context);
            return;
        }

        TenantInfo? tenant = await tenantResolver.ResolveAsync(subdomain, context.RequestAborted);

        if (tenant is null)
        {
            await TenantHttpErrors.WriteAsync(context, StatusCodes.Status404NotFound, "Tenant.NotFound", "المتجر غير موجود");
            return;
        }

        if (tenant.Status is TenantStatus.Suspended)
        {
            await TenantHttpErrors.WriteAsync(context, StatusCodes.Status403Forbidden, "Tenant.Suspended", "تم تعليق حساب المتجر، يرجى التواصل مع الدعم");
            return;
        }

        tenantContextSetter.Set(tenant, IsReadOnly(tenant, dateTimeProvider.UtcNow));

        await next.Invoke(context);
    }

    internal static bool IsReadOnly(TenantInfo tenant, DateTime utcNow)
    {
        var now = new DateTimeOffset(utcNow, TimeSpan.Zero);

        return tenant.Status switch
        {
            TenantStatus.Expired => true,
            TenantStatus.Trial when tenant.TrialEndsAtUtc <= now => true,
            TenantStatus.Active when tenant.SubscriptionExpiresAtUtc <= now => true,
            _ => false
        };
    }

    internal static string? ExtractSubdomain(string host, string baseDomain)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(baseDomain))
        {
            return null;
        }

        if (host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase) ||
            host.Equals($"www.{baseDomain}", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!host.EndsWith($".{baseDomain}", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string prefix = host[..^(baseDomain.Length + 1)];

        // Multi-level subdomains are not supported.
        if (prefix.Length == 0 || prefix.Contains('.', StringComparison.Ordinal))
        {
            return null;
        }

        return prefix.ToLowerInvariant();
    }
}
