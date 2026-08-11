using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Services;
using Application.Abstractions.Tenants;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Database;
using Infrastructure.Database.Interceptors;
using Infrastructure.DomainEvents;
using Infrastructure.GoldPrices;
using Infrastructure.Tenants;
using Infrastructure.Tenancy;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services
 .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
 .AddCookie(opts =>
 {
     opts.LoginPath = "/Account/Login";
     opts.LogoutPath = "/Account/Logout";
     opts.AccessDeniedPath = "/Account/AccessDenied";

     // Sliding session — each request resets the expiry
     opts.SlidingExpiration = true;
     opts.ExpireTimeSpan = TimeSpan.FromHours(8);

         // Cookie hardening
         opts.Cookie.Name = "GoldStoreAuth.Session";
         opts.Cookie.HttpOnly = true;              // JS cannot read the cookie
         opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
         opts.Cookie.SameSite = SameSiteMode.Strict;
         opts.Cookie.IsEssential = true;

         // When hostname verification is enabled, share the session cookie across
         // tenant subdomains of the parent domain so a login on the public host
         // carries over to https://{tenant-key}.{BaseDomain} after the redirect.
         TenantHostOptions tenancy = configuration.GetSection(TenantHostOptions.SectionName)
             .Get<TenantHostOptions>() ?? new TenantHostOptions();
         if (!string.IsNullOrWhiteSpace(tenancy.CookieDomain))
         {
             opts.Cookie.Domain = tenancy.CookieDomain;
         }
     });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthSessionManager, CookieAuthSessionManager>();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}
