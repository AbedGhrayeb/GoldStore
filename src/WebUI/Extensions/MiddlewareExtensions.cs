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

    public static IApplicationBuilder UseReadOnlyTenantEnforcement(this IApplicationBuilder app)
    {
        app.UseMiddleware<ReadOnlyTenantMiddleware>();

        return app;
    }
}
