using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using WebUI.Infrastructure;
namespace WebUI;

public static class DependencyInjection
{

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // MVC
        services.AddControllersWithViews();
        services.AddAuthorization()
                .AddExceptionHandling()
                .AddControllerWithJsonConfiguration()
                .AddValidation()
                .AddAppRateLimiting()
                .AddAppOutputCaching();
        // ─── HTTP security headers ─────────────────────────────────────────────────────
        services.AddHsts(opts =>
        {
            opts.MaxAge = TimeSpan.FromDays(365);
            opts.IncludeSubDomains = true;
            opts.Preload = true;
        });
        return services;
    }

    public static IServiceCollection AddAppOutputCaching(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.SizeLimit = 100 * 1024 * 1024; // 100 mb
            options.AddBasePolicy(policy =>
                policy.Expire(TimeSpan.FromSeconds(60)));
        });

        return services;
    }

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddSlidingWindowLimiter("SlidingWindow", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.SegmentsPerWindow = 6;
                limiterOptions.QueueLimit = 10;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.AutoReplenishment = true;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }


    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static IServiceCollection AddControllerWithJsonConfiguration(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options => options
            .JsonSerializerOptions
            .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        return services;
    }


}
