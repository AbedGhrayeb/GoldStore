using Domain.Catalog;
using Domain.CustomerPurchases;
using Domain.Debts;
using Domain.Employees;
using Domain.Expenses;
using Domain.Finance;
using Domain.Inventory;
using Domain.Sales;
using Domain.SupplierOperations;
using Domain.Suppliers;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Employee> Employees { get; }
    DbSet<SalaryPayment> SalaryPayments { get; }
    DbSet<FinancialTransaction> FinancialTransactions { get; }
    DbSet<FinancialAccount> FinancialAccounts { get; }
    DbSet<Category> Categories { get; }
    DbSet<GoldLedgerEntry> GoldLedgerEntries { get; }
    DbSet<InventoryAdjustment> InventoryAdjustments { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<SupplierGoldLedgerEntry> SupplierGoldLedgerEntries { get; }
    DbSet<SupplierManufacturingLedgerEntry> SupplierManufacturingLedgerEntries { get; }
    DbSet<SupplierDelivery> SupplierDeliveries { get; }
    DbSet<SupplierScrapGoldPayment> SupplierScrapGoldPayments { get; }
    DbSet<SupplierManufacturingPayment> SupplierManufacturingPayments { get; }
    DbSet<SupplierFinancialTransaction> SupplierFinancialTransactions { get; }
    DbSet<SupplierFinancialLedgerEntry> SupplierFinancialLedgerEntries { get; }
    DbSet<SupplierFinancialPayment> SupplierFinancialPayments { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }

    DbSet<Debt> Debts { get; }

    DbSet<DebtLedgerEntry> DebtLedgerEntries { get; }

    DbSet<SalesInvoice> SalesInvoices { get; }

    DbSet<SalesInvoiceItem> SalesInvoiceItems { get; }

    DbSet<CustomerPurchaseInvoice> CustomerPurchaseInvoices { get; }

    DbSet<CustomerPurchaseInvoiceItem> CustomerPurchaseInvoiceItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
