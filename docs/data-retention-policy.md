# GoldStore Data Retention, Export, Archive, and Deletion Policy (Phase 9)

Approved-by: store owner (single-company deployment). This policy governs tenant-owned data in
the shared database. Global data (karats, permissions, subscription plans) is out of scope —
it is seeded and read-only.

## 1. Retention periods by data class

| Data class | Tables | Retention | Deletion allowed? |
| --- | --- | --- | --- |
| Financial ledgers | `FinancialTransactions`, `FinancialAccounts`, `Debts`, `DebtLedgerEntries`, `SupplierFinancialLedgerEntries` | Indefinite (financial records) | Never auto-deleted |
| Gold ledgers & inventory | `GoldLedgerEntries`, `InventoryAdjustments`, `SupplierGoldLedgerEntries`, `SupplierManufacturingLedgerEntries` | Indefinite (stock history) | Never auto-deleted |
| Business documents | `SalesInvoices`, `SalesInvoiceItems`, `CustomerPurchaseInvoices`, `CustomerPurchaseInvoiceItems`, `SupplierDeliveries`, `SupplierScrapGoldPayments`, `SupplierManufacturingPayments`, `SupplierFinancialTransactions`, `SupplierFinancialPayments` | Indefinite (auditable documents) | Void/cancel is auditable; no hard delete |
| Identity | `Users`, `UserRoles`, `RefreshTokens`, `TenantSettings`, `TenantSubscriptions` | While the tenant account exists | Only via the deletion procedure below |
| Suppliers, categories, employees | `Suppliers`, `Categories`, `Employees`, `SalaryPayments`, `Expenses`, `ExpenseCategories` | While the tenant account exists | Only via the deletion procedure below |

**Cancelled tenants:** data is retained after the read-only grace window (`Cancelled` status,
`CancellationReadOnlyGracePeriodDays` = 30 by default). Access is blocked; retention continues
indefinitely until an explicit deletion is approved.

## 2. Export

Self-serve export (M6 reporting) is **not yet implemented**. Until it ships, use the
DB-level fallback below, or the current KPI/ledger pages for ad-hoc views.

### DB-level export of a single tenant

```powershell
# Logical backup of just the tenant's rows, per tenant-owned table.
# Tenant-owned table list (all have a required TenantId):
#   Categories, CustomerPurchaseInvoiceItems, CustomerPurchaseInvoices,
#   DebtLedgerEntries, Debts, Employees, ExpenseCategories, Expenses,
#   FinancialAccounts, FinancialTransactions, GoldLedgerEntries,
#   InventoryAdjustments, RefreshTokens, SalaryPayments, SalesInvoiceItems,
#   SalesInvoices, SupplierDeliveries, SupplierFinancialLedgerEntries,
#   SupplierFinancialPayments, SupplierFinancialTransactions,
#   SupplierGoldLedgerEntries, SupplierManufacturingLedgerEntries,
#   SupplierManufacturingPayments, Suppliers, SupplierScrapGoldPayments,
#   TenantSettings, TenantSubscriptions, UserRoles, Users
sqlcmd -S . -d GoldStoreDb -Q "SELECT * FROM SalesInvoices WHERE TenantId = '$TENANT_ID'"
  -o "export-sales-invoices.csv" -s "," -W
```

Or a full logical backup of the database (includes every tenant; safe default for a
single-company deployment):

```powershell
sqlcmd -S . -d GoldStoreDb -Q "BACKUP DATABASE GoldStoreDb TO DISK = 'NUL'"  # verify T-SQL
# Use the SQL Server backup UI or Ola-style maintenance for a real backup instead.
```

Data-health exports (per-tenant row counts + balances) are available at any time via
`GET /host/api/v1/reconciliation` — use this for audit snapshots.

## 3. Archive

For long-term retention of a cancelled or closing tenant, produce a full logical backup of its
tables (Section 2), label it with the tenant key + date, and store it outside the production
server. The archived copy is authoritative if the production row is later deleted.

## 4. Deletion procedure

Hard deletion is **destructive** — there is no soft-delete today. Follow this sequence for a
tenant removal (approved by the store owner in writing):

1. Export + verify the archive (Section 2/3). Store the backup checksum off-server.
2. Take a full database backup and verify it restores into a scratch database.
3. Delete in **FK order**: child/granular rows before their parents —
   ledger entries and invoice items before invoices; invoice/ledger parents before
   suppliers/accounts; users/roles last; then `TenantSubscription`, `TenantSettings`, `Tenant`.
4. Run `GET /host/api/v1/reconciliation` → confirm the tenant no longer appears and every other
   tenant shows 0 anomalies.
5. If any step fails, **restore the backup** (never attempt a partial deletion).

Pending M7: a first-class host deletion workflow (with hard-delete confirmation, audit record,
and reconciliation trigger) replaces steps 3–4.

## 5. Policy changes

Retention/export/deletion behaviour changes require store-owner approval and are recorded here
(revision history below).

---

*Revision history*
- 2026-08-13: initial policy created (Phase 9). Deletion is manual + backup-first; export
  falls back to DB-level until M6 reporting lands.
