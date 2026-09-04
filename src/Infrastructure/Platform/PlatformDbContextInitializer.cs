using Application.Abstractions.Authentication;
using Domain.Common;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Result;

namespace Infrastructure.Platform;

public class PlatformDbContextInitializer(
    ILogger<PlatformDbContextInitializer> logger,
    PlatformDbContext context,
    IConfiguration configuration,
    IPasswordHasher passwordHasher)
{
    private readonly ILogger<PlatformDbContextInitializer> _logger = logger;
    private readonly PlatformDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the platform database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await TrySeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the platform database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    private async Task TrySeedAsync(CancellationToken cancellationToken)
    {
        if (!_context.Plans.Any())
        {
            Result<Plan> basicPlan = Plan.Create(
                "الباقة الأساسية",
                monthlyPrice: 15m,
                annualPrice: 150m,
                Currency.USD,
                description: "باقة البداية للمتاجر الصغيرة",
                featuresJson: """{"maxUsers":2,"modules":{"hr":false,"expenses":true}}""");

            Result<Plan> standardPlan = Plan.Create(
                "الباقة المعيارية",
                monthlyPrice: 25m,
                annualPrice: 250m,
                Currency.USD,
                description: "الباقة الأنسب لمتاجر المجوهرات النشطة",
                featuresJson: """{"maxUsers":5,"modules":{"hr":true,"expenses":true}}""");

            Result<Plan> premiumPlan = Plan.Create(
                "الباقة المميزة",
                monthlyPrice: 40m,
                annualPrice: 400m,
                Currency.USD,
                description: "كل الميزات مع دعم التخصيص المتقدم",
                featuresJson: """{"maxUsers":15,"modules":{"hr":true,"expenses":true},"advancedCustomization":true}""");

            _context.Plans.AddRange(basicPlan.Value, standardPlan.Value, premiumPlan.Value);
        }

        if (!_context.PlatformAdmins.Any())
        {
            string defaultPassword = _configuration["DefaultUserPassword"]!;
            string hashedPassword = _passwordHasher.Hash(defaultPassword);

            Result<PlatformAdmin> platformAdmin = PlatformAdmin.Create(
                "admin@platform.goldstore",
                "Platform",
                "Admin",
                hashedPassword);

            _context.PlatformAdmins.Add(platformAdmin.Value);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public static class PlatformInitializerExtensions
{
    public static async Task InitializePlatformDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = app.Services.CreateScope();

        PlatformDbContextInitializer initializer = scope.ServiceProvider.GetRequiredService<PlatformDbContextInitializer>();

        await initializer.InitializeAsync(cancellationToken);

        await initializer.SeedAsync(cancellationToken);
    }
}
