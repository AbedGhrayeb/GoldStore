# M7 — Optimization & Production Plan

Status: **in progress** (started 2026-08-13).

## Confirmed decisions

1. **Stay SQL Server** (`UseSqlServer`) for M7; the PostgreSQL migration stays a separate
   future effort.
2. **Out of scope — password reset & email verification**: logged-in users already update their
   profile and password via `UsersController.UpdateProfile` → `UpdateUserCommand`
   (security-stamp rotation + refresh-token revocation). Self-serve forgotten-password reset is
   a non-goal.
3. **HybridCache in-memory only** (no Redis L2).
4. **Skipped by owner: A4 (Dockerfile + docker-compose), A5 (Nginx topology) and A6
   (automated backup strategy).** All three remain open M7 items in `docs/tasks.md`; the stale
   `docker-compose.yml` (references the missing `src/Web.Api/Dockerfile`), the Nginx topology
   and manual backup flow stay as-is for now.

## Current-state findings (verified 2026-08-13)

- Rate limiter ("SlidingWindow") and output cache registered in `WebUI.DependencyInjection` but
  **never applied** to any endpoint.
- `GoldPriceService` uses `IMemoryCache` (15 min TTL, single-process, no stampede protection);
  no distributed cache.
- No account lockout; no password reset flow (by design, see decision 2).
- No audit trail — only `AuditableEntity` created/updated stamps via `AuditableEntityInterceptor`.
- Health-check packages referenced (`AspNetCore.HealthChecks.NpgSql`,
  `AspNetCore.HealthChecks.UI.Client`, `Microsoft.Extensions.Diagnostics.HealthChecks`) but not
  registered/mapped (`Program.cs` `//app.MapHealthChecks(...)`).
- `//app.ApplyMigrations();` commented out for non-Development.
- Secrets committed: `appsettings.json` (live goldapi.io key, commented databaseasp.net
  connection string, placeholder JWT secret) and the WebDeploy `publishSettings` file.
- `docker-compose.yml` stale — references nonexistent `src/Web.Api/Dockerfile`; no Dockerfile in
  the repo.
- No background jobs (trial/grace expiry manual).
- DB is SQL Server (`Infrastructure.DependencyInjection.AddDatabase` → `UseSqlServer`).

## Workstream A — Production config & deployment

- [x] **A1** Secret/config hygiene: env-var-driven config + `appsettings.Production.json`; scrub
      goldapi key / databaseasp.net connection string / JWT placeholder from `appsettings.json`
      (dev values move to user-secrets); move `publishSettings` out of source control.
- [x] **A2** Wire `ApplyMigrations()` for production behind an idempotent opt-in flag
      (`App:MigrateOnStartup`), keeping the Development `InitializeDatabaseAsync` path.
- [x] **A3** Health checks: `AddHealthChecks().AddSqlServer(...)` (or DbContext ping);
      map `/health` + `/health/ready`; JSON writer; exempt from tenant resolution.
- [ ] ~~**A4** Dockerfile + docker-compose fix~~ — **deferred by owner**.
- [ ] ~~**A5** Nginx: HTTPS termination, reverse proxy, static files (production topology config)~~ — **deferred by owner**.
- [ ] ~~**A6** Backup strategy~~ — **deferred by owner**.

## Workstream B — Authentication hardening

- [x] **B1** Account lockout: `FailedLoginAttempts` / `LockedUntilUtc` on `User`; enforce
      5 fails / 15 min in `LoginUserCommand` + `LoginPlatformUserCommand`; reset on success;
      integration tests (lockout, unlock, host path). Migration `M7AccountLockout`; 5 unit +
      4 integration tests added.
- [x] **B2** Rate limiting activation: `[EnableRateLimiting]` on login/refresh (Account,
      `api/auth`, host login) + strict login policy; 429 responses; health/static exempt; tests.
      `LoginLimiter` (IP-partitioned, config-tunable); `UseForwardedHeaders` (loopback trust) +
      `UseRateLimiter` in the pipeline; 2 integration tests added.

## Workstream C — Caching & performance

- [ ] **C1** HybridCache (in-memory): replace `IMemoryCache` in `GoldPriceService`; cache karats;
      tenant-keyed balance/KPI caches (`TenantId` prefix) with eviction on ledger writes.
- [ ] **C2** Output caching: apply only to tenant-safe GETs (gold price, karats, categories);
      vary by tenant key; document no cross-tenant leak; isolation test.
- [ ] **C3** Index audit: compare hot queries vs `docs/per-tenant-monitoring.md` inventory; add
      missing indexes via EF migration; before/after timings.
- [ ] **C4** Performance baseline report (KPI/balance/list timings).

## Workstream D — Audit & observability

- [ ] **D1** AuditLog entity + interceptor (create/update/delete, before/after JSON, user, tenant,
      timestamp); opt-in per aggregate; per `tenancy-policy.md` (status/plan/grace changes, host
      support access).
- [ ] **D2** Host-action audit: host login/logout, tenant provision, status changes.
- [ ] **D3** Alerting: Seq + optional webhook/email for monitoring triggers (failed tenant
      resolution, cross-tenant violation, read-only gate, anomalies, health down).

## Workstream E — Background jobs

- [ ] **E1** Job infra (Quartz.NET) with tenant-aware scoped scopes.
- [ ] **E2** Trial/Active → grace expiry job (subscription end → `Cancelled` + grace).
- [ ] **E3** Grace-end enforcement: block logins after grace; data retained (retention policy).

## Workstream F — Verification & docs

- [ ] **F1** Tests for B, C1/C2, D1, E2/E3, A2/A3; keep existing 34 tests green.
- [ ] **F2** Update `AGENTS.md`, `docs/tasks.md`, `docs/host-admin-runbook.md`,
      `docs/per-tenant-monitoring.md`, `docs/production-rollout-checklist.md`,
      `docs/data-retention-policy.md`.
- [ ] **F3** Frontend QA (MVC): error handling, loading states, RTL pass.

## Gates & order

A → B → C → D → E → F. `dotnet build GoldStore.slnx` + `dotnet test GoldStore.slnx` after each
workstream; docs updated within each.

## Phase 9 deferral coverage

| Phase 9 open item | M7 task |
| --- | --- |
| Secret hygiene / production config | A1 |
| `ApplyMigrations()` wiring | A2 |
| `/health` endpoint | A3 |
| Alerting | D3 |
| Trial/grace scheduled jobs | E2/E3 |
| Host action audit trail | D1/D2 |
| Plan-change endpoint | **still deferred** (no M7 owner; re-evaluate at Phase 7a) |
| Password reset | **out of scope** (decision 2) |

## Out of scope

PostgreSQL migration · password reset / email verification · M6 reporting/export ·
Phase 7a API / 7b Angular · Redis/distributed cache · Docker (A4) · automated backup (A6).
