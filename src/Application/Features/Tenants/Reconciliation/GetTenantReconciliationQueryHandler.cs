using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Debts;
using Domain.Finance;
using Domain.Inventory;
using Domain.Suppliers;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Tenants.Reconciliation;

/// <summary>
/// Host-only reconciliation handler (plan Phase 6). Host administration is the one
/// sanctioned place for <c>IgnoreQueryFilters()</c> (plan Phase 3 item 8): the host
/// has no ambient tenant, so tenant-owned rows are invisible to the global filter.
/// Every read below selects a tenant explicitly and never crosses tenant boundaries.
/// </summary>
internal sealed class GetTenantReconciliationQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetTenantReconciliationQuery, TenantReconciliationResponse>
{
    public async Task<Result<TenantReconciliationResponse>> Handle(
        GetTenantReconciliationQuery query,
        CancellationToken cancellationToken)
    {
        List<Tenant> tenants = await context.Tenants
            .AsNoTracking()
            .Where(tenant => query.TenantId == null || tenant.Id == query.TenantId)
            .OrderBy(tenant => tenant.Name)
            .ToListAsync(cancellationToken);

        List<Guid> allTenantIds = await context.Tenants
            .AsNoTracking()
            .Select(tenant => tenant.Id)
            .ToListAsync(cancellationToken);

        var tableCounts = new Dictionary<string, Dictionary<Guid, int>>
        {
            ["Users"] = await CountByTenantAsync(context.Users, cancellationToken),
            ["Employees"] = await CountByTenantAsync(context.Employees, cancellationToken),
            ["SalaryPayments"] = await CountByTenantAsync(context.SalaryPayments, cancellationToken),
            ["FinancialAccounts"] = await CountByTenantAsync(context.FinancialAccounts, cancellationToken),
            ["FinancialTransactions"] = await CountByTenantAsync(context.FinancialTransactions, cancellationToken),
            ["Categories"] = await CountByTenantAsync(context.Categories, cancellationToken),
            ["GoldLedgerEntries"] = await CountByTenantAsync(context.GoldLedgerEntries, cancellationToken),
            ["InventoryAdjustments"] = await CountByTenantAsync(context.InventoryAdjustments, cancellationToken),
            ["Suppliers"] = await CountByTenantAsync(context.Suppliers, cancellationToken),
            ["SupplierGoldLedgerEntries"] = await CountByTenantAsync(context.SupplierGoldLedgerEntries, cancellationToken),
            ["SupplierManufacturingLedgerEntries"] = await CountByTenantAsync(context.SupplierManufacturingLedgerEntries, cancellationToken),
            ["SupplierDeliveries"] = await CountByTenantAsync(context.SupplierDeliveries, cancellationToken),
            ["SupplierScrapGoldPayments"] = await CountByTenantAsync(context.SupplierScrapGoldPayments, cancellationToken),
            ["SupplierManufacturingPayments"] = await CountByTenantAsync(context.SupplierManufacturingPayments, cancellationToken),
            ["SupplierFinancialTransactions"] = await CountByTenantAsync(context.SupplierFinancialTransactions, cancellationToken),
            ["SupplierFinancialLedgerEntries"] = await CountByTenantAsync(context.SupplierFinancialLedgerEntries, cancellationToken),
            ["SupplierFinancialPayments"] = await CountByTenantAsync(context.SupplierFinancialPayments, cancellationToken),
            ["Expenses"] = await CountByTenantAsync(context.Expenses, cancellationToken),
            ["ExpenseCategories"] = await CountByTenantAsync(context.ExpenseCategories, cancellationToken),
            ["Debts"] = await CountByTenantAsync(context.Debts, cancellationToken),
            ["DebtLedgerEntries"] = await CountByTenantAsync(context.DebtLedgerEntries, cancellationToken),
            ["SalesInvoices"] = await CountByTenantAsync(context.SalesInvoices, cancellationToken),
            ["SalesInvoiceItems"] = await CountByTenantAsync(context.SalesInvoiceItems, cancellationToken),
            ["CustomerPurchaseInvoices"] = await CountByTenantAsync(context.CustomerPurchaseInvoices, cancellationToken),
            ["CustomerPurchaseInvoiceItems"] = await CountByTenantAsync(context.CustomerPurchaseInvoiceItems, cancellationToken),
            ["RefreshTokens"] = await CountByTenantAsync(context.RefreshTokens, cancellationToken),
            ["UserRoles"] = await CountByTenantAsync(context.UserRoles, cancellationToken),
            ["TenantSettings"] = await CountByTenantAsync(context.TenantSettings, cancellationToken),
            ["TenantSubscriptions"] = await CountByTenantAsync(context.TenantSubscriptions, cancellationToken)
        };

        var validTenantIds = allTenantIds.ToHashSet();

        List<ReconciliationAnomaly> anomalies = BuildAnomalies(tableCounts, validTenantIds);

        List<TenantReconciliationBlock> blocks = [];
        foreach (Tenant tenant in tenants)
        {
            blocks.Add(await BuildBlockAsync(tenant, tableCounts, cancellationToken));
        }

        return new TenantReconciliationResponse
        {
            GeneratedAtUtc = new DateTimeOffset(dateTimeProvider.UtcNow),
            Anomalies = anomalies,
            Tenants = blocks
        };
    }

    private static async Task<Dictionary<Guid, int>> CountByTenantAsync<TEntity>(
        IQueryable<TEntity> source,
        CancellationToken cancellationToken)
        where TEntity : class, ITenantEntity
        => await source
            .IgnoreQueryFilters()
            .GroupBy(entity => entity.TenantId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

    private static List<ReconciliationAnomaly> BuildAnomalies(
        Dictionary<string, Dictionary<Guid, int>> tableCounts,
        HashSet<Guid> validTenantIds)
    {
        List<ReconciliationAnomaly> anomalies = [];

        foreach ((string table, Dictionary<Guid, int> counts) in tableCounts.OrderBy(pair => pair.Key))
        {
            foreach ((Guid tenantId, int count) in counts)
            {
                if (tenantId == Guid.Empty)
                {
                    anomalies.Add(new ReconciliationAnomaly(
                        table, tenantId, count, "contains rows with an empty tenant id"));
                }
                else if (!validTenantIds.Contains(tenantId))
                {
                    anomalies.Add(new ReconciliationAnomaly(
                        table, tenantId, count, "references a tenant that does not exist"));
                }
            }
        }

        return anomalies;
    }

    private async Task<TenantReconciliationBlock> BuildBlockAsync(
        Tenant tenant,
        Dictionary<string, Dictionary<Guid, int>> tableCounts,
        CancellationToken cancellationToken)
    {
        Guid tenantId = tenant.Id;

        List<EntityRowCount> rowCounts = tableCounts
            .OrderBy(pair => pair.Key)
            .Select(pair => new EntityRowCount(pair.Key, pair.Value.GetValueOrDefault(tenantId)))
            .ToList();

        List<GoldStockBalance> goldStock = await context.GoldLedgerEntries
            .IgnoreQueryFilters()
            .Where(entry => entry.TenantId == tenantId)
            .GroupBy(entry => entry.Karat)
            .Select(group => new GoldStockBalance(
                (int)group.Key,
                group.Sum(entry => entry.MovementType == GoldMovementType.Increase ? entry.WeightInGrams : -entry.WeightInGrams),
                group.Sum(entry => entry.MovementType == GoldMovementType.Increase ? entry.Equivalent21KWeightInGrams : -entry.Equivalent21KWeightInGrams)))
            .ToListAsync(cancellationToken);

        decimal totalEquivalent21K = await context.GoldLedgerEntries
            .IgnoreQueryFilters()
            .Where(entry => entry.TenantId == tenantId)
            .SumAsync(entry => entry.MovementType == GoldMovementType.Increase ? entry.Equivalent21KWeightInGrams : -entry.Equivalent21KWeightInGrams, cancellationToken);

        List<CurrencyTotals> financialTotals = await context.FinancialTransactions
            .IgnoreQueryFilters()
            .Where(transaction => transaction.TenantId == tenantId)
            .GroupBy(transaction => transaction.Currency)
            .Select(group => new CurrencyTotals(
                group.Key.ToString(),
                group.Sum(transaction => transaction.TransactionType == FinancialTransactionType.Inflow ? transaction.Amount : 0m),
                group.Sum(transaction => transaction.TransactionType == FinancialTransactionType.Outflow ? transaction.Amount : 0m),
                group.Sum(transaction => transaction.TransactionType == FinancialTransactionType.Inflow ? transaction.Amount : -transaction.Amount)))
            .OrderByDescending(totals => totals.Net)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, decimal> accountBalances = await context.FinancialTransactions
            .IgnoreQueryFilters()
            .Where(transaction => transaction.TenantId == tenantId)
            .GroupBy(transaction => transaction.AccountId)
            .Select(group => new
            {
                AccountId = group.Key,
                Balance = group.Sum(transaction => transaction.TransactionType == FinancialTransactionType.Inflow ? transaction.Amount : -transaction.Amount)
            })
            .ToDictionaryAsync(balance => balance.AccountId, balance => balance.Balance, cancellationToken);

        List<FinancialAccount> accounts = await context.FinancialAccounts
            .IgnoreQueryFilters()
            .Where(account => account.TenantId == tenantId)
            .OrderBy(account => account.Currency)
            .ThenBy(account => account.Name)
            .ToListAsync(cancellationToken);

        List<FinancialBalance> financialBalances = accounts
            .Select(account => new FinancialBalance(
                account.Id,
                account.Name,
                account.Currency.ToString(),
                accountBalances.GetValueOrDefault(account.Id)))
            .ToList();

        List<DebtTotals> debtTotals = await ComputeDebtTotalsAsync(tenantId, cancellationToken);

        Dictionary<Guid, string> supplierNames = await context.Suppliers
            .IgnoreQueryFilters()
            .Where(supplier => supplier.TenantId == tenantId)
            .Select(supplier => new { supplier.Id, supplier.Name })
            .ToDictionaryAsync(supplier => supplier.Id, supplier => supplier.Name, cancellationToken);

        List<SupplierGoldBalance> supplierGoldBalances = (await context.SupplierGoldLedgerEntries
            .IgnoreQueryFilters()
            .Where(entry => entry.TenantId == tenantId)
            .GroupBy(entry => new { entry.SupplierId, entry.Karat })
            .Select(group => new
            {
                group.Key.SupplierId,
                group.Key.Karat,
                Net = group.Sum(entry => entry.MovementType == SupplierBalanceMovementType.Increase ? entry.WeightInGrams : -entry.WeightInGrams)
            })
            .ToListAsync(cancellationToken))
            .Select(balance => new SupplierGoldBalance(
                balance.SupplierId,
                supplierNames.GetValueOrDefault(balance.SupplierId, string.Empty),
                (int)balance.Karat,
                balance.Net))
            .OrderByDescending(balance => balance.NetWeight)
            .ToList();

        List<SupplierManufacturingBalance> supplierManufacturingBalances = (await context.SupplierManufacturingLedgerEntries
            .IgnoreQueryFilters()
            .Where(entry => entry.TenantId == tenantId)
            .GroupBy(entry => new { entry.SupplierId, entry.Currency })
            .Select(group => new
            {
                group.Key.SupplierId,
                group.Key.Currency,
                Net = group.Sum(entry => entry.MovementType == SupplierBalanceMovementType.Increase ? entry.Amount : -entry.Amount)
            })
            .ToListAsync(cancellationToken))
            .Select(balance => new SupplierManufacturingBalance(
                balance.SupplierId,
                supplierNames.GetValueOrDefault(balance.SupplierId, string.Empty),
                balance.Currency.ToString(),
                balance.Net))
            .OrderByDescending(balance => balance.NetAmount)
            .ToList();

        return new TenantReconciliationBlock
        {
            TenantId = tenant.Id,
            Key = tenant.Key,
            Name = tenant.Name,
            Status = tenant.Status.ToString(),
            RowCounts = rowCounts,
            GoldStock = goldStock,
            TotalEquivalent21K = totalEquivalent21K,
            FinancialBalances = financialBalances,
            FinancialTotals = financialTotals,
            DebtTotals = debtTotals,
            SupplierGoldBalances = supplierGoldBalances,
            SupplierManufacturingBalances = supplierManufacturingBalances
        };
    }

    private async Task<List<DebtTotals>> ComputeDebtTotalsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        List<Guid> debtIds = await context.Debts
            .IgnoreQueryFilters()
            .Where(debt => debt.TenantId == tenantId)
            .Select(debt => debt.Id)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, (DebtDirection Direction, Currency Currency)> debtMeta = debtIds.Count == 0
            ? []
            : await context.Debts
                .IgnoreQueryFilters()
                .Where(debt => debt.TenantId == tenantId)
                .Select(debt => new { debt.Id, debt.Direction, debt.Currency })
                .ToDictionaryAsync(debt => debt.Id, debt => (debt.Direction, debt.Currency), cancellationToken);

        Dictionary<Guid, decimal> debtBalances = debtIds.Count == 0
            ? []
            : await context.DebtLedgerEntries
                .IgnoreQueryFilters()
                .Where(entry => entry.TenantId == tenantId && debtIds.Contains(entry.DebtId))
                .GroupBy(entry => entry.DebtId)
                .Select(group => new
                {
                    DebtId = group.Key,
                    Balance = group.Sum(entry => entry.MovementType == DebtBalanceMovementType.Increase ? entry.Amount : -entry.Amount)
                })
                .ToDictionaryAsync(balance => balance.DebtId, balance => balance.Balance, cancellationToken);

        var totals = new Dictionary<Currency, (decimal Receivable, decimal Payable)>();

        foreach ((Guid debtId, (DebtDirection Direction, Currency Currency) meta) in debtMeta)
        {
            decimal balance = debtBalances.GetValueOrDefault(debtId, 0m);
            if (balance <= 0)
            {
                continue;
            }

            (decimal Receivable, decimal Payable) current = totals.GetValueOrDefault(meta.Currency);

            if (meta.Direction == DebtDirection.Receivable)
            {
                totals[meta.Currency] = (current.Receivable + balance, current.Payable);
            }
            else
            {
                totals[meta.Currency] = (current.Receivable, current.Payable + balance);
            }
        }

        return totals
            .OrderByDescending(pair => pair.Value.Receivable + pair.Value.Payable)
            .Select(pair => new DebtTotals(
                pair.Key.ToString(),
                pair.Value.Receivable,
                pair.Value.Payable,
                pair.Value.Receivable - pair.Value.Payable))
            .ToList();
    }
}
