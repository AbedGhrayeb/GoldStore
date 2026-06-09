using Application.Abstractions.Data;
using Domain.Catalog;
using Domain.Finance;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Suppliers;
using Domain.Todos;
using Domain.Users;
using Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<TodoItem> TodoItems { get; set; }

    public DbSet<FinancialTransaction> FinancialTransactions { get; set; }

    public DbSet<FinancialAccount> FinancialAccounts { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<GoldLedgerEntry> GoldLedgerEntries { get; set; }

    public DbSet<Supplier> Suppliers { get; set; }

    public DbSet<SupplierGoldLedgerEntry> SupplierGoldLedgerEntries { get; set; }

    public DbSet<SupplierManufacturingLedgerEntry> SupplierManufacturingLedgerEntries { get; set; }

    public DbSet<SupplierDelivery> SupplierDeliveries { get; set; }

    public DbSet<SupplierScrapGoldPayment> SupplierScrapGoldPayments { get; set; }

    public DbSet<SupplierManufacturingPayment> SupplierManufacturingPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

    }

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
