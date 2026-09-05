// <copyright file="DbContextTenantIsolationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Tenants;
using Domain.Common;
using Domain.Inventory;
using Domain.Suppliers;
using Domain.Tenants;
using Infrastructure.Database;
using Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using SharedKernel.Result;
using Xunit;

namespace Application.IntegrationTests.TenantIsolation;

/// <summary>
/// Phase 8 items 1-5: EF Core global query filters, the central tenant write guard,
/// and tenant-aware unique constraints, exercised against a real SQL Server database.
/// </summary>
public sealed class DbContextTenantIsolationTests : IClassFixture<MultiTenantWebApplicationFactory>
{
    private readonly MultiTenantWebApplicationFactory factory;

    public DbContextTenantIsolationTests(MultiTenantWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    // ─── Global query filters ─────────────────────────────────────────────────
    [Fact]
    public async Task QueryFilters_TenantASeesOnlyItsOwnRows()
    {
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(
            Domain.Tenants.InitialTenant.Id, MultiTenantWebApplicationFactory.TenantAKey);
        using (scope)
        {
            List<Supplier> suppliers = await db.Suppliers.ToListAsync();
            List<Domain.Catalog.Category> categories = await db.Categories.ToListAsync();

            Assert.NotEmpty(suppliers);
            Assert.All(suppliers, supplier => Assert.Equal(Domain.Tenants.InitialTenant.Id, supplier.TenantId));
            Assert.Contains(suppliers, supplier => supplier.Id == this.factory.SupplierA.Id);
            Assert.DoesNotContain(suppliers, supplier => supplier.Id == this.factory.SupplierB.Id);

            Assert.Contains(categories, category => category.Id == this.factory.CategoryA.Id);
            Assert.DoesNotContain(categories, category => category.Id == this.factory.CategoryB.Id);
        }
    }

    [Fact]
    public async Task QueryFilters_TenantBCannotReadTenantARows()
    {
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(
            this.factory.TenantBId, MultiTenantWebApplicationFactory.TenantBKey);
        using (scope)
        {
            Supplier? foreignSupplier = await db.Suppliers
                .SingleOrDefaultAsync(supplier => supplier.Id == this.factory.SupplierA.Id);
            Domain.Catalog.Category? foreignCategory = await db.Categories
                .SingleOrDefaultAsync(category => category.Id == this.factory.CategoryA.Id);

            Assert.Null(foreignSupplier);
            Assert.Null(foreignCategory);

            List<Supplier> ownSuppliers = await db.Suppliers.ToListAsync();
            Assert.Contains(ownSuppliers, supplier => supplier.Id == this.factory.SupplierB.Id);
        }
    }

    [Fact]
    public async Task QueryFilters_NoTenantMatchesNothing()
    {
        (IServiceScope scope, ApplicationDbContext db) = this.factory.OpenNoTenantContext();
        using (scope)
        {
            // Deny by default: with no ambient tenant the filter matches no rows.
            Assert.Empty(await db.Suppliers.ToListAsync());
            Assert.Empty(await db.Categories.ToListAsync());
        }
    }

    // ─── Tenant write guard ───────────────────────────────────────────────────
    [Fact]
    public async Task WriteGuard_StampsAddedRowsWithCurrentTenant()
    {
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(
            Domain.Tenants.InitialTenant.Id, MultiTenantWebApplicationFactory.TenantAKey);
        using (scope)
        {
            Supplier supplier = Supplier.Create("Stamped Supplier", "0791234567", null, null, null).Value;

            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();

            Assert.Equal(Domain.Tenants.InitialTenant.Id, supplier.TenantId);
        }
    }

    [Fact]
    public async Task WriteGuard_RejectsCreateWithoutTenant()
    {
        (IServiceScope scope, ApplicationDbContext db) = this.factory.OpenNoTenantContext();
        using (scope)
        {
            db.Suppliers.Add(Supplier.Create("No Tenant", "0791234567", null, null, null).Value);

            await Assert.ThrowsAsync<TenantAccessViolationException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task WriteGuard_RejectsClientSuppliedTenantId()
    {
        // TenantSettings carries its own TenantId (tenant B). Saving it while the
        // ambient tenant is A must be rejected — client-supplied tenant ids never win.
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(
            Domain.Tenants.InitialTenant.Id, MultiTenantWebApplicationFactory.TenantAKey);
        using (scope)
        {
            Result<TenantSettings> settings = TenantSettings.Create(
                this.factory.TenantBId, "Foreign", null, "Asia/Amman");

            db.TenantSettings.Add(settings.Value);

            await Assert.ThrowsAsync<TenantAccessViolationException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task WriteGuard_TenantIdIsImmutable()
    {
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(
            Domain.Tenants.InitialTenant.Id, MultiTenantWebApplicationFactory.TenantAKey);
        using (scope)
        {
            Supplier supplier = await db.Suppliers.SingleAsync(s => s.Id == this.factory.SupplierA.Id);

            // Simulate a tenant reassignment (a client cannot set the property directly).
            db.Entry(supplier).Property(nameof(ITenantEntity.TenantId)).CurrentValue = this.factory.TenantBId;

            await Assert.ThrowsAsync<TenantAccessViolationException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task WriteGuard_RejectsModifyingForeignEntity()
    {
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(
            Domain.Tenants.InitialTenant.Id, MultiTenantWebApplicationFactory.TenantAKey);
        using (scope)
        {
            Supplier foreign = await db.Suppliers
                .IgnoreQueryFilters()
                .SingleAsync(s => s.Id == this.factory.SupplierB.Id);

            foreign.Update(foreign.Id, "Hacked Name", foreign.PrimaryPhone, null, null, null);

            await Assert.ThrowsAsync<TenantAccessViolationException>(() => db.SaveChangesAsync());
        }
    }

    // ─── Tenant-aware unique constraints ──────────────────────────────────────
    [Fact]
    public async Task UniqueConstraint_SameNameAllowedInDifferentTenants()
    {
        // Both tenants deliberately have a supplier named "Gold House" (seeded).
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(
            Domain.Tenants.InitialTenant.Id, MultiTenantWebApplicationFactory.TenantAKey);
        using (scope)
        {
            Assert.NotNull(await db.Suppliers.SingleAsync(s => s.Name == "Gold House"));
        }

        (IServiceScope scopeB, ApplicationDbContext dbB) = await this.factory.OpenTenantContextAsync(
            this.factory.TenantBId, MultiTenantWebApplicationFactory.TenantBKey);
        using (scopeB)
        {
            Assert.NotNull(await dbB.Suppliers.SingleAsync(s => s.Name == "Gold House"));
        }
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateWithinTenantIsRejected()
    {
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(
            Domain.Tenants.InitialTenant.Id, MultiTenantWebApplicationFactory.TenantAKey);
        using (scope)
        {
            db.Suppliers.Add(Supplier.Create("Gold House", "0799999999", null, null, null).Value);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    // ─── Ledger scoping (plan Phase 5 item 5) ─────────────────────────────────
    [Fact]
    public async Task LedgerEntries_AreScopedToTheirTenant()
    {
        (IServiceScope scopeA, ApplicationDbContext dbA) = await this.factory.OpenTenantContextAsync(
            Domain.Tenants.InitialTenant.Id, MultiTenantWebApplicationFactory.TenantAKey);
        Guid entryId;
        decimal tenantA21k;
        using (scopeA)
        {
            GoldLedgerEntry entry = GoldLedgerEntry.Create(
                Karat.K21, 10m, GoldMovementType.Increase, GoldReferenceType.InventoryAdjustment, null, "isolation test")
                .Value;
            dbA.GoldLedgerEntries.Add(entry);
            await dbA.SaveChangesAsync();

            Assert.Equal(Domain.Tenants.InitialTenant.Id, entry.TenantId);
            entryId = entry.Id;
            tenantA21k = await dbA.GoldLedgerEntries.SumAsync(e => e.Equivalent21KWeightInGrams);
        }

        (IServiceScope scopeB, ApplicationDbContext dbB) = await this.factory.OpenTenantContextAsync(
            this.factory.TenantBId, MultiTenantWebApplicationFactory.TenantBKey);
        using (scopeB)
        {
            Assert.Null(await dbB.GoldLedgerEntries.SingleOrDefaultAsync(e => e.Id == entryId));

            decimal visible = await dbB.GoldLedgerEntries.SumAsync(e => e.Equivalent21KWeightInGrams);

            Assert.NotEqual(tenantA21k, visible);
        }
    }
}
