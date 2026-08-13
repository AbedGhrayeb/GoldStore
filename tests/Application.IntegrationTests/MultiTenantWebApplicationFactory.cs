using Application.Abstractions.Authentication;
using Application.Abstractions.Tenants;
using Domain.Authorization;
using Domain.Catalog;
using Domain.Suppliers;
using Domain.Tenants;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Result;
using WebUI;
using Xunit;

namespace Application.IntegrationTests;

/// <summary>
/// Real SQL Server-backed test host (plan Phase 8). Each factory instance gets a
/// brand-new, randomly named database so test classes never share state. The host is
/// started through <c>Program.Main</c>, which applies migrations and seeds the initial
/// tenant; the fixture then seeds a second fully-provisioned tenant and a set of
/// eligibility fixtures (pending, grace, expired, disabled) with deliberately similar
/// data so isolation failures are easy to detect.
/// </summary>
public sealed class MultiTenantWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestPassword = "Test@Store#1234!";
    public const string StoreAdministratorRoleKey = "store_admin";

    public const string TenantAKey = InitialTenant.Key;
    public const string TenantBKey = "second-store";

    public string DbName { get; } = $"GoldStoreDb_Tests_{Guid.NewGuid():N}";

    public Guid TenantBId { get; } = Guid.CreateVersion7();

    public Supplier SupplierA { get; private set; } = null!;

    public Supplier SupplierB { get; private set; } = null!;

    public Category CategoryA { get; private set; } = null!;

    public Category CategoryB { get; private set; } = null!;

    public string ConnectionString { get; }

    public MultiTenantWebApplicationFactory()
    {
        ConnectionString =
            $"Server=.;Database={DbName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Hostname verification would require test subdomains; the JWT/cookie
                // tenant claims and EF filters are what the isolation tests exercise.
                ["Tenancy:RequireHostnameVerification"] = "false",
                // Keep the log noise down and avoid Seq network attempts.
                ["Serilog:MinimumLevel:Default"] = "Warning",
            });
        });

        // The app captures the connection string eagerly in Program.Main, before test
        // configuration overrides are applied. Re-register the DbContext so the host
        // targets the throwaway test database instead of the real one.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseSqlServer(ConnectionString,
                    sqlServerOptions => sqlServerOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName));
            });

            // Seeding and requests run without an HTTP user context, which would leave
            // audit columns unset and break handlers that read CreatedAtUtc. Use a fixed
            // test identity so every write in the test host is audited.
            services.AddScoped<IUserContext>(_ => new TestUserContext());
        });
    }

    public async Task InitializeAsync()
    {
        await DropDatabaseAsync();

        // Starting the server runs Program.Main, which migrates the (empty) database
        // and seeds the initial tenant "goldstore".
        _ = CreateClient();

        await SeedTestDataAsync();
    }

    public new async Task DisposeAsync()
    {
        try
        {
            await DropDatabaseAsync();
        }
        finally
        {
            await base.DisposeAsync();
        }
    }

    // ─── Helpers for tests ────────────────────────────────────────────────────

    /// <summary>Opens a scoped DbContext bound to the given ambient tenant.</summary>
    public async Task<(IServiceScope Scope, ApplicationDbContext Db)> OpenTenantContextAsync(
        Guid tenantId, string tenantKey)
    {
        IServiceScope scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICurrentTenantSetter>().Set(tenantId, tenantKey);
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (scope, db);
    }

    /// <summary>Opens a scoped DbContext with no ambient tenant (deny-by-default).</summary>
    public (IServiceScope Scope, ApplicationDbContext Db) OpenNoTenantContext()
    {
        IServiceScope scope = Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (scope, db);
    }

    /// <summary>Performs a real cookie login (GET login page, POST credentials, follow redirects).</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        HttpClient client = CreateClient();
        bool loggedIn = await TestAuth.LoginAsync(client, email, password);
        Assert.True(loggedIn, $"Login failed for {email}.");
        return client;
    }

    // ─── Seeding ──────────────────────────────────────────────────────────────

    private async Task SeedTestDataAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ICurrentTenantSetter setter = scope.ServiceProvider.GetRequiredService<ICurrentTenantSetter>();
        IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        TimeProvider time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        DateTimeOffset utcNow = time.GetUtcNow();

        Guid tenantAId = InitialTenant.Id;

        Role storeAdmin = await db.Roles.SingleAsync(r => r.Key == StoreAdministratorRoleKey, CancellationToken.None);
        SubscriptionPlan plan = await db.SubscriptionPlans.SingleAsync(cancellationToken: CancellationToken.None);

        // ── Tenant B: active, fully provisioned, mirrors tenant A's shape ──
        setter.Set(TenantBId, TenantBKey);
        db.Tenants.Add(Tenant.Create(TenantBId, "Second Store", TenantBKey, TenantStatus.Active).Value);
        db.TenantSubscriptions.Add(TenantSubscription
            .Create(TenantBId, plan.Id, SubscriptionBillingCycle.Annual, utcNow, utcNow.AddYears(1)).Value);
        db.TenantSettings.Add(TenantSettings
            .Create(TenantBId, "Second Store", logoUrl: null, timeZoneId: "Asia/Amman", enabledFeatures: [.. Domain.Tenants.Features.All]).Value);
        User userB = CreateUser(hasher, TenantBId, "admin@second-store.goldstore.test", "Second");
        db.Users.Add(userB);
        db.UserRoles.Add(UserRole.Create(TenantBId, userB.Id, storeAdmin.Id).Value);
        SupplierB = Supplier.Create("Gold House", "0791111111", secondaryPhone: null, bankAccountNumber: null, notes: "second store").Value;
        CategoryB = Category.Create(parentCategoryId: null, name: "خواتم", description: "rings").Value;
        db.Suppliers.Add(SupplierB);
        db.Categories.Add(CategoryB);
        await db.SaveChangesAsync(CancellationToken.None);

        // ── Tenant A: deliberately similar rows, saved under A's ambient tenant ──
        setter.Set(tenantAId, TenantAKey);
        SupplierA = Supplier.Create("Gold House", "0790000000", secondaryPhone: null, bankAccountNumber: null, notes: "initial store").Value;
        CategoryA = Category.Create(parentCategoryId: null, name: "خواتم", description: "rings").Value;
        db.Suppliers.Add(SupplierA);
        db.Categories.Add(CategoryA);
        await db.SaveChangesAsync(CancellationToken.None);

        // ── Eligibility fixtures (plan Phase 8 item 6) ──
        await SeedEligibilityTenantAsync(db, setter, hasher, storeAdmin, plan, utcNow, "pending-store", TenantStatus.Pending, graceUntilUtc: null);
        await SeedEligibilityTenantAsync(db, setter, hasher, storeAdmin, plan, utcNow, "grace-store", TenantStatus.Cancelled, graceUntilUtc: utcNow.AddDays(30));
        await SeedEligibilityTenantAsync(db, setter, hasher, storeAdmin, plan, utcNow, "expired-store", TenantStatus.Cancelled, graceUntilUtc: utcNow.AddDays(-1));

        // Disabled user in the initial tenant.
        setter.Set(tenantAId, TenantAKey);
        User disabled = CreateUser(hasher, tenantAId, "disabled@goldstore.test", "Disabled");
        disabled.IsActive = false;
        db.Users.Add(disabled);
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task SeedEligibilityTenantAsync(
        ApplicationDbContext db,
        ICurrentTenantSetter setter,
        IPasswordHasher hasher,
        Role storeAdmin,
        SubscriptionPlan plan,
        DateTimeOffset utcNow,
        string key,
        TenantStatus status,
        DateTimeOffset? graceUntilUtc)
    {
        Guid tenantId = Guid.CreateVersion7();
        setter.Set(tenantId, key);

        Tenant tenant = Tenant.Create(tenantId, key, key, TenantStatus.Active).Value;
        if (status == TenantStatus.Cancelled)
        {
            // The domain rejects a grace window that has already ended, so cancel with a
            // future grace first, then backdate the grace through reflection.
            DateTimeOffset futureGrace = graceUntilUtc is not null && graceUntilUtc > utcNow
                ? graceUntilUtc.Value
                : utcNow.AddDays(30);
            tenant.Cancel(futureGrace);
            if (graceUntilUtc is not null && graceUntilUtc < utcNow)
            {
                SetCancellationGrace(tenant, graceUntilUtc.Value);
            }
        }
        else if (status == TenantStatus.Pending)
        {
            SetPending(tenant);
        }

        db.Tenants.Add(tenant);
        db.TenantSubscriptions.Add(TenantSubscription
            .Create(tenantId, plan.Id, SubscriptionBillingCycle.Annual, utcNow, utcNow.AddYears(1)).Value);
        db.TenantSettings.Add(TenantSettings
            .Create(tenantId, key, logoUrl: null, timeZoneId: "Asia/Amman", enabledFeatures: [.. Domain.Tenants.Features.All]).Value);

        User user = CreateUser(hasher, tenantId, $"admin@{key}.goldstore.test", key);
        db.Users.Add(user);
        db.UserRoles.Add(UserRole.Create(tenantId, user.Id, storeAdmin.Id).Value);

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private static User CreateUser(IPasswordHasher hasher, Guid tenantId, string email, string suffix) =>
        User.Create(Guid.CreateVersion7(), tenantId, email, suffix, "Admin", hasher.Hash(TestPassword)).Value;

    /// <summary>Fixed identity for the test host; the real UserContext reads an HTTP
    /// context that is absent during seeding and background flows.</summary>
    private sealed class TestUserContext : IUserContext
    {
        public bool IsAvailable => true;

        public Guid UserId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    private static void SetCancellationGrace(Tenant tenant, DateTimeOffset graceUntilUtc)
    {
        tenant.GetType().GetProperty(nameof(Tenant.CancellationReadOnlyUntilUtc))!
            .SetValue(tenant, graceUntilUtc);
    }

    private static void SetPending(Tenant tenant)
    {
        tenant.GetType().GetProperty(nameof(Tenant.Status))!.SetValue(tenant, TenantStatus.Pending);
    }

    private async Task DropDatabaseAsync()
    {
        await using SqlConnection connection = new("Server=.;Trusted_Connection=True;TrustServerCertificate=True;");
        await connection.OpenAsync();
        await using SqlCommand command = new(
            $"IF DB_ID('{DbName}') IS NOT NULL " +
            $"BEGIN ALTER DATABASE [{DbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DbName}]; END",
            connection);
        await command.ExecuteNonQueryAsync();
    }
}
