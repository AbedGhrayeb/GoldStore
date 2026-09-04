using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Caching;
using Application.Abstractions.Data;
using Application.Abstractions.Services;
using Application.Abstractions.Subscriptions;
using Application.Abstractions.Tenants;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Caching;
using Infrastructure.Data;
using Infrastructure.Database;
using Infrastructure.Database.Interceptors;
using Infrastructure.DomainEvents;
using Infrastructure.GoldPrices;
using Infrastructure.HealthChecks;
using Infrastructure.Invoices;
using Infrastructure.Phone;
using Infrastructure.Subscriptions;
using Infrastructure.Tenancy;
using Infrastructure.Tenants;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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

        // HybridCache-backed tenant-scoped cache (M7-C1); entries are evicted by the
        // KpiCacheInvalidationInterceptor on any tenant write.
        services.AddSingleton<ICacheService, HybridCacheService>();

        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        services.Configure<GoldApiOptions>(configuration.GetSection("GoldApi"));
        services.Configure<TenantHostOptions>(configuration.GetSection(TenantHostOptions.SectionName));
        services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));
        services.Configure<TwoFactorOptions>(configuration.GetSection(TwoFactorOptions.SectionName));

        // HybridCache (in-memory L1 only per M7 decision 3; no Redis L2). Registered
        // per-tenant stampede protection for the gold price and any keyed caches.
        services.AddHybridCache();

        services.AddHttpClient("GoldApi", client =>
        {
            client.BaseAddress = new Uri("https://www.goldapi.io");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IGoldPriceService, GoldPriceService>();

        // Central subscription quota gate (plan Phase 4 item 7).
        services.AddScoped<ISubscriptionGate, SubscriptionGate>();
        services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();

        // Firebase: initialize DefaultInstance only when ProjectId + credentials are configured.
        // Development can run without Firebase via LogPhoneVerifier (dev-token:+970...) .
        try
        {
            string? projectId = configuration["Firebase:ProjectId"];
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                string? svc = configuration["Firebase:ServiceAccountJsonPath"];
                FirebaseAdmin.FirebaseApp app = string.IsNullOrWhiteSpace(svc) || !File.Exists(svc)
                    ? FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions { ProjectId = projectId })
                    : FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
                    {
                        ProjectId = projectId,
                        Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(svc)
                    });
                _ = app;
            }
        }
        catch
        {
            // Defer to runtime verifier — AllowDevTokens will cover dev
        }

        services.AddScoped<Application.Abstractions.Phone.IPhoneVerifier>(sp =>
        {
            IWebHostEnvironment env = sp.GetRequiredService<IWebHostEnvironment>();
            IConfiguration cfg = sp.GetRequiredService<IConfiguration>();
            IOptions<FirebaseOptions> fb = sp.GetRequiredService<IOptions<FirebaseOptions>>();
            bool hasProject = !string.IsNullOrWhiteSpace(fb.Value.ProjectId) && FirebaseAdmin.FirebaseApp.DefaultInstance is not null;
            return hasProject
                ? new FirebasePhoneVerifier(fb)
                : new LogPhoneVerifier(env, cfg) as Application.Abstractions.Phone.IPhoneVerifier;
        });
        services.AddSingleton<Application.Abstractions.Phone.ITwoFactorTicketService, TwoFactorTicketService>();
        services.AddSingleton<Application.Abstractions.Phone.IPhoneRecoveryCodeService, PhoneRecoveryCodeService>();

        // Ambient tenant for the current scope (claims-based on HTTP requests,
        // explicitly selected for host/background flows).
        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());
        services.AddScoped<ICurrentTenantSetter>(sp => sp.GetRequiredService<CurrentTenant>());

        // Order matters: the tenant write guard runs before the audit interceptor.
        services.AddScoped<ISaveChangesInterceptor, TenantEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, KpiCacheInvalidationInterceptor>();

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

        // Readiness check pings the database through the app's own DbContext so the
        // configured connection string is validated end to end (plan Phase 9 / M7 A3).
        services.AddScoped<DatabaseHealthCheck>();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

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

                // Browser clients receive this JWT in an HttpOnly cookie. Header credentials
                // keep precedence so external API clients can continue to use bearer tokens.
                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            context.Token = context.Request.Cookies[JwtCookieDefaults.TenantAccessCookieName];
                        }

                        return Task.CompletedTask;
                    },
                };
            })
            .AddJwtBearer(HostAuthDefaults.AuthenticationScheme, opts =>
            {
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
                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            context.Token = context.Request.Cookies[JwtCookieDefaults.HostAccessCookieName];
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthSessionManager, CookieAuthSessionManager>();
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddScoped<IPlatformTokenProvider, PlatformTokenProvider>();
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
