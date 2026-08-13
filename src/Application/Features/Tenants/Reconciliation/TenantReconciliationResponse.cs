namespace Application.Tenants.Reconciliation;

/// <summary>
/// Result of a tenant reconciliation run. <see cref="Anomalies"/> reports rows that
/// violate tenant ownership (empty or unknown <c>TenantId</c>); <see cref="Tenants"/>
/// holds per-tenant row counts and balances recomputed from ledger entries.
/// </summary>
public sealed class TenantReconciliationResponse
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    public List<ReconciliationAnomaly> Anomalies { get; set; } = [];

    public List<TenantReconciliationBlock> Tenants { get; set; } = [];
}

/// <summary>
/// A tenant-ownership violation discovered while scanning a tenant-owned table.
/// </summary>
public sealed record ReconciliationAnomaly(
    string Table,
    Guid? TenantId,
    long Count,
    string Message);

/// <summary>
/// Row counts and recomputed balances for a single tenant.
/// </summary>
public sealed class TenantReconciliationBlock
{
    public Guid TenantId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<EntityRowCount> RowCounts { get; set; } = [];

    public List<GoldStockBalance> GoldStock { get; set; } = [];

    public decimal TotalEquivalent21K { get; set; }

    public List<FinancialBalance> FinancialBalances { get; set; } = [];

    public List<CurrencyTotals> FinancialTotals { get; set; } = [];

    public List<DebtTotals> DebtTotals { get; set; } = [];

    public List<SupplierGoldBalance> SupplierGoldBalances { get; set; } = [];

    public List<SupplierManufacturingBalance> SupplierManufacturingBalances { get; set; } = [];
}

public sealed record EntityRowCount(string Entity, long Count);

public sealed record GoldStockBalance(int Karat, decimal WeightGrams, decimal Equivalent21K);

public sealed record FinancialBalance(Guid AccountId, string AccountName, string Currency, decimal Balance);

public sealed record CurrencyTotals(string Currency, decimal TotalInflow, decimal TotalOutflow, decimal Net);

public sealed record DebtTotals(string Currency, decimal TotalReceivable, decimal TotalPayable, decimal Net);

public sealed record SupplierGoldBalance(Guid SupplierId, string SupplierName, int Karat, decimal NetWeight);

public sealed record SupplierManufacturingBalance(Guid SupplierId, string SupplierName, string Currency, decimal NetAmount);
