using System.Security.Claims;
using Application.Abstractions.Tenants;
using Infrastructure.Authentication;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog.Context;
using WebUI.Authorization;
using WebUI.Infrastructure;

namespace WebUI.Middleware;

/// <summary>
/// Resolves and enforces the ambient tenant for every authenticated request (plan Phase 2).
/// A request cannot reach a tenant endpoint without a valid, operational current tenant,
/// and the tenant always comes from the authenticated claims — a client can never switch
/// tenant by changing a header.
/// </summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IOptions<TenantHostOptions> options)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenant currentTenant,
        ILogger<TenantResolutionMiddleware> logger)
    {
        // Explicit exemptions: endpoint metadata ([HostOnly], [AllowAnonymous]) and
        // configured path prefixes. Tenant resolution is the default for everything else.
        if (!IsTenantResolutionRequired(context, options.Value))
        {
            await next(context);
            return;
        }

        // Public entry points (login, landing) are unauthenticated and have no tenant yet.
        ClaimsPrincipal user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (!HasTenantClaims(user))
        {
            logger.LogWarning(
                "Authenticated request reached tenant resolution without tenant claims. Path: {RequestPath}",
                context.Request.Path);
            await TenantProblemDetails.WriteAsync(context, TenantProblemDetails.MissingContext);
            return;
        }

        if (options.Value.RequireHostnameVerification && !await VerifyHostnameAsync(context, options.Value, user))
        {
            return;
        }

        if (!currentTenant.IsAvailable || !currentTenant.IsOperational)
        {
            logger.LogWarning(
                "Tenant is not operational for an authenticated request. Path: {RequestPath}",
                context.Request.Path);
            await TenantProblemDetails.WriteAsync(context, TenantProblemDetails.AccessDenied);
            return;
        }

        // Central read-only subscription gate (plan Phase 4 item 7): a cancelled tenant
        // inside its grace period may read but never write.
        if (currentTenant.IsReadOnly && !HttpMethods.IsGet(context.Request.Method))
        {
            logger.LogWarning(
                "Read-only tenant attempted a write operation. Path: {RequestPath}, Method: {Method}",
                context.Request.Path,
                context.Request.Method);
            await TenantProblemDetails.WriteAsync(context, TenantProblemDetails.ReadOnly);
            return;
        }

        // Structured logging scope: correlate every downstream log entry with the tenant.
        using (LogContext.PushProperty("TenantId", currentTenant.TenantId))
        using (LogContext.PushProperty("TenantKey", currentTenant.TenantKey))
        using (LogContext.PushProperty("UserId", user.FindFirstValue(ClaimTypes.NameIdentifier)))
        {
            await next(context);
        }
    }

    private static bool IsTenantResolutionRequired(HttpContext context, TenantHostOptions options)
    {
        Endpoint? endpoint = context.GetEndpoint();
        if (endpoint is not null)
        {
            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
                || endpoint.Metadata.GetMetadata<HostOnlyAttribute>() is not null)
            {
                return false;
            }
        }

        string path = context.Request.Path.Value ?? string.Empty;

        return !IsExemptPath(path, options.ExemptPaths);
    }

    private static bool IsExemptPath(string path, string[] exemptPaths)
    {
        foreach (string exempt in exemptPaths)
        {
            if (string.IsNullOrWhiteSpace(exempt))
            {
                continue;
            }

            if (path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTenantClaims(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(CustomClaims.TenantId), out Guid tenantId)
        && tenantId != Guid.Empty
        && !string.IsNullOrWhiteSpace(user.FindFirstValue(CustomClaims.TenantKey));

    private static async Task<bool> VerifyHostnameAsync(
        HttpContext context,
        TenantHostOptions options,
        ClaimsPrincipal user)
    {
        string? tenantKey = user.FindFirstValue(CustomClaims.TenantKey);
        if (string.IsNullOrWhiteSpace(tenantKey))
        {
            await TenantProblemDetails.WriteAsync(context, TenantProblemDetails.MissingContext);
            return false;
        }

        string requestHost = context.Request.Host.Host ?? string.Empty;
        string expectedHost = $"{tenantKey}.{options.BaseDomain}";

        if (string.Equals(requestHost, expectedHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(requestHost, options.PublicHost, StringComparison.OrdinalIgnoreCase))
        {
            // Authenticated on the public login host: move to the tenant's canonical
            // subdomain so branding and tenant settings are served from the right host.
            string target = $"{options.Scheme}://{expectedHost}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
            context.Response.Redirect(target, permanent: false);
            return false;
        }

        await TenantProblemDetails.WriteAsync(context, TenantProblemDetails.HostMismatch);
        return false;
    }
}
