using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Services;
using Application.Abstractions.Tenancy;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Database;
using Infrastructure.Database.Interceptors;
using Infrastructure.DomainEvents;
using Infrastructure.GoldPrices;
using Infrastructure.Platform;
using Infrastructure.Tenancy;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
            .AddTenancy(configuration)
            .AddAuthenticationInternal()
            .AddAuthorizationInternal();

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        services.Configure<GoldApiOptions>(configuration.GetSection("GoldApi"));
        services.AddMemoryCache();
        services.AddHttpClient("GoldApi", client =>
        {
            client.BaseAddress = new Uri("https://www.goldapi.io");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IGoldPriceService, GoldPriceService>();
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            // Escape hatch: tenants with a dedicated connection string run on their own
            // database; everyone else shares the main database (schema per tenant).
            ITenantContext tenantContext = sp.GetRequiredService<ITenantContext>();
            string effectiveConnectionString = tenantContext is { IsResolved: true, ConnectionString: not null }
                ? tenantContext.ConnectionString
                : connectionString!;

            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            // The history table lives in the tenant schema (placeholder $tenant) so each
            // tenant tracks its own applied migrations — a shared dbo history table would
            // make the idempotent provision script skip all tables for later tenants.
            options.UseSqlServer(effectiveConnectionString, sqlServerOptions =>
                             sqlServerOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ApplicationDbContext.PlaceholderSchema));
        });

        // Platform catalog (tenants, plans, subscriptions, platform admins).
        // Same SQL Server database, isolated under the "platform" schema.
        // No interceptors: auditing is tenant-user based and does not apply here.
        services.AddDbContext<PlatformDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
                             sqlServerOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, PlatformDbContext.Schema));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPlatformDbContext>(sp => sp.GetRequiredService<PlatformDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();
        services.AddScoped<PlatformDbContextInitializer>();
        return services;
    }

    private static IServiceCollection AddTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenancyOptions>(configuration.GetSection(TenancyOptions.SectionName));

        // One scoped instance behind both contracts: readers use ITenantContext,
        // the middleware/background services use ITenantContextSetter to establish it.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextSetter>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantResolver, TenantResolver>();

        // Singletons: both create their own scopes for tenant-scoped work.
        services.AddSingleton<ITenantSchemaProvisioner, TenantSchemaProvisioner>();
        services.AddSingleton<ITenantSeeder, TenantSeeder>();

        // Startup migration runner for all active tenants (adopts legacy schemas too).
        services.AddSingleton<ITenantMigrationRunner, TenantMigrationRunner>();
        services.AddHostedService<TenantMigrationHostedService>();

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services)
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
 });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthSessionManager, CookieAuthSessionManager>();
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddSingleton<IPlatformTokenProvider, PlatformTokenProvider>();

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
