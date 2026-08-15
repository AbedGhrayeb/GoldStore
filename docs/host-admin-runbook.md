# GoldStore Host-Administration Runbook (Phase 9)

Operational guide for platform administrators. It assumes the host identity model from
`docs/tenancy-policy.md` (platform administrators use the separate `PlatformUser` identity and
dedicated `/host/*` endpoints) and covers provisioning, lifecycle management, support access,
and incident response.

## Access model

- **Platform admins** authenticate through `POST /host/api/v1/auth/login` (host cookie scheme). Only
  `PlatformUser` records can log in; customer (`User`) credentials are rejected.
- All `/host/*` endpoints carry the `[HostOnly]` attribute and are **exempt from tenant
  resolution**. A customer role can never reach them (pinned by
  `TenantUser_CannotReachHostAdministration`).
- Store tenants are served by the same host in this deployment
  (`Tenancy:RequireHostnameVerification = false`); see "Deployment notes" for the subdomain
  trade-off.

### Tenant API surface (Phase 7a complete)

Store-facing integrations use the **tenant** API under `/api/v1` with JWT bearer tokens
(`POST /api/v1/auth/login`), one endpoint group per module (`Users`, `Categories`,
`GoldPrices`, `Reference`, `Suppliers`, `SupplierDeliveries`, `SupplierPayments`,
`SupplierFinancialTransactions`, `Inventory`, `SalesInvoices`, `CustomerPurchaseInvoices`,
`Finance`, `Expenses`, `Employees`, `Dashboard`). All tenant routes require a valid, operational
current tenant and enforce feature gates; `TenantId` is never accepted from the client.
OpenAPI document (Development): `/openapi/v1.json`; interactive reference: Scalar at
`/scalar/v1`. Endpoint pattern: one `IEndpoint` class per group in `src/WebUI/Endpoints/*`,
auto-discovered via `AddEndpoints()`/`MapEndpoints()` — adding a group never touches `Program.cs`.

### Endpoint inventory

| Endpoint | Method | Purpose |
| --- | --- | --- |
| `/host/api/v1/auth/login` | GET | Renders the host sign-in page (browser redirect target). |
| `/host/api/v1/auth/login` | POST | Platform admin login; sets the host cookie. |
| `/host/api/v1/auth/logout` | POST | Clears the host cookie. |
| `/host/api/v1/tenants` | GET | List all tenants (id, key, name, status). |
| `/host/api/v1/tenants` | POST | Provision a new tenant (body: `ProvisionTenantRequest`). |
| `/host/api/v1/tenants/{tenantId}/status` | PATCH | Change tenant status (body: `UpdateTenantStatusRequest`). |
| `/host/api/v1/reconciliation` | GET | Per-tenant data-health report; optional `?tenantId=` filter. |

## 1. Provisioning a new tenant

`POST /host/api/v1/tenants` with:

```json
{
  "name": "Al Noor Gold",
  "key": "al-noor-gold",
  "timeZoneId": "Asia/Amman",
  "adminFirstName": "Ahmad",
  "adminLastName": "Hassan",
  "adminEmail": "ahmad@al-noor-gold.example",
  "adminPassword": "<strong-temporary-password>",
  "subscriptionPlanId": "<plan-guid-from /seed or DB>",
  "billingCycle": "Monthly",
  "startsAtUtc": "2026-08-13T00:00:00Z",
  "endsAtUtc": "2026-09-13T00:00:00Z"
}
```

What `ProvisionTenantCommand` creates atomically:

- The `Tenant` (status `Active`),
- `TenantSettings` (all feature flags enabled by default),
- a `TenantSubscription` bound to the selected `SubscriptionPlan` and billing cycle,
- the admin `User` (email uniqueness is **system-wide** — the address must not already exist),
- the `store_admin` `UserRole` grant,
- seed financial accounts (JOD, USD, ILS cash accounts).

Rules:

- Tenant key must match `^[a-z0-9]([a-z0-9-]*[a-z0-9])?$` — it becomes the canonical
  `https://{tenant-key}.goldstore.app` subdomain when hostname verification is enabled.
- `adminEmail` must be globally unique (`IX_Users_Email`).
- The subscription must be within the chosen plan's billing window.

Handover: share the temporary admin password out of band. There is **no self-serve password
reset yet** (open item, M7) — until then, a forgotten password is reset by an admin directly
in the store or escalated per incident response below.

## 2. Suspension and reactivation

`PATCH /host/api/v1/tenants/{tenantId}/status` with:

```json
{ "newStatus": "Cancelled", "transitionAtUtc": "2026-09-12T00:00:00Z" }
```

Allowed transitions (`UpdateTenantStatusCommand`):

- `Active` → `Pending` | `Trial` | `Cancelled`
- `Trial` → `Active` | `Cancelled`
- `Pending` → `Trial` | `Active`
- `Cancelled` → `Active`

Behavioural effects:

- Transition to `Cancelled` **auto-cancels the active subscription** and sets the read-only
  grace window ending at `transitionAtUtc`. Until then the store is **read-only**
  (writes fail with `403 tenant.read_only`); after the grace end, store logins are denied.
- `Pending` tenants cannot log in or use operational APIs.
- Reactivation (`Cancelled` → `Active`) restores full access but does **not** re-create a
  subscription — create a new one via provisioning/plan change before reactivating a paying
  store.
- A `Trial` that reaches its end date is transitioned to `Cancelled` by a scheduled job when
  the trial-job milestone (M7) ships; until then this is a manual host action.

## 3. Plan changes and limits

There is **no dedicated plan-change endpoint yet** (open item, M7). Today:

- Status changes go through the PATCH endpoint above.
- Subscription term/plan edits require a DB operation (backed up) or the future endpoint.
- Enforced limits are: **active users**, **invoices per billing period** (final/posted sales +
  customer purchases), **active branches** and **storage** (both reserved, enforced when the
  branch module / uploads ship). Generic add/edit operations are never limited by plan.

## 4. Support access

- Sign in as a `PlatformUser` at `/host/api/v1/auth/login` (never reuse a customer `User`).
- Use `/host/api/v1/tenants` to find the tenant, and `/host/api/v1/reconciliation?tenantId=...` to inspect a
  store's data health before/after support operations.
- **Audit-trail recording for host actions is not yet implemented** (open item, M7). Today host
  activity is captured by request logging only (Serilog scopes `TenantId`/`UserId`). Do not
  rely on the system for legal-grade auditing until M7 lands.

## 5. Incident response

| Symptom | Where to look | Action |
| --- | --- | --- |
| Failed tenant resolution on store endpoints | 400/404 ProblemDetails; Serilog scope | Verify the route is tenant-protected and the request carries a resolvable tenant; for API clients the 401 host challenge on `/host/*` is expected. |
| Cross-tenant write rejected | `TenantAccessViolationException` → `403 tenant.access_violation` | A store user attempted to touch another tenant's aggregate. Check the `TenantId` scopes in logs; no data action needed — the write was blocked before commit. |
| Read-only grace gate | `403 tenant.read_only` | Tenant is in the cancellation grace window. Expected; confirm `TransitionAtUtc` is correct. |
| Insufficient stock / balance errors | Business-error toast (`status: 0`), reconciliation | Recompute from ledgers; gold/financial balances are never stored, only derived. |
| Suspected data corruption | `GET /host/api/v1/reconciliation` | `Anomalies` must be empty and per-tenant row counts must match the rollout baseline (see `docs/production-rollout-checklist.md`). Any mismatch → stop writes, restore from the last verified backup (see `docs/multi-tenancy-migration-runbook.md` §8). |
| Forgotten admin password | — | No self-serve reset (M7). Reset the password hash in the DB after taking a backup, or re-provision in a scratch copy. |

General rule: **the migration is forward-only in place** — rollback is always "restore the
verified backup + roll back the release", never a partial reverse of tenant IDs.

## 6. Deployment notes

- `Tenancy:RequireHostnameVerification = false` (single-host deployment on runasp.net).
  Trade-off: the canonical `{tenant-key}.goldstore.app` subdomain is **not enforced**, so a
  request hostname cannot be trusted for tenant routing yet. Before enabling subdomains:
  1. point `*.goldstore.app` at the host,
  2. set the cookie domain to `.goldstore.app` so the login carries to subdomains,
  3. flip `RequireHostnameVerification = true`,
  4. re-run the Phase 8 hostname tests.
- `ApplyMigrations()` is **not** wired for production (`//app.ApplyMigrations();` in
  `Program.cs` is commented out outside Development). Apply migrations explicitly with
  `dotnet ef database update --project src/Infrastructure --startup-project src/WebUI`.
- Health probes are enabled and tenant-exempt: `/health` (all checks, UI writer),
  `/health/ready` (database connectivity via `DatabaseHealthCheck`), `/health/live`
  (process liveness). Use them for operational monitoring alongside
  `/host/api/v1/reconciliation` and Seq.
- Committed `appsettings.json` holds the dev connection string, the goldapi.io key, and a
  placeholder JWT secret; and `sarhangold.runasp.net-WebDeploy.publishSettings` contains
  deployment credentials. **Move all secrets to environment variables / secrets store before
  production rollout** (open item, M7).
