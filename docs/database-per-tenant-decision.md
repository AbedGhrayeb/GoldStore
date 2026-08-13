# GoldStore Database-per-Tenant Decision (Phase 9)

## Decision

GoldStore keeps the **shared database, shared schema** architecture. All tenant-owned tables
carry a required `TenantId` with a `RESTRICT` FK to `Tenants`, a `TenantId`-first index, and a
`(TenantId, ...)`-scoped uniqueness constraint. This is the architecture already implemented
(Phases 3–6) and the Phase 9 decision is to **retain it**.

## Rationale

- Single-company, single-branch deployment today; expected store count is small
  (tens, not thousands).
- One SQL Server instance keeps operations, backup/restore, and reporting simple; per-tenant
  databases multiply backup chains and connection management without proportional benefit.
- Isolation is enforced in depth: global EF query filters on `IQueryable`, a write-guard
  interceptor that stamps and validates `TenantId` on save, tenant-scoped unique indexes,
  a host-only `/host/reconciliation` data-health report, and 23 integration tests covering
  cross-tenant read/write denial (Phase 8).
- Ledger rules (balances always derived, never stored) are tenant-agnostic and cheaper to
  audit in one schema.

## Mitigations already in place

- Query filters + write guard → cross-tenant reads/writes are impossible by construction.
- `(TenantId, ...)` unique indexes → tenant-scoped uniqueness (invoice numbers, supplier/category
  names, account names) without global collisions.
- `GET /host/reconciliation` → ownership anomalies and per-tenant balance health on demand.
- Serilog request-context logging scopes `TenantId`/`UserId` → per-tenant observability in Seq.
- Future: PostgreSQL RLS as defense-in-depth when the SQL Server → PostgreSQL migration lands
  (shared schema remains).

## Reassessment triggers

Move to **database-per-tenant** only if any of the following becomes real:

1. **Contractual isolation** — a store requires a written guarantee of physical data separation
   (e.g. an enterprise/gold-exchange tenant).
2. **Data residency** — a store is legally required to keep its data in a specific region,
   forcing a separate database.
3. **Dedicated performance guarantees** — one tenant's load threatens others (noisy neighbour)
   and shared-schema tuning cannot contain it.
4. **Exceptional customization** — a tenant needs schema-level customization that would pollute
   the shared schema.

## Cost of reassessment (when triggered)

Tenant routing must move from the `TenantId`-on-every-table filter to a per-request database
connection selected by the resolved tenant; reconciliation, monitoring, backups, and the
migration runbook all split per database. Estimate this as a dedicated multi-week effort before
committing.

## Revision history

- 2026-08-13: initial decision record (Phase 9) — shared schema retained.
