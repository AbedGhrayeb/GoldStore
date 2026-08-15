using System.Reflection;
using Application.Abstractions.Data;
using Application.Abstractions.Tenants;
using Domain.Authorization;
using Domain.Catalog;
using Domain.Common;
using Domain.CustomerPurchases;
using Domain.Debts;
using Domain.Expenses;
using Domain.Finance;
using Domain.Inventory;
using Domain.Sales;
using Domain.SupplierOperations;
using Domain.Suppliers;
using Domain.Employees;
using Domain.Tenants;
using Domain.Users;
using Infrastructure.DomainEvents;
using Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SharedKernel;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher,
    ICurrentTenant currentTenant)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Employee> Employees { get; set; }

    public DbSet<SalaryPayment> SalaryPayments { get; set; }

    public DbSet<FinancialTransaction> FinancialTransactions { get; set; }

    public DbSet<FinancialAccount> FinancialAccounts { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<InvoiceNumberSequence> InvoiceNumberSequences { get; set; }

    public DbSet<GoldLedgerEntry> GoldLedgerEntries { get; set; }

    public DbSet<InventoryAdjustment> InventoryAdjustments { get; set; }

    public DbSet<Supplier> Suppliers { get; set; }

    public DbSet<SupplierGoldLedgerEntry> SupplierGoldLedgerEntries { get; set; }

    public DbSet<SupplierManufacturingLedgerEntry> SupplierManufacturingLedgerEntries { get; set; }

    public DbSet<SupplierDelivery> SupplierDeliveries { get; set; }

    public DbSet<SupplierScrapGoldPayment> SupplierScrapGoldPayments { get; set; }

    public DbSet<SupplierManufacturingPayment> SupplierManufacturingPayments { get; set; }

    public DbSet<SupplierFinancialTransaction> SupplierFinancialTransactions { get; set; }

    public DbSet<SupplierFinancialLedgerEntry> SupplierFinancialLedgerEntries { get; set; }

    public DbSet<SupplierFinancialPayment> SupplierFinancialPayments { get; set; }

    public DbSet<Expense> Expenses { get; set; }

    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }

    public DbSet<Debt> Debts { get; set; }

    public DbSet<DebtLedgerEntry> DebtLedgerEntries { get; set; }

    public DbSet<SalesInvoice> SalesInvoices { get; set; }

    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; }

    public DbSet<CustomerPurchaseInvoice> CustomerPurchaseInvoices { get; set; }

    public DbSet<CustomerPurchaseInvoiceItem> CustomerPurchaseInvoiceItems { get; set; }

    public DbSet<Permission> Permissions { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<RolePermission> RolePermissions { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Tenant> Tenants { get; set; }

    public DbSet<TenantSettings> TenantSettings { get; set; }

    public DbSet<TenantSubscription> TenantSubscriptions { get; set; }

    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public DbSet<PlatformUser> PlatformUsers { get; set; }

    /// <summary>
    /// The tenant applied by the global query filters. Read from the scoped
    /// <see cref="ICurrentTenant"/> at query time, never captured at model-build
    /// time. Deny by default: when no tenant is available the filters match nothing.
    /// </summary>
    private Guid CurrentTenantId => currentTenant.IsAvailable ? currentTenant.TenantId : Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.ApplyTenantOwnership();
        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            SetTenantQueryFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    private static readonly MethodInfo SetTenantQueryFilterMethod = typeof(ApplicationDbContext)
        .GetMethod(nameof(SetTenantQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("SetTenantQueryFilter method not found.");

    private void SetTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail

        List<IDomainEvent> domainEvents = ExtractDomainEvents();
        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(domainEvents);

        return result;
    }
    private async Task PublishDomainEventsAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }

    private List<IDomainEvent> ExtractDomainEvents()
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();
        return domainEvents;
    }
}
