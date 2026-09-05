// <copyright file="MiddlewareExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using WebUI.Middleware;

namespace WebUI.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextLoggingMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantResolutionMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseTwoFactorEnforcement(this IApplicationBuilder app)
    {
        app.UseMiddleware<TwoFactorEnforcementMiddleware>();
        return app;
    }
}
