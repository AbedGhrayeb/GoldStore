using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Abstractions.Authentication;
using Application.Abstractions.Tenants;
using Domain.Authorization;
using Domain.Catalog;
using Domain.Common;
using Domain.Employees;
using Domain.Finance;
using Domain.Inventory;
using Domain.Suppliers;
using Domain.Tenants;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SharedKernel.Result;
using Testcontainers.PostgreSql;
using WebUI;
using Xunit;

namespace Application.IntegrationTests;

/// <summary>
/// PostgreSQL-backed test host via Testcontainers. Each factory instance gets a
/// brand-new randomly named database inside a shared postgres:18.6-alpine container
/// so test classes never share state. The host is started through Program.Main which
/// applies migrations and seeds the initial tenant.
/// </summary>
public class MultiTenantWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestPassword = "Test@Store#1234!";
    public const string StoreAdministratorRoleKey = "store_admin";

    public const string TenantAKey = InitialTenant.Key;
    public const string TenantBKey = "second-store";

    /// <summary>Admin of a fully-provisioned tenant that has the catalog feature disabled.</summary>
    public const string NoCatalogAdmin = "admin@no-catalog.goldstore.test";

    /// <summary>Admin of a tenant that has the supplier feature disabled.</summary>
    public const string NoSuppliersAdmin = "admin@no-suppliers.goldstore.test";

    /// <summary>Admin of a tenant that has the inventory feature disabled.</summary>
    public const string NoInventoryAdmin = "admin@no-inventory.goldstore.test";

    /// <summary>Admin of a tenant that has the sales feature disabled.</summary>
    public const string NoSalesAdmin = "admin@no-sales.goldstore.test";

    /// <summary>Admin of a tenant that has the purchases feature disabled.</summary>
    public const string NoPurchasesAdmin = "admin@no-purchases.goldstore.test";

    /// <summary>Admin of a tenant that has the finance feature disabled.</summary>
    public const string NoFinanceAdmin = "admin@no-finance.goldstore.test";

    /// <summary>Admin of a tenant that has the expenses feature disabled.</summary>
    public const string NoExpensesAdmin = "admin@no-expenses.goldstore.test";

    /// <summary>Admin of a tenant that has the HR feature disabled.</summary>
    public const string NoHrAdmin = "admin@no-hr.goldstore.test";

    /// <summary>Email of the user seeded with the pinned test identity (see <see cref="TestUserContext.Id"/>).</summary>
    public const string ProfileUserEmail = "me@goldstore.test";

    public string DbName { get; } = $"GoldStoreDb_Tests_{Guid.NewGuid():N}";

    public Guid TenantBId { get; } = Guid.CreateVersion7();

    public Supplier SupplierA { get; private set; } = null!;

    public Supplier SupplierB { get; private set; } = null!;

    public Category CategoryA { get; private set; } = null!;

    public Category CategoryB { get; private set; } = null!;

    public Employee EmployeeA { get; private set; } = null!;

    public Employee EmployeeB { get; private set; } = null!;

    public FinancialAccount FinancialAccountA { get; private set; } = null!;

    public FinancialAccount FinancialAccountB { get; private set; } = null!;

    // Shared container — started lazily once per test run, reused across factories
    private static readonly PostgreSqlContainer SharedContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18.6-alpine")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("Postgres-2026!")
        .WithPortBinding(5432, true)
        .Build();

    private static bool _sharedStarted;

    private static async Task EnsureContainerStartedAsync()
    {
        if (_sharedStarted)
        {
            return;
        }

        await SharedContainer.StartAsync();
        _sharedStarted = true;
    }

    private string SharedConnectionString => SharedContainer.GetConnectionString();

    public string ConnectionString { get; private set; } = null!;

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
                // Keep the strict login limiter out of the way of the general isolation
                // suite (many of these tests sign in repeatedly); the dedicated
                // AuthRateLimitingTests factory pins the 429 behaviour with a tiny limit.
                ["RateLimiting:Login:PermitLimit"] = "100000",
                // Point the app at the per-factory test database inside the container
                ["ConnectionStrings:Database"] = ConnectionString,
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
                options.UseNpgsql(ConnectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName))
                    .UseSnakeCaseNamingConvention();
            });

            // Seeding and requests run without an HTTP user context, which would leave
            // audit columns unset and break handlers that read CreatedAtUtc. Use a fixed
            // test identity so every write in the test host is audited.
            services.AddScoped<IUserContext>(_ => new TestUserContext());
        });
    }

    public async Task InitializeAsync()
    {
        await EnsureContainerStartedAsync();
        ConnectionString = BuildDatabaseConnectionString(DbName);
        await CreateDatabaseAsync();

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

    /// <summary>
    /// Signs in through the tenant JWT auth endpoint (<c>/api/v1/auth/login</c>) and returns a
    /// client with the <c>Authorization: Bearer &lt;token&gt;</c> header pre-set. Mirrors
    /// <see cref="CreateAuthenticatedClientAsync"/> for the tenant API surface (plan Phase 7a).
    /// </summary>
    public async Task<HttpClient> CreateJwtClientAsync(string email, string password)
    {
        HttpClient client = CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password });
        Assert.True(response.IsSuccessStatusCode, $"JWT login failed for {email}.");

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        string accessToken = body.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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
        EmployeeB = Employee.Create(
            "Second", "Seller", RoleEnum.Salesperson, 1000M, Currency.JOD, SalaryCycleEnum.Monthly, userId: null).Value;
        FinancialAccountB = FinancialAccount.Create(
            "API B3 JOD Cash", Currency.JOD, FinancialAccountType.Cash, "B3-JOD-B", notes: null).Value;
        db.Suppliers.Add(SupplierB);
        db.Categories.Add(CategoryB);
        db.Employees.Add(EmployeeB);
        db.FinancialAccounts.Add(FinancialAccountB);
        db.GoldLedgerEntries.Add(GoldLedgerEntry.Create(
            Karat.K21, 100M, GoldMovementType.Increase, GoldReferenceType.InventoryAdjustment,
            Guid.CreateVersion7(), "API B3 opening stock").Value);
        db.FinancialTransactions.Add(FinancialTransaction.Create(
            FinancialAccountB.Id, Currency.JOD, 100000M, FinancialTransactionType.Inflow,
            FinancialReferenceType.ManualAdjustment, Guid.CreateVersion7(), "API B3 opening balance").Value);
        await db.SaveChangesAsync(CancellationToken.None);

        // ── Tenant A: deliberately similar rows, saved under A's ambient tenant ──
        setter.Set(tenantAId, TenantAKey);
        SupplierA = Supplier.Create("Gold House", "0790000000", secondaryPhone: null, bankAccountNumber: null, notes: "initial store").Value;
        CategoryA = Category.Create(parentCategoryId: null, name: "خواتم", description: "rings").Value;
        EmployeeA = Employee.Create(
            "Initial", "Seller", RoleEnum.Salesperson, 1000M, Currency.JOD, SalaryCycleEnum.Monthly, userId: null).Value;
        FinancialAccountA = await db.FinancialAccounts
            .FirstAsync(account => account.Currency == Currency.JOD && account.AccountType == FinancialAccountType.Cash);
        db.Suppliers.Add(SupplierA);
        db.Categories.Add(CategoryA);
        db.Employees.Add(EmployeeA);
        db.GoldLedgerEntries.Add(GoldLedgerEntry.Create(
            Karat.K21, 100M, GoldMovementType.Increase, GoldReferenceType.InventoryAdjustment,
            Guid.CreateVersion7(), "API B3 opening stock").Value);
        db.FinancialTransactions.Add(FinancialTransaction.Create(
            FinancialAccountA.Id, Currency.JOD, 100000M, FinancialTransactionType.Inflow,
            FinancialReferenceType.ManualAdjustment, Guid.CreateVersion7(), "API B3 opening balance").Value);
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

        // The test host pins IUserContext.UserId to TestUserContext.Id for every scope;
        // seed a matching user so identity-relative endpoints (GET /api/v1/users/me)
        // resolve a real profile instead of a 404.
        db.Users.Add(User.Create(
            TestUserContext.Id, tenantAId, ProfileUserEmail, "Me", "Profile", hasher.Hash(TestPassword)).Value);

        await db.SaveChangesAsync(CancellationToken.None);

        // ── Feature-gate fixtures ──
        await SeedNoCatalogTenantAsync(db, setter, hasher, storeAdmin, plan, utcNow);
        await SeedFeatureRestrictedTenantAsync(
            db, setter, hasher, storeAdmin, plan, utcNow,
            "no-suppliers-store", "No Suppliers Store", NoSuppliersAdmin, "NoSuppliers", Domain.Tenants.Features.Suppliers);
        await SeedFeatureRestrictedTenantAsync(
            db, setter, hasher, storeAdmin, plan, utcNow,
            "no-inventory-store", "No Inventory Store", NoInventoryAdmin, "NoInventory", Domain.Tenants.Features.Inventory);
        await SeedFeatureRestrictedTenantAsync(
            db, setter, hasher, storeAdmin, plan, utcNow,
            "no-sales-store", "No Sales Store", NoSalesAdmin, "NoSales", Domain.Tenants.Features.Sales);
        await SeedFeatureRestrictedTenantAsync(
            db, setter, hasher, storeAdmin, plan, utcNow,
            "no-purchases-store", "No Purchases Store", NoPurchasesAdmin, "NoPurchases", Domain.Tenants.Features.Purchases);
        await SeedFeatureRestrictedTenantAsync(
            db, setter, hasher, storeAdmin, plan, utcNow,
            "no-finance-store", "No Finance Store", NoFinanceAdmin, "NoFinance", Domain.Tenants.Features.Finance);
        await SeedFeatureRestrictedTenantAsync(
            db, setter, hasher, storeAdmin, plan, utcNow,
            "no-expenses-store", "No Expenses Store", NoExpensesAdmin, "NoExpenses", Domain.Tenants.Features.Expenses);
        await SeedFeatureRestrictedTenantAsync(
            db, setter, hasher, storeAdmin, plan, utcNow,
            "no-hr-store", "No HR Store", NoHrAdmin, "NoHr", Domain.Tenants.Features.Hr);
    }

    private async Task SeedNoCatalogTenantAsync(
        ApplicationDbContext db,
        ICurrentTenantSetter setter,
        IPasswordHasher hasher,
        Role storeAdmin,
        SubscriptionPlan plan,
        DateTimeOffset utcNow)
    {
        var tenantId = Guid.CreateVersion7();
        setter.Set(tenantId, "no-catalog-store");

        db.Tenants.Add(Tenant.Create(tenantId, "No Catalog Store", "no-catalog-store", TenantStatus.Active).Value);
        db.TenantSubscriptions.Add(TenantSubscription
            .Create(tenantId, plan.Id, SubscriptionBillingCycle.Annual, utcNow, utcNow.AddYears(1)).Value);
        db.TenantSettings.Add(TenantSettings
            .Create(tenantId, "No Catalog Store", logoUrl: null, timeZoneId: "Asia/Amman",
                enabledFeatures: [.. Domain.Tenants.Features.All
                    .Where(feature => feature != Domain.Tenants.Features.Catalog)]).Value);

        User user = CreateUser(hasher, tenantId, NoCatalogAdmin, "NoCatalog");
        db.Users.Add(user);
        db.UserRoles.Add(UserRole.Create(tenantId, user.Id, storeAdmin.Id).Value);

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task SeedFeatureRestrictedTenantAsync(
        ApplicationDbContext db,
        ICurrentTenantSetter setter,
        IPasswordHasher hasher,
        Role storeAdmin,
        SubscriptionPlan plan,
        DateTimeOffset utcNow,
        string tenantKey,
        string tenantName,
        string adminEmail,
        string userSuffix,
        string disabledFeature)
    {
        var tenantId = Guid.CreateVersion7();
        setter.Set(tenantId, tenantKey);

        db.Tenants.Add(Tenant.Create(tenantId, tenantName, tenantKey, TenantStatus.Active).Value);
        db.TenantSubscriptions.Add(TenantSubscription
            .Create(tenantId, plan.Id, SubscriptionBillingCycle.Annual, utcNow, utcNow.AddYears(1)).Value);
        db.TenantSettings.Add(TenantSettings
            .Create(tenantId, tenantName, logoUrl: null, timeZoneId: "Asia/Amman",
                enabledFeatures: [.. Domain.Tenants.Features.All.Where(feature => feature != disabledFeature)]).Value);

        User user = CreateUser(hasher, tenantId, adminEmail, userSuffix);
        db.Users.Add(user);
        db.UserRoles.Add(UserRole.Create(tenantId, user.Id, storeAdmin.Id).Value);

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
        var tenantId = Guid.CreateVersion7();
        setter.Set(tenantId, key);

        Tenant tenant = Tenant.Create(tenantId, key, key, TenantStatus.Active).Value;
        if (status == TenantStatus.Cancelled)
        {
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
        internal static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public bool IsAvailable => true;

        public Guid UserId => Id;

        public Guid? UserIdOrNull => Id;
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

    private string BuildDatabaseConnectionString(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(SharedConnectionString)
        {
            Database = database
        };
        return builder.ConnectionString;
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(SharedConnectionString);
        await connection.OpenAsync();
        // Quote identifier to handle mixed-case / hyphenated names safely
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{DbName}\" TEMPLATE template0;", connection);
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42P04") // duplicate_database
        {
            // Already exists — safe to ignore for idempotent retry
        }
    }

    private async Task DropDatabaseAsync()
    {
        // Terminate backends first — WITH (FORCE) handles this in PG13+ but connector
        // still needs a clean connection to 'postgres'
        await using var connection = new NpgsqlConnection(SharedConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{DbName}\" WITH (FORCE);", connection);
        await command.ExecuteNonQueryAsync();
    }
}
