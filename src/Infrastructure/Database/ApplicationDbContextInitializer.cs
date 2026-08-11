using Application.Abstractions.Authentication;
using Application.Abstractions.Tenants;
using Domain.Common;
using Domain.Finance;
using Domain.Tenants;
using Domain.Users;
using Infrastructure.Database;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Result;

namespace Infrastructure.Data;

public class ApplicationDbContextInitializer(
    ILogger<ApplicationDbContextInitializer> logger,
    ApplicationDbContext context,
    IConfiguration configuration,
    IPasswordHasher passwordHasher,
    ICurrentTenantSetter currentTenantSetter,
    TimeProvider timeProvider)
{
    private readonly ILogger<ApplicationDbContextInitializer> _logger = logger;
    private readonly ApplicationDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ICurrentTenantSetter _currentTenantSetter = currentTenantSetter;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task InitializeAsync(CancellationToken cancellationToken=default)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task SeedAsync(CancellationToken cancellationToken=default)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await TrySeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task TrySeedAsync(CancellationToken cancellationToken=default)
    {
        // Host-level data first: plans and tenants are global and not tenant-filtered.
        SubscriptionPlan plan = await SeedSubscriptionPlanAsync(cancellationToken);
        await SeedInitialTenantAsync(cancellationToken);

        // Every tenant-owned row below belongs to the initial tenant. Selecting it
        // explicitly lets the write guard stamp rows and lets queries pass the
        // global tenant filter.
        _currentTenantSetter.Set(InitialTenant.Id, InitialTenant.Key);

        await SeedInitialSubscriptionAsync(plan, cancellationToken);
        await SeedInitialSettingsAsync(cancellationToken);
        await SeedDefaultUserAsync(cancellationToken);
        await SeedDefaultFinancialAccountsAsync(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubscriptionPlan> SeedSubscriptionPlanAsync(CancellationToken cancellationToken)
    {
        SubscriptionPlan? plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Key == "standard", cancellationToken);

        if (plan is not null)
        {
            return plan;
        }

        // Unlimited default plan; limits are introduced with real subscriptions later.
        plan = SubscriptionPlan.Create("Standard", "standard", null, null, null, null).Value;
        _context.SubscriptionPlans.Add(plan);

        return plan;
    }

    private async Task SeedInitialTenantAsync(CancellationToken cancellationToken)
    {
        if (await _context.Tenants.AnyAsync(t => t.Id == InitialTenant.Id, cancellationToken))
        {
            return;
        }

        Result<Tenant> tenant = Tenant.Create(InitialTenant.Id, InitialTenant.Name, InitialTenant.Key, TenantStatus.Active);
        _context.Tenants.Add(tenant.Value);
    }

    private async Task SeedInitialSubscriptionAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        if (await _context.TenantSubscriptions.AnyAsync(s => s.TenantId == InitialTenant.Id, cancellationToken))
        {
            return;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Result<TenantSubscription> subscription = TenantSubscription.Create(
            InitialTenant.Id, plan.Id, SubscriptionBillingCycle.Annual, utcNow, utcNow.AddYears(100));

        _context.TenantSubscriptions.Add(subscription.Value);
    }

    private async Task SeedInitialSettingsAsync(CancellationToken cancellationToken)
    {
        if (await _context.TenantSettings.AnyAsync(s => s.TenantId == InitialTenant.Id, cancellationToken))
        {
            return;
        }

        Result<TenantSettings> settings = TenantSettings.Create(
            InitialTenant.Id, InitialTenant.Name, logoUrl: null, timeZoneId: "Asia/Amman");

        _context.TenantSettings.Add(settings.Value);
    }

    private async Task SeedDefaultUserAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        string defaultPassword = _configuration["DefaultUserPassword"]!;
        string hashedPassword = _passwordHasher.Hash(defaultPassword);
        Result<User> defaultUser = User.Create(Guid.CreateVersion7(), InitialTenant.Id, "admin@goldstore", "Admin", "Admin", hashedPassword);

        _context.Users.Add(defaultUser.Value);
    }

    private async Task SeedDefaultFinancialAccountsAsync(CancellationToken cancellationToken)
    {
        if (await _context.FinancialAccounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedAccounts = new List<FinancialAccount>();

        Result<FinancialAccount> jodAccount = FinancialAccount.Create("صندوق الدينار الرئيسي", Currency.JOD, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الدينار الكاش الرئيسي");
        Result<FinancialAccount> usdAccount = FinancialAccount.Create("صندوق الدولار الرئيسي", Currency.USD, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الدولار الكاش الرئيسي");
        Result<FinancialAccount> ilsAccount = FinancialAccount.Create("صندوق الدينار الرئيسي", Currency.ILS, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الشيكل الكاش الرئيسي");

        seedAccounts.AddRange(jodAccount.Value, usdAccount.Value, ilsAccount.Value);

        // TenantId is stamped by the tenant write guard from the ambient tenant.
        _context.FinancialAccounts.AddRange(seedAccounts);
    }
}

public static class InitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app,CancellationToken cancellationToken=default)
    {
        using IServiceScope scope = app.Services.CreateScope();

        ApplicationDbContextInitializer initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();

        await initializer.InitializeAsync(cancellationToken);

        await initializer.SeedAsync(cancellationToken);
    }
}
