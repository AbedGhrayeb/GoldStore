# GoldStore Per-Tenant Monitoring Guide (Phase 9)

Operational guidance for keeping tenant isolation healthy in production. Pair with
`docs/host-admin-runbook.md` (incident response) and `docs/production-rollout-checklist.md`
(baseline + post-go-live).

## 1. Index inventory (verified against GoldStoreDb, 2026-08-13)

Every tenant-owned table has a `TenantId`-first index; uniqueness is tenant-scoped.

| Table | Tenant index | Unique |
| --- | --- | --- |
| Categories | `IX_Categories_TenantId_ParentCategoryId_Name` | yes |
| Categories | `IX_Categories_TenantId_Name` | no |
| CustomerPurchaseInvoices | `IX_CustomerPurchaseInvoices_TenantId_InvoiceNumber` | yes |
| CustomerPurchaseInvoices | `IX_CustomerPurchaseInvoices_TenantId_Date` | no |
| ExpenseCategories | `IX_ExpenseCategories_TenantId_Name` | yes |
| FinancialAccounts | `IX_FinancialAccounts_TenantId_Name_Currency` | yes |
| RefreshTokens | `IX_RefreshTokens_TenantId` | no |
| SalesInvoices | `IX_SalesInvoices_TenantId_InvoiceNumber` | yes |
| SalesInvoices | `IX_SalesInvoices_TenantId_Date` | no |
| Suppliers | `IX_Suppliers_TenantId_Name` | yes |
| UserRoles | `IX_UserRoles_TenantId_UserId_RoleId` | yes |
| Users | `IX_Users_TenantId` | no |
| TenantSettings | `IX_TenantSettings_TenantId` | yes |

System-wide (not tenant-scoped by design): `Users.Email`, `RefreshTokens.TokenHash`,
`Tenants.Key`, `SubscriptionPlans.Key`, `PlatformUsers.Email`, `Permissions.Key`, `Roles.Key`,
`RolePermissions`.

Note: tables with no uniqueness requirement (Employees, ExpenseCategories-for-uniqueness, the
ledger tables) intentionally have no unique index; `Employees` has none at all — acceptable
(no unique natural key).

## 2. Hot query patterns to watch

- **KPI/balance reads** — store operations, gold ledger, debt, expense KPI handlers and
  account-balance queries. These aggregate ledger rows per tenant; confirm they seek on the
  `TenantId`-first indexes above rather than scanning.
- **List pages** — every GetPaged/List endpoint filters by `TenantId` before paginating.
- **Invoice number generation** — next-number lookups do a tenant-scoped
  `CountAsync(... StartsWith ...)`; backed by `(TenantId, InvoiceNumber)` unique index.
- **Reconciliation** — `GET /host/api/v1/reconciliation` scans tenant-owned tables per tenant; safe at
  current scale, re-check if row counts grow by orders of magnitude.

## 3. Missing-index workflow

Run periodically and on any KPI slowdown:

```sql
SELECT migs.user_seeks, migs.user_scans, migs.avg_total_user_cost,
       mid.database_id, mid.object_id, mid.statement AS table,
       mid.equality_columns, mid.incrementality, mid.included_columns
FROM sys.dm_db_missing_index_group_stats migs
JOIN sys.dm_db_missing_index_groups mig ON migs.group_handle = mig.index_group_handle
JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE mid.statement NOT LIKE '%__EFMigrations%'
ORDER BY migs.avg_total_user_cost * migs.user_seeks DESC;
```

Any candidate index must be **`TenantId`-first** (global query filters always apply `TenantId`)
and deployed via an EF Core migration, not ad-hoc DDL, so the schema stays reproducible.

## 4. Alert triggers

| Signal | Where it surfaces | Meaning / action |
| --- | --- | --- |
| 400/404 on tenant-protected endpoints with no resolvable tenant | Seq, `TenantId` scope missing | Tenant resolution failure — check hostname/config; runbook §5. |
| `403 tenant.access_violation` | Seq, error level | Cross-tenant write blocked by the guard interceptor — expected safety behaviour; no data action. |
| `403 tenant.read_only` | Seq, error level | Cancelled tenant in grace window — verify `TransitionAtUtc` if unexpected. |
| Insufficient stock / balance business errors | Seq + client toast | Ledger-level invariant violation — re-check with `/host/api/v1/reconciliation` before any correction. |
| `Anomalies` non-empty in `/host/api/v1/reconciliation` | Host endpoint | Ownership corruption — stop writes, restore from verified backup. |
| New tenant key appearing in row counts | `/host/api/v1/reconciliation` | Only from deliberate provisioning (runbook §1); otherwise investigate. |
| High-duration queries per tenant | Seq request duration, `TenantId` scope | Hot query regression — run the missing-index workflow, check plan for TenantId seek. |

## 5. Seq queries (per tenant)

Request logging enriches every log event with `TenantId` and `UserId`. Useful searches:

```
TenantId = '00000000-0000-0000-0000-000000000001' and Level = "Error"
TenantId = '<tenant>' and Duration >= 1000
HasProperty("tenant.access_violation")
```

## 6. Open monitoring items (M7)

- `/health` endpoint (DB connectivity) not enabled yet.
- No alerting/notification wiring (Seq is log-only today).
- Trial-end / grace-end scheduled jobs not yet implemented (status transitions are manual).

## Revision history

- 2026-08-13: initial guide (Phase 9); index inventory captured from live GoldStoreDb.
