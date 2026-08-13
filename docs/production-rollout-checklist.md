# GoldStore Production Rollout Checklist (Phase 9)

Checklist for deploying GoldStore to production and onboarding the first store. The real
`GoldStoreDb` at the time of writing already contains the initial tenant (`goldstore`) — the
"existing store" — so the checklist covers the pre-flight, the demo-tenant validation, and the
initial-store verification that precede go-live.

## 0. Baseline snapshot (taken 2026-08-13)

`GET /host/reconciliation`-equivalent run against `GoldStoreDb`:

- **Tenants:** exactly one — `goldstore` (`00000000-0000-0000-0000-000000000001`), status `Active`.
- **Anomalies:** none (every tenant-owned row has a non-null `TenantId`; no orphan FKs).
- **Row counts (per tenant):**

| Table | Rows |
| --- | --- |
| Categories | 1 |
| FinancialAccounts | 3 |
| RefreshTokens | 1 |
| Suppliers | 1 |
| TenantSettings | 1 |
| TenantSubscriptions | 1 |
| UserRoles | 1 |
| Users | 2 |
| All other tenant-owned tables | 0 |

- **Indexes verified (TenantId-first / tenant-unique):** see `docs/per-tenant-monitoring.md` §1.

Any later run must show the same shape — growing row counts are expected, but new tenants must
never appear and `Anomalies` must stay empty.

## 1. Pre-flight (each release)

- [ ] `dotnet build GoldStore.slnx` clean (TreatWarningsAsErrors).
- [ ] `dotnet test GoldStore.slnx` green (34/34: 7 unit + 23 integration + 4 architecture).
- [ ] Full DB backup taken **and verified to restore** into a scratch database
      (`docs/multi-tenancy-migration-runbook.md` §0).
- [ ] Secrets removed from source control / moved to env vars (open item, M7 — see
      `docs/host-admin-runbook.md` §6).
- [ ] Migrations applied explicitly:
      `dotnet ef database update --project src/Infrastructure --startup-project src/WebUI`
      (`ApplyMigrations()` is not wired outside Development).
- [ ] `GET /host/reconciliation` → 1 tenant, `Anomalies` empty; compare with the §0 baseline.

## 2. Demo-tenant validation (every new store onboarding)

1. Sign in as a platform admin: `POST /host/login` (`admin@goldstore` / the configured
   `DefaultPlatformUserPassword`).
2. Provision: `POST /host/tenants` with a distinct key (see runbook §1). Confirm `tenantId`
   is returned.
3. List: `GET /host/tenants` shows the new tenant as `Active`.
4. Sign in as the store admin on the tenant; confirm the seeded roles and JOD/USD/ILS cash
   accounts; exercise one KPI + one create flow (e.g. an expense) so gold/financial ledgers
   move.
5. Confirm isolation on the spot: a store user cannot reach `/host/*` (401/redirect), and a
   cross-tenant edit is rejected (403 `tenant.access_violation`).
6. Run `GET /host/reconciliation` → two tenants, both `Anomalies` empty.
7. Hand over admin credentials; the store changes the password.

## 3. Existing store as the initial production tenant

- The `goldstore` tenant is already seeded and its data is stamped with the fixed tenant ID
  (`00000000-...-0001`). No backfill is required for a fresh or current schema
  (`docs/multi-tenancy-migration-runbook.md` "Current repo state").
- Verify each operational area once before go-live: suppliers, inventory, debts, sales,
  purchases, expenses, HR (the M1–M5 feature set).
- Record a fresh reconciliation snapshot as the new baseline.

## 4. Go-live

- [ ] HSTS enabled (Development-only code path off; `appsettings` points at production).
- [ ] `/health` — not yet enabled (open item, M7); substitute: reconciliation + Seq reachable.
- [ ] Gold API reachable from production (`GoldApi:BaseUrl`, `CacheDurationMinutes`), with the
      API key supplied via env var.
- [ ] Seq endpoint reachable (`Serilog:WriteTo` Seq server) — required for per-tenant
      log scoping.
- [ ] Cookie auth configured for the public host; note `RequireHostnameVerification` remains
      `false` (single-host deployment — see runbook §6 for the subdomain path).

## 5. Post-go-live

- [ ] Daily: `GET /host/reconciliation` snapshot; compare to baseline.
- [ ] Watch the alert triggers in `docs/per-tenant-monitoring.md` §4.
- [ ] Keep every tenant's row counts and balances traceable back to a baseline snapshot for
      at least the current billing period.
