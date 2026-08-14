# GoldStore Multi-Tenancy Migration Runbook (Phase 6)

This runbook covers migrating an **existing single-store GoldStore deployment** into the
multi-tenant schema (plan Phase 6). Perform it in a **non-production copy first** and take a
**verified backup** before touching production.

> **Current repo state:** the schema migrations (`20260810213935_InitialCreate`,
> `20260811162538_Phase3TenantIndexes`, `20260811190134_Phase4Authentication`) already create
> every tenant-owned table with a **required `TenantId`**, a `RESTRICT` foreign key to
> `Tenants`, and an index starting with `TenantId`. The initial tenant is seeded by
> `ApplicationDbContextInitializer.SeedInitialTenantAsync` with a fixed ID. A fresh or
> development database therefore needs **no backfill**. The SQL below documents the path for a
> legacy database that predates tenancy.

## 0. Backup

- Stop writes to the source database.
- Take a logical backup and verify it restores into a scratch database:
  `pg_dump -Fc -f goldstore-pre-migration.dump <db>` then restore into a scratch DB and run a
  row-count spot check.
- Record the backup checksum and store it outside the production server.

## 1. Create the initial tenant

```sql
INSERT INTO "Tenants" ("Id", "Name", "Key", "Status", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('<fixed-tenant-guid>', 'GoldStore Store', '<tenant-key>', 2, now(), now());
```

The tenant key must match `^[a-z0-9]([a-z0-9-]*[a-z0-9])?$`. Record the fixed GUID — every
backfilled row uses it.

## 2. Add nullable `TenantId` columns

```sql
ALTER TABLE "Users"            ADD COLUMN "TenantId" uuid NULL;
ALTER TABLE "Suppliers"        ADD COLUMN "TenantId" uuid NULL;
-- ... repeat for every tenant-owned table ...
```

## 3. Backfill every existing row

```sql
UPDATE "Users"            SET "TenantId" = '<fixed-tenant-guid>';
UPDATE "Suppliers"        SET "TenantId" = '<fixed-tenant-guid>';
-- ... repeat for every tenant-owned table, including both ledgers
-- (GoldLedgerEntries, FinancialTransactions, DebtLedgerEntries,
--  SupplierGoldLedgerEntries, SupplierManufacturingLedgerEntries) ...
```

## 4. Validate ownership before adding constraints

Run ownership checks; each of the following must return **0** rows before proceeding:

```sql
-- Rows still missing a tenant
SELECT count(*) FROM "SalesInvoices" WHERE "TenantId" IS NULL;

-- Rows whose tenant does not exist (orphans)
SELECT count(*) FROM "SalesInvoices" s
LEFT JOIN "Tenants" t ON t."Id" = s."TenantId"
WHERE t."Id" IS NULL;
```

Then run the full reconciliation report (item 7) and snapshot the output.

## 5. Add constraints

```sql
ALTER TABLE "SalesInvoices" ALTER COLUMN "TenantId" SET NOT NULL;
ALTER TABLE "SalesInvoices"
  ADD CONSTRAINT "FK_SalesInvoices_Tenants_TenantId"
  FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_SalesInvoices_TenantId" ON "SalesInvoices" ("TenantId");
-- ... repeat for every tenant-owned table ...
```

Tenant-aware unique constraints must replace any old global ones, e.g.:
`(TenantId, InvoiceNumber)`, `(TenantId, Name)` for suppliers/categories,
`(TenantId, Name, Currency)` for accounts. Email uniqueness stays **system-wide**
(`Users.Email`, `PlatformUsers.Email`).

## 6. Ship code and constraints together

Do not release a build that writes rows without a `TenantId`. The write guard
(`TenantEntityInterceptor` + the `SaveChanges` override in `ApplicationDbContext`) and the
global query filters must be deployed in the **same release** that applies these constraints.

## 7. Reconcile before and after

The host-only endpoint **`GET /host/api/v1/reconciliation`** (host cookie auth) returns, per tenant:
- row counts for every tenant-owned table,
- ownership anomalies (empty or unknown `TenantId`),
- gold stock per karat and total 21K-equivalent weight recomputed from `GoldLedgerEntries`,
- financial balances per account and totals per currency from `FinancialTransactions`,
- receivable/payable totals per currency from `DebtLedgerEntries`,
- supplier gold and manufacturing balances from their ledger entries.

Procedure:
1. Before migration: snapshot `GET /host/api/v1/reconciliation` (all tenants).
2. After migration: run it again on the migrated copy.
3. Compare `RowCounts` and every recomputed balance — they must be identical.
4. `Anomalies` must be empty.
5. Spot-check totals: users, suppliers, invoices, gold ledger totals, financial ledger totals.

## 8. Rollback plan

- **The migration is forward-only in-place.** Never attempt a partial reverse of tenant IDs.
- Rollback = **restore the verified pre-migration backup** (item 0) into the production
  database and roll back the application to the previous release.
- If the constraint step (item 5) fails validation: do not proceed to item 6; restore the
  backup. There is no safe partial state between "nullable backfilled" and "constrained".
- After any restore, re-run the reconciliation report and confirm `Anomalies` is empty and
  counts match the pre-migration snapshot before reopening the application to users.
