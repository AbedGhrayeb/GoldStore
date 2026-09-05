// <copyright file="ApplicationDbContextInitializer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Tenants;
using Domain.Authorization;
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
    private const string StoreAdministratorRoleKey = "store_admin";

    private readonly ILogger<ApplicationDbContextInitializer> logger = logger;
    private readonly ApplicationDbContext context = context;
    private readonly IConfiguration configuration = configuration;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly ICurrentTenantSetter currentTenantSetter = currentTenantSetter;
    private readonly TimeProvider timeProvider = timeProvider;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await this.context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await this.TrySeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task TrySeedAsync(CancellationToken cancellationToken = default)
    {
        // Host-level data first: plans and tenants are global and not tenant-filtered.
        SubscriptionPlan plan = await this.SeedSubscriptionPlanAsync(cancellationToken);
        await this.SeedInitialTenantAsync(cancellationToken);

        // Every tenant-owned row below belongs to the initial tenant. Selecting it
        // explicitly lets the write guard stamp rows and lets queries pass the
        // global tenant filter.
        this.currentTenantSetter.Set(InitialTenant.Id, InitialTenant.Key);

        // Global authorization reference data (plan Phase 4): permissions and role
        // templates are shared by every tenant.
        await this.SeedPermissionsAsync(cancellationToken);
        await this.SeedStoreAdministratorRoleAsync(cancellationToken);
        await this.SeedAdditionalRolesAsync(cancellationToken);

        await this.SeedInitialSubscriptionAsync(plan, cancellationToken);
        await this.SeedInitialSettingsAsync(cancellationToken);
        await this.SeedDefaultUserAsync(cancellationToken);
        await this.SeedDefaultUserRoleAsync(cancellationToken);
        await this.BackfillSecurityStampsAsync(cancellationToken);
        await this.SeedDefaultFinancialAccountsAsync(cancellationToken);

        // Host administrator identity (plan Phase 4 item 6), independent of any tenant.
        await this.SeedDefaultPlatformUserAsync(cancellationToken);

        await this.context.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubscriptionPlan> SeedSubscriptionPlanAsync(CancellationToken cancellationToken)
    {
        SubscriptionPlan? plan = await this.context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Key == "standard", cancellationToken);

        if (plan is not null)
        {
            return plan;
        }

        // Unlimited default plan; limits are introduced with real subscriptions later.
        plan = SubscriptionPlan.Create("Standard", "standard", null, null, null, null, isTrial: false, durationInMonths: 12, price: 199.00m, discountPercent: null).Value;
        this.context.SubscriptionPlans.Add(plan);

        // Seed a trial plan for 1 month (free) if not exists
        if (!await this.context.SubscriptionPlans.AnyAsync(p => p.Key == "trial", cancellationToken))
        {
            SubscriptionPlan trial = SubscriptionPlan.Create("Trial", "trial", 2, 50, 1, 5368709120, isTrial: true, durationInMonths: 1, price: 0m, discountPercent: null).Value;
            this.context.SubscriptionPlans.Add(trial);
        }

        return plan;
    }

    private async Task SeedInitialTenantAsync(CancellationToken cancellationToken)
    {
        if (await this.context.Tenants.AnyAsync(t => t.Id == InitialTenant.Id, cancellationToken))
        {
            return;
        }

        Result<Tenant> tenant = Tenant.Create(InitialTenant.Id, InitialTenant.Name, InitialTenant.Key, TenantStatus.Active);
        this.context.Tenants.Add(tenant.Value);
    }

    private async Task SeedInitialSubscriptionAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        if (await this.context.TenantSubscriptions.AnyAsync(s => s.TenantId == InitialTenant.Id, cancellationToken))
        {
            return;
        }

        DateTimeOffset utcNow = this.timeProvider.GetUtcNow();
        Result<TenantSubscription> subscription = TenantSubscription.Create(
            InitialTenant.Id, plan.Id, SubscriptionBillingCycle.Annual, utcNow, utcNow.AddYears(100));

        this.context.TenantSubscriptions.Add(subscription.Value);
    }

    private async Task SeedInitialSettingsAsync(CancellationToken cancellationToken)
    {
        if (await this.context.TenantSettings.AnyAsync(s => s.TenantId == InitialTenant.Id, cancellationToken))
        {
            return;
        }

        Result<TenantSettings> settings = TenantSettings.Create(
            InitialTenant.Id, InitialTenant.Name, logoUrl: null, timeZoneId: "Asia/Amman",
            enabledFeatures: [.. Features.All]);

        this.context.TenantSettings.Add(settings.Value);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        foreach ((string key, string name) in Permissions.All)
        {
            if (!await this.context.Permissions.AnyAsync(p => p.Key == key, cancellationToken))
            {
                Result<Permission> permission = Permission.Create(key, name);
                this.context.Permissions.Add(permission.Value);
            }
        }
    }

    private async Task SeedStoreAdministratorRoleAsync(CancellationToken cancellationToken)
    {
        Role? role = await this.context.Roles.FirstOrDefaultAsync(r => r.Key == StoreAdministratorRoleKey, cancellationToken);

        if (role is null)
        {
            Result<Role> created = Role.Create(StoreAdministratorRoleKey, "مدير المتجر");
            role = created.Value;
            this.context.Roles.Add(role);
        }

        List<Guid> permissionIds = await this.context.Permissions.Select(p => p.Id).ToListAsync(cancellationToken);
        List<Guid> grantedIds = await this.context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        foreach (Guid permissionId in permissionIds)
        {
            if (!grantedIds.Contains(permissionId))
            {
                // Fix: add via DbSet directly to ensure Added state (navigation Add was resulting in Detached->Modified and concurrency failure)
                var rp = new RolePermission(Guid.CreateVersion7(), role.Id, permissionId);
                this.context.RolePermissions.Add(rp);
            }
        }
    }

    private async Task SeedAdditionalRolesAsync(CancellationToken cancellationToken)
    {
        // Seeded granular roles for admin-managed permission page
        var roleDefinitions = new[]
        {
            new
            {
                Key = "manager", Name = "مدير عام", Permissions = new[]
            {
                Permissions.UsersView, Permissions.EmployeesView, Permissions.EmployeesManage,
                Permissions.SuppliersView, Permissions.SuppliersManage,
                Permissions.InventoryView, Permissions.InventoryManage,
                Permissions.FinanceView, Permissions.FinanceManage,
                Permissions.SalesView, Permissions.SalesManage,
                Permissions.PurchasesView, Permissions.PurchasesManage,
                Permissions.ExpensesView, Permissions.ExpensesManage,
                Permissions.ReportsView, Permissions.SettingsView
            },
            },
            new
            {
                Key = "cashier", Name = "أمين صندوق", Permissions = new[]
            {
                Permissions.SalesView, Permissions.SalesManage,
                Permissions.PurchasesView, Permissions.FinanceView, Permissions.InventoryView,
                Permissions.ExpensesView
            },
            },
            new
            {
                Key = "viewer", Name = "مشاهد", Permissions = new[]
            {
                Permissions.UsersView, Permissions.EmployeesView, Permissions.SuppliersView,
                Permissions.InventoryView, Permissions.FinanceView, Permissions.SalesView,
                Permissions.PurchasesView, Permissions.ExpensesView, Permissions.ReportsView, Permissions.SettingsView
            },
            },
            new
            {
                Key = "inventory_clerk", Name = "مسؤول مخزون", Permissions = new[]
            {
                Permissions.InventoryView, Permissions.InventoryManage,
                Permissions.SuppliersView, Permissions.SuppliersManage,
                Permissions.PurchasesView
            },
            },
        };

        // Need permission lookup by key
        Dictionary<string, Guid> permissionMap = await this.context.Permissions.ToDictionaryAsync(p => p.Key, p => p.Id, cancellationToken);

        foreach (var def in roleDefinitions)
        {
            Role? role = await this.context.Roles.FirstOrDefaultAsync(r => r.Key == def.Key, cancellationToken);
            if (role is null)
            {
                Result<Role> created = Role.Create(def.Key, def.Name);
                role = created.Value;
                this.context.Roles.Add(role);
                await this.context.SaveChangesAsync(cancellationToken); // need Id for FK
            }

            List<Guid> grantedIds = await this.context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken);

            foreach (string permKey in def.Permissions)
            {
                if (!permissionMap.TryGetValue(permKey, out Guid permId))
                {
                    continue;
                }

                if (!grantedIds.Contains(permId))
                {
                    this.context.RolePermissions.Add(new RolePermission(Guid.CreateVersion7(), role.Id, permId));
                }
            }
        }
    }

    private async Task SeedDefaultUserRoleAsync(CancellationToken cancellationToken)
    {
        User? admin = await this.context.Users.FirstOrDefaultAsync(u => u.Email == "admin@goldstore", cancellationToken);
        Role? role = await this.context.Roles.FirstOrDefaultAsync(r => r.Key == StoreAdministratorRoleKey, cancellationToken);

        if (admin is null || role is null)
        {
            return;
        }

        if (!await this.context.UserRoles.AnyAsync(ur => ur.UserId == admin.Id && ur.RoleId == role.Id, cancellationToken))
        {
            Result<UserRole> userRole = UserRole.Create(admin.TenantId, admin.Id, role.Id);
            this.context.UserRoles.Add(userRole.Value);
        }
    }

    private async Task BackfillSecurityStampsAsync(CancellationToken cancellationToken)
    {
        // Rows migrated before the SecurityStamp column share the migration default.
        // Assign each a unique stamp so sessions can be revoked individually.
        List<User> users = await this.context.Users
            .Where(u => string.IsNullOrEmpty(u.SecurityStamp))
            .ToListAsync(cancellationToken);

        foreach (User user in users)
        {
            user.RegenerateSecurityStamp();
        }
    }

    private async Task SeedDefaultPlatformUserAsync(CancellationToken cancellationToken)
    {
        if (await this.context.PlatformUsers.AnyAsync(cancellationToken))
        {
            // Ensure legacy seed has phone (migration adds nullable column)
            PlatformUser? existing = await this.context.PlatformUsers.FirstOrDefaultAsync(cancellationToken);
            if (existing is not null && string.IsNullOrWhiteSpace(existing.PhoneNumber))
            {
                existing.SetPhoneNumberVerified("+970592990484");
                await this.context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        string defaultPassword = this.configuration["DefaultPlatformUserPassword"] ?? this.configuration["DefaultUserPassword"]!;
        Result<PlatformUser> platformUser = PlatformUser.Create(
            "platform@goldstore.app", "Platform", "Admin", this.passwordHasher.Hash(defaultPassword));

        // Seed Palestine phone for mandatory 2FA demo; enrollment via dev-token:+970592990484 in dev
        platformUser.Value.SetPhoneNumberVerified("+970592990484");
        this.context.PlatformUsers.Add(platformUser.Value);
    }

    private async Task SeedDefaultUserAsync(CancellationToken cancellationToken)
    {
        if (await this.context.Users.AnyAsync(cancellationToken))
        {
            User? existing = await this.context.Users.FirstOrDefaultAsync(u => u.Email == "admin@goldstore", cancellationToken);
            if (existing is not null && string.IsNullOrWhiteSpace(existing.PhoneNumber))
            {
                existing.SetPhoneNumberVerified("+970592990484");
                await this.context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        string defaultPassword = this.configuration["DefaultUserPassword"]!;
        string hashedPassword = this.passwordHasher.Hash(defaultPassword);
        Result<User> defaultUser = User.Create(Guid.CreateVersion7(), InitialTenant.Id, "admin@goldstore", "Admin", "Admin", hashedPassword);
        defaultUser.Value.SetPhoneNumberVerified("+970592990484");
        this.context.Users.Add(defaultUser.Value);
    }

    private async Task SeedDefaultFinancialAccountsAsync(CancellationToken cancellationToken)
    {
        if (await this.context.FinancialAccounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedAccounts = new List<FinancialAccount>();

        Result<FinancialAccount> jodAccount = FinancialAccount.Create("صندوق الدينار الرئيسي", Currency.JOD, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الدينار الكاش الرئيسي");
        Result<FinancialAccount> usdAccount = FinancialAccount.Create("صندوق الدولار الرئيسي", Currency.USD, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الدولار الكاش الرئيسي");
        Result<FinancialAccount> ilsAccount = FinancialAccount.Create("صندوق الدينار الرئيسي", Currency.ILS, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الشيكل الكاش الرئيسي");

        seedAccounts.AddRange(jodAccount.Value, usdAccount.Value, ilsAccount.Value);

        // TenantId is stamped by the tenant write guard from the ambient tenant.
        this.context.FinancialAccounts.AddRange(seedAccounts);
    }
}

public static class InitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = app.Services.CreateScope();

        ApplicationDbContextInitializer initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();

        await initializer.InitializeAsync(cancellationToken);

        await initializer.SeedAsync(cancellationToken);
    }
}
