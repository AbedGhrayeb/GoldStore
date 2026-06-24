using WebUI.Infrastructure;

namespace WebUI;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // MVC
        services.AddControllersWithViews();
        // Razor Pages
        services.AddAuthorization();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        // ─── HTTP security headers ─────────────────────────────────────────────────────
        services.AddHsts(opts =>
        {
            opts.MaxAge = TimeSpan.FromDays(365);
            opts.IncludeSubDomains = true;
            opts.Preload = true;
        });
        return services;
    }
}
