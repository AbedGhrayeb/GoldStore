using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Services;
using Application.Abstractions.Subscriptions;
using Application.Abstractions.Tenants;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Database;
using Infrastructure.Database.Interceptors;
using Infrastructure.DomainEvents;
using Infrastructure.GoldPrices;
using Infrastructure.Subscriptions;
using Infrastructure.Tenants;
using Infrastructure.Tenancy;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices(configuration)
            .AddDatabase(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal();

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton(TimeProvider.System);

        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        services.Configure<GoldApiOptions>(configuration.GetSection("GoldApi"));
        services.Configure<TenantHostOptions>(configuration.GetSection(TenantHostOptions.SectionName));
        services.AddMemoryCache();
        services.AddHttpClient("GoldApi", client =>
        {
            client.BaseAddress = new Uri("https://www.goldapi.io");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IGoldPriceService, GoldPriceService>();

        // Central subscription quota gate (plan Phase 4 item 7).
        services.AddScoped<ISubscriptionGate, SubscriptionGate>();

        // Ambient tenant for the current scope (claims-based on HTTP requests,
        // explicitly selected for host/background flows).
        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());
        services.AddScoped<ICurrentTenantSetter>(sp => sp.GetRequiredService<CurrentTenant>());

        // Order matters: the tenant write guard runs before the audit interceptor.
        services.AddScoped<ISaveChangesInterceptor, TenantEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

         services.AddDbContext<ApplicationDbContext>((sp, options) =>
         {
             options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
             options.UseSqlServer(connectionString, sqlServerOptions =>
                              sqlServerOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName));
         });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();
        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string issuer = configuration["Jwt:Issuer"]!;
        string audience = configuration["Jwt:Audience"]!;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opts =>
            {
                opts.LoginPath = "/Account/Login";
                opts.LogoutPath = "/Account/Logout";
                opts.AccessDeniedPath = "/Account/AccessDenied";

                // Sliding session — each request resets the expiry
                opts.SlidingExpiration = true;
                opts.ExpireTimeSpan = TimeSpan.FromHours(8);

                // Pick the authentication scheme by path: JSON APIs use bearer tokens,
                // host administration uses the dedicated host cookie, everything else
                // (the MVC store) uses the tenant cookie.
                opts.ForwardDefaultSelector = context =>
                    context.Request.Path.StartsWithSegments("/api")
                        ? JwtBearerDefaults.AuthenticationScheme
                        : context.Request.Path.StartsWithSegments("/host")
                            ? HostAuthDefaults.AuthenticationScheme
                            : null;

                // Cookie hardening
                opts.Cookie.Name = "GoldStoreAuth.Session";
                opts.Cookie.HttpOnly = true;              // JS cannot read the cookie
                opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opts.Cookie.SameSite = SameSiteMode.Strict;
                opts.Cookie.IsEssential = true;

                // Revoke cookie sessions when the user's security stamp changes
                // (password change, account disable) — checked at most every 30 minutes.
                opts.Events.OnValidatePrincipal = async context =>
                {
                    SecurityStampValidator validator = context.HttpContext.RequestServices
                        .GetRequiredService<SecurityStampValidator>();
                    await validator.ValidateAsync(context);
                };

                // When hostname verification is enabled, share the session cookie across
                // tenant subdomains of the parent domain so a login on the public host
                // carries over to https://{tenant-key}.{BaseDomain} after the redirect.
                TenantHostOptions tenancy = configuration.GetSection(TenantHostOptions.SectionName)
                    .Get<TenantHostOptions>() ?? new TenantHostOptions();
                if (!string.IsNullOrWhiteSpace(tenancy.CookieDomain))
                {
                    opts.Cookie.Domain = tenancy.CookieDomain;
                }
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                // Map JWT short claim names (sub/nameid/role) back to ClaimTypes so the
                // rest of the codebase reads the principal with standard claim types.
                opts.MapInboundClaims = true;

                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                // Tenant status (Active / trial / cancellation read-only grace) is enforced
                // centrally by TenantResolutionMiddleware on every authenticated request, so
                // no per-request database hit is needed here (plan Phase 4 item 4).
            })
            .AddCookie(HostAuthDefaults.AuthenticationScheme, opts =>
            {
                opts.LoginPath = "/host/login";
                opts.LogoutPath = "/host/logout";
                opts.AccessDeniedPath = "/host/login";

                opts.SlidingExpiration = true;
                opts.ExpireTimeSpan = TimeSpan.FromHours(8);

                opts.Cookie.Name = "GoldStoreHost.Session";
                opts.Cookie.HttpOnly = true;
                opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opts.Cookie.SameSite = SameSiteMode.Strict;
                opts.Cookie.IsEssential = true;
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthSessionManager, CookieAuthSessionManager>();
        services.AddScoped<IPlatformAuthSessionManager, PlatformAuthSessionManager>();
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<SecurityStampValidator>();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddTransient<IAuthorizationHandler, FeatureAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}
