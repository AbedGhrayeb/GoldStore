using Domain.Catalog;
using Domain.Finance;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Suppliers;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoItem> TodoItems { get; }
    DbSet<FinancialTransaction> FinancialTransactions { get; }
    DbSet<FinancialAccount> FinancialAccounts { get; }
    DbSet<Category> Categories { get; }
    DbSet<GoldLedgerEntry> GoldLedgerEntries { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<SupplierGoldLedgerEntry> SupplierGoldLedgerEntries { get; }
    DbSet<SupplierManufacturingLedgerEntry> SupplierManufacturingLedgerEntries { get; }
    DbSet<SupplierDelivery> SupplierDeliveries { get; }
    DbSet<SupplierScrapGoldPayment> SupplierScrapGoldPayments { get; }
    DbSet<SupplierManufacturingPayment> SupplierManufacturingPayments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
