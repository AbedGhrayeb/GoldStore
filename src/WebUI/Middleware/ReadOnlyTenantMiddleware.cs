// <copyright file="ReadOnlyTenantMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Frozen;
using Application.Abstractions.Tenancy;
using Microsoft.AspNetCore.Http;

namespace WebUI.Middleware;

public sealed class ReadOnlyTenantMiddleware(RequestDelegate next)
{
    private static readonly FrozenSet<string> WriteMethods = FrozenSet.ToFrozenSet(
        new[] { HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete },
        StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> ExemptPaths = FrozenSet.ToFrozenSet(
        new[]
        {
            "/api/identity/token",
            "/api/identity/refresh-token",
            "/api/tenant/subscription/renew",
        },
        StringComparer.OrdinalIgnoreCase);

    public async Task Invoke(HttpContext context, ITenantContext tenantContext)
    {
        if (IsBlockedWrite(context, tenantContext))
        {
            await TenantHttpErrors.WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Tenant.ReadOnly",
                "انتهت صلاحية اشتراك المتجر، الوضع للقراءة فقط. يرجى تجديد الاشتراك من لوحة الإدارة");

            return;
        }

        await next.Invoke(context);
    }

    private static bool IsBlockedWrite(HttpContext context, ITenantContext tenantContext)
    {
        if (!tenantContext.IsResolved || !tenantContext.IsReadOnly)
        {
            return false;
        }

        PathString path = context.Request.Path;

        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!WriteMethods.Contains(context.Request.Method))
        {
            return false;
        }

        return !ExemptPaths.Contains(path.Value ?? string.Empty);
    }
}
