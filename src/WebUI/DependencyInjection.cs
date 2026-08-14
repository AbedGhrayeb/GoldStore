using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using WebUI.Endpoints;
using WebUI.Infrastructure;
using WebUI.Infrastructure.OpenApi;
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
                .AddAppOutputCaching()
                .AddOpenApiDocumentation()
                .AddAngularCors();
        // ─── HTTP security headers ─────────────────────────────────────────────────────
        services.AddHsts(opts =>
        {
            opts.MaxAge = TimeSpan.FromDays(365);
            opts.IncludeSubDomains = true;
            opts.Preload = true;
        });
        return services;
    }

    /// <summary>
    /// Built-in OpenAPI document (plan Phase 7a) with the JWT bearer security scheme and
    /// document metadata. Served through the Scalar API reference UI in Program.cs.
    /// </summary>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "GoldStore API",
                    Version = ApiRoutes.Version,
                    Description = "Tenant-isolated API for the GoldStore ERP. " +
                        "Tenant endpoints require a bearer token from POST /api/v1/auth/login; " +
                        "endpoints never accept a tenant id — the tenant is resolved from the token claims."
                };
                return Task.CompletedTask;
            });

            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        return services;
    }

    /// <summary>
    /// Cross-origin policy for the Angular client. The production client is served from
    /// the same tenant subdomain as the API (same-origin, no CORS needed), so this mainly
    /// enables the local Angular dev server and tenant subdomains of the public domain.
    /// </summary>
    public static IServiceCollection AddAngularCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngularApp", policy =>
            {
                policy.SetIsOriginAllowed(IsAllowedAngularOrigin)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static bool IsAllowedAngularOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && uri.Port == 4200)
        {
            return true;
        }

        return uri.Host.Equals("goldstore.app", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".goldstore.app", StringComparison.OrdinalIgnoreCase);
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

            // Strict, IP-partitioned policy for authentication endpoints (M7-B2). The
            // permit/window limits are read per-request from the *runtime* configuration
            // (not the bootstrap builder.Configuration snapshot) so tests can shrink them
            // and the deployment can tune them without a rebuild. The app trusts only its
            // own reverse proxy (ForwardedHeaders KnownProxies = loopback), so the key is
            // the real client address when nginx fronts it.
            options.AddPolicy("LoginLimiter", context =>
            {
                IConfiguration runtime = context.RequestServices.GetRequiredService<IConfiguration>();
                int permitLimit = runtime.GetValue("RateLimiting:Login:PermitLimit", 10);
                int windowSeconds = runtime.GetValue("RateLimiting:Login:WindowSeconds", 60);

                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                IConfiguration runtime = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                context.HttpContext.Response.Headers.RetryAfter =
                    runtime.GetValue("RateLimiting:Login:WindowSeconds", 60).ToString();
                return ValueTask.CompletedTask;
            };
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
