using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Services;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Database;
using Infrastructure.Database.Interceptors;
using Infrastructure.DomainEvents;
using Infrastructure.GoldPrices;
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
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString, sqlServerOptions =>
                             sqlServerOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();
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
