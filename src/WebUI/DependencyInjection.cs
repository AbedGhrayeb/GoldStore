using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using WebUI.Infrastructure;
using WebUI.OpenApi.Transformers;
namespace WebUI;

public static class DependencyInjection
{

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        //API
        services.AddControllers();
        // MVC
        services.AddControllersWithViews();

        services.AddAuthorization()
            .AddIdentityInfrastructure(configuration)
                .AddExceptionHandling()
                .AddControllerWithJsonConfiguration()
                .AddValidation()
                .AddAppRateLimiting()
                .AddAppOutputCaching()
                .AddApiDocumentation();
        ;
        // ─── HTTP security headers ─────────────────────────────────────────────────────
        services.AddHsts(opts =>
        {
            opts.MaxAge = TimeSpan.FromDays(365);
            opts.IncludeSubDomains = true;
            opts.Preload = true;
        });
        return services;
    }

    private static IServiceCollection AddAppOutputCaching(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.SizeLimit = 100 * 1024 * 1024; // 100 mb
            options.AddBasePolicy(policy =>
                policy.Expire(TimeSpan.FromSeconds(60)));
        });

        return services;
    }

    private static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
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


    private static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
    private static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        string[] versions = ["v1"];

        foreach (var version in versions)
        {
            services.AddOpenApi(version, options =>
            {
                // Versioning config
                options.AddDocumentTransformer<VersionInfoTransformer>();

                // Security Scheme config
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

                // Security Operation config
                options.AddOperationTransformer<BearerSecurityOperationTransformer>();
            });
        }

        return services;
    }

    private static IServiceCollection AddControllerWithJsonConfiguration(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options => options
            .JsonSerializerOptions
            .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        return services;
    }
    private static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddAuthentication(options =>
         {
             options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
         })
.AddJwtBearer(options =>
            {
                var jwtSettings = configuration.GetSection("Jwt");

                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)
                    )
                };
            })
            .AddJwtBearer(AuthConstants.PlatformBearerScheme, options =>
            {
                var jwtSettings = configuration.GetSection("Jwt");

                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["PlatformAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)
                    )
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("MvcPolicy", policy =>
            {
                policy.AuthenticationSchemes.Add(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                policy.RequireAuthenticatedUser();
            });

            options.AddPolicy("ApiPolicy", policy =>
            {
                policy.AuthenticationSchemes.Add(
                    JwtBearerDefaults.AuthenticationScheme);

                policy.RequireAuthenticatedUser();
            });

            options.AddPolicy(AuthConstants.PlatformApiPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(AuthConstants.PlatformBearerScheme);

                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthConstants.PlatformAdminRole);
            });
        });
        return services;
    }

}
