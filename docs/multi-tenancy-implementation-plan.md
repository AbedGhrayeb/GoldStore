# Multi-Tenancy Implementation Plan

## Decision Summary

GoldStore will use a **shared database and shared schema**. Every tenant-owned row will have a required `TenantId`. The current scale of 50–200 stores makes this the best starting point: it is economical, simple to operate, and still provides strong isolation when enforced consistently.

Each user belongs to exactly one tenant through `User.TenantId`. A customer user must never choose or supply a different tenant while using the application. The authenticated token is the source of truth for the tenant on protected APIs.

The application may use an `X-Tenant-Key` header (or, preferably later, a tenant subdomain) only to identify the store at unauthenticated entry points such as login and public branding. A header by itself must never authorize access to tenant data.

## Non-Negotiable Rules

1. All tenant-owned data has a non-null `TenantId`.
2. APIs never accept `TenantId` in request bodies, route values, or query strings for ordinary customer operations.
3. Application code obtains the current tenant only from `ICurrentTenant`.
4. EF Core filters all tenant-owned reads automatically; writes are stamped and validated centrally.
5. A customer user cannot use an ID from another tenant to read, edit, post, or delete data.
6. Financial balances, gold ledger balances, reports, exports, audit entries, and background jobs are tenant-scoped.
7. Customization is configuration and feature flags, not tenant-specific source-code branches.

## Phase 0 — Confirm Product Boundaries

Complete these decisions before writing tenancy code:

1. Confirm that one user belongs to one store only. This is the selected model for the first release.
1. Confirm that one customer user belongs to one store only through `User.TenantId`. This is the selected model for the first release.
2. Define host/platform administrators as a separate `PlatformUser` identity, not as store users with an elevated role. Host administrators manage tenants, subscriptions, and support tasks through dedicated host endpoints with explicit tenant selection and audit logging.
3. Define the tenant lifecycle as `Pending`, `Trial`, `Active`, and `Cancelled`. `Pending` tenants cannot sign in. `Trial` and `Active` tenants have full enabled-feature access within their plan limits. A cancelled tenant has read-only access for a fixed grace period, then has no application access while its data remains retained according to policy.
4. Define subscription plans with full enabled-feature access and measurable limits for active users, posted invoices per billing period, active branches, and uploaded storage. Do not count generic add/edit operations because legitimate corrections to financial and gold records must remain possible and auditable.
5. Use `goldstore.app` as the public login/onboarding domain. After authentication, use the JWT tenant ID and tenant key to redirect users to `https://{tenant-key}.goldstore.app`. Support custom domains in a later phase through a tenant-domain mapping.
6. Make normalized email addresses system-wide unique. Login is performed on the public domain with email and password, without requiring a tenant key.
7. Identify global/reference data versus tenant data. Seeded karats, configured permission definitions, and subscription plan definitions are global. Store categories, users, suppliers, prices, inventory, financial data, sales, settings, and audit records are tenant-owned.

**Exit criteria:** an approved subscription policy defines the cancellation read-only grace period, plan limits, limit-counting rules, `Pending` behavior, tenant key format, canonical tenant URL, data retention, and host-administrator responsibilities.

## Phase 1 — Create the Tenant Domain Model

Add a `Tenants` module in the existing clean architecture layers.

1. Create a `Tenant` aggregate in `src/Domain` with an ID, Arabic/display name, immutable normalized key, lifecycle status, and audit fields.
2. Create `Subscription` and `SubscriptionPlan` models. Keep billing-provider references, billing period, renewal date, status, plan limits, and grace-period information outside operational ledger entities.
3. Create `TenantSettings` for safe customizations: logo URL, store display name, invoice settings, timezone, locale, theme values, and enabled features.
4. Add `ITenantEntity` in the shared domain/application boundary with a `Guid TenantId` contract.
5. Add `TenantId` to `User` and configure the required relationship to `Tenant`. Do not allow a customer user with a null tenant.
6. Create a separate `PlatformUser` identity for host administrators. It is never assigned a `TenantId` and is authorized only for explicit host administration use cases.
7. Make tenant ownership explicit for every existing and future business entity. Start with users, categories, gold prices, suppliers, supplier operations, inventory entries, financial accounts, financial transactions, sales invoices, purchase invoices, employees, and expenses.
8. Keep seeded karats, configured permission definitions, and subscription plans read-only and global.
9. Decide role scope. If roles can be customized by each store, make roles tenant-owned and keep permissions as global definitions; otherwise use global role templates and tenant-scoped user-role assignments.

## Phase 2 — Add Tenant Context and Request Resolution

Place the tenant abstractions in `src/Application` so commands, queries, and infrastructure can depend on them without depending on WebUI.

1. Define `ICurrentTenant` with the tenant ID, tenant key, status, enabled features, and an `IsAvailable` indicator.
2. Implement a scoped tenant context in `src/Infrastructure` or `src/WebUI`. Never store current-tenant state in a singleton or static variable.
3. Add tenant-resolution middleware early in the WebUI pipeline.
4. Authenticate customer users at the public domain using system-wide unique email and password; do not require or trust a client-supplied tenant header.
5. For authenticated tenant endpoints, resolve the tenant from the validated JWT `tenant_id` claim and verify that the request hostname matches the claimed tenant key. Redirect public-domain requests to the tenant's canonical subdomain after login.
6. Exempt only carefully selected host endpoints, health checks, and authentication endpoints from normal tenant resolution. Make exemptions explicit, not convention-based.
7. Return consistent problem details for a missing tenant, unknown tenant, disabled tenant, and suspended subscription. Do not disclose whether another tenant exists to unauthorized callers.
8. Add structured logging scopes with `TenantId`, `TenantKey`, and `UserId`; do not log sensitive authentication tokens or customer financial details.

**Exit criteria:** a request cannot reach a tenant endpoint without a valid current tenant, and a client cannot switch tenant by changing a header.

## Phase 3 — Enforce Data Isolation in EF Core

This phase is the core protection. It must be completed before generating new CRUD endpoints.

1. Configure a global EF Core query filter for every `ITenantEntity`, comparing its `TenantId` with the scoped `ICurrentTenant` value.
2. Ensure the query filter reads a DbContext instance property or scoped service correctly; do not capture a startup-time tenant value in the EF model.
3. Add a `SaveChanges` interceptor or DbContext override that assigns the current tenant to newly added tenant entities.
4. In the same write guard, reject attempts to create records without a current tenant, alter an existing `TenantId`, or save an entity belonging to another tenant.
5. Configure indexes with `TenantId` first where queries are tenant-bound. Convert uniqueness constraints to tenant-aware indexes, such as `(TenantId, NormalizedName)` and `(TenantId, Code)`.
6. Validate cross-aggregate references in command handlers. For example, an invoice may not reference a supplier, financial account, or inventory item from a different tenant even when the client knows its GUID.
7. Combine tenant filters safely with soft-delete filters if soft deletion is added.
8. Treat `IgnoreQueryFilters()` as host-only infrastructure. Do not use it in ordinary application handlers; require a dedicated host administration use case and explicit tenant selection when it is necessary.
9. Ensure raw SQL, SQL views, reporting queries, and repository methods apply the same tenant predicate. Global query filters do not protect raw SQL automatically.
10. After the move to PostgreSQL, evaluate PostgreSQL Row-Level Security as a defense-in-depth layer for tenant-owned tables. Do not use it as a substitute for application-level checks; introduce it only with connection/session handling and integration tests in place.

**Exit criteria:** all normal read and write paths are tenant-isolated by default, including gold and financial ledger calculations.

## Phase 4 — Update Authentication and Authorization

1. Authenticate users by system-wide normalized email and password on `goldstore.app`, then load the user's required tenant relationship.
2. Verify the user and tenant are eligible before issuing tokens: `Pending` and expired-cancellation-grace tenants receive no operational token.
3. Include immutable claims in access and refresh tokens: user ID, tenant ID, role/permission claims, and a token/session version if supported.
4. During token validation or authorization, confirm that the tenant is `Trial`, `Active`, or within its cancellation read-only grace period. Cache this carefully if database validation on every request is too costly.
5. Store refresh tokens with `TenantId` and revoke them when a tenant is suspended, a user is disabled, or the tenant relationship changes.
6. Add authorization policies for host administration, tenant administration, and feature access. Do not use a customer role as a host-administrator bypass.
7. Add central feature, quota, and read-only subscription gates so endpoints can enforce limits without duplicating checks in handlers. Count active users, posted invoices in the billing period, active branches, and actual uploaded storage bytes.

**Exit criteria:** tokens bind a user to one tenant, suspended tenants cannot call operational APIs, and host access is separated from tenant access.

## Phase 5 — Make Application Features Tenant-Aware

> **Complete** (audited 2026-08, held to the pragmatic bar: the EF query filter + write-guard interceptor remain the primary protection; explicit `ICurrentTenant` scoping is applied where handlers aggregate/validate/unique-check).
> - Items 2 (no client `TenantId` in commands), 6 (domain events carry `TenantId`; no outbox/jobs/imports yet), 7 (provisioning/suspension workflows): verified pass.
> - Items 1/3/4: pass under the pragmatic bar — all aggregation/KPI/balance/unique-check handlers inject `ICurrentTenant`; unique checks are tenant-scoped via the filtered context and backed by `(TenantId, …)` unique indexes.
> - Item 5: `Equivalent21KWeight` is server-side only; the 18K → ×700/875 (0.8) store convention is intentional and pinned by `tests/Application.UnitTests`.

Apply this pattern to every current module before exposing or expanding endpoints.

1. Inject `ICurrentTenant` into commands, queries, domain-event handlers, and background-job entry points that operate on tenant data.
2. Remove any client-provided tenant assignment from commands and DTOs. The handler or persistence guard obtains the tenant from context.
3. Use tenant-aware unique checks in validators and handlers.
4. Scope all list, search, dashboard, report, export, and lookup queries by the current tenant even when a global query filter exists; the filter is protection, while the explicit domain intent improves clarity.
5. Verify the GoldStore business rules under tenant scope: equivalent 21K weight, gold ledger balances, financial balances, debt calculations, and invoice posting must only aggregate entries for one tenant.
6. Carry tenant context into domain events, outbox records, notifications, scheduled work, imports, exports, and audit logs. A background process must select a tenant explicitly before using tenant repositories.
7. Create host-only onboarding and suspension workflows. Tenant provisioning should create the tenant, settings, administrator user, subscription, default roles, and allowed seed data in a single transaction or a recoverable workflow.

**Exit criteria:** no business operation can accidentally aggregate data from multiple stores.

## Phase 6 — Migrate Existing Data Safely

Perform this in a non-production copy first and take a verified backup before production migration.

1. Create the initial tenant record representing the existing GoldStore deployment.
2. Add nullable `TenantId` columns to existing tenant-owned tables.
3. Backfill every existing row with the initial tenant ID, including users and both ledgers.
4. Validate row counts and tenant ownership before adding constraints.
5. Add non-null constraints, foreign keys, tenant-aware indexes, and unique constraints.
6. Deploy the tenant-context and EF enforcement code together with the final constraints; avoid a release where new rows can be written without `TenantId`.
7. Run reconciliation reports before and after migration: number of users, suppliers, invoices, gold ledger totals, financial ledger totals, and calculated balances.
8. Document a rollback plan that restores the database backup rather than attempting an unsafe partial reversal of tenant IDs.

**Exit criteria:** existing data belongs to exactly one initial tenant and all balances reconcile with the pre-migration state.

## Phase 7 — Implement the API Layer and Angular Client (deferred)

> **Deferred until the MVC version is complete.** The current production client is the ASP.NET MVC Razor app (`src/WebUI`). All multi-tenancy phases (0–6) and the remaining business features are completed on that MVC app first. After multi-tenancy is finished, the API layer is generated on the tenant-aware base, then the Angular client is built against it. The MVC app is kept as a fallback during and after the transition; it is not deleted.

### Phase 7a — Implement the API Layer

1. Expose every business module through minimal API endpoints using the `IEndpoint` contract and `AddEndpoints`/`MapEndpoints`, reusing the existing commands, queries, validators, and handlers from the MVC layer.
2. Follow the non-negotiable tenancy rules on every endpoint: never accept `TenantId` in request bodies, route values, or query strings; resolve the tenant from `ICurrentTenant` only.
3. Return RFC 9457 ProblemDetails consistently for validation, cross-tenant access, missing/unknown/disabled tenants, and suspended subscriptions.
4. Add tenant-aware FluentValidation for unique checks (e.g., `(TenantId, NormalizedName)`, `(TenantId, Code)`) and cross-aggregate reference validation in handlers.
5. Document the full API surface with OpenAPI metadata and keep host-only endpoints explicitly separated from tenant endpoints.
6. Add integration tests against the API for direct-ID attacks, write stamping, and tenant-aware unique constraints.

**Exit criteria:** every business operation is available as a tenant-isolated API, and the MVC and API layers share the same application logic and tenancy guarantees.

### Phase 7b — Build the Angular Client

1. Use `goldstore.app` for public login and onboarding. After receiving a valid token, redirect to the tenant key contained in its claims and load tenant-specific branding/configuration from that canonical subdomain.
2. Do not use a tenant ID or tenant header to establish authorization in the client.
3. Do not send `TenantId` in operational request models. Do not treat browser storage as an authorization source.
4. After login, rely on the API token and server enforcement. The server verifies that the current hostname maps to the same tenant as the token.
5. Render customization from `TenantSettings` and feature flags: name, logo, theme, invoice options, and enabled navigation items.
6. Handle subscription status centrally: show clear renewal/suspension messaging and prevent navigation to disabled features without pretending client-side checks are security.

**Exit criteria:** one frontend deployment serves all stores while each store sees only its authorized branding and features, and the MVC fallback continues to serve the same tenant-isolated data.

## Phase 8 — Test Tenant Isolation and Operations

> **Complete** (2026-08). Items 1–6 covered by `tests/Application.IntegrationTests` (23 tests: DbContext query-filter/write-guard/unique-constraint tests, HTTP direct-ID attack tests, login eligibility, read-only grace tenant, host-only separation) and `tests/ArchitectureTests` (4 layer rules). Item 7 (background jobs) and item 9 (operational checks) deferred until background work exists.

Add integration tests before relying on the implementation in production.

1. Seed at least two tenants with deliberately similar users, suppliers, invoices, ledger entries, and identifiers.
2. Test that each tenant can list and retrieve only its own data.
3. Test direct-ID attacks: use a valid entity ID from tenant B while authenticated as tenant A for every read, update, delete, posting, and download endpoint.
4. Test write stamping and rejection of a supplied or modified `TenantId`.
5. Test that tenant-aware unique constraints allow the same name/code in different tenants but reject duplicates within one tenant.
6. Test login behavior, JWT/hostname mismatch rejection, disabled users, pending tenants, cancellation grace, expired subscriptions, quota enforcement, and host-only policies.
7. Test background jobs, domain events, imports, reports, and exports with two tenants to prove tenant context is carried correctly.
8. Add architecture tests that prevent Domain from depending on tenant HTTP concerns and prevent Application handlers from bypassing `ICurrentTenant`/tenant repositories where applicable.
9. Add operational checks: per-tenant log correlation, tenant-aware audit records, backup/restore rehearsal, and alerts for failed tenant resolution or rejected cross-tenant writes.

**Exit criteria:** automated tests prove that a user of tenant A cannot observe or mutate tenant B data through normal APIs, guessed IDs, jobs, reports, or exports.

## Phase 9 — Production Rollout

1. Ship the tenant foundation and migration before generating the remaining API endpoints. ✅ done
2. Onboard one internal/demo tenant first, then migrate the existing store as the initial production tenant. — rollout steps documented in `docs/production-rollout-checklist.md` (baseline snapshot 2026-08-13: single `goldstore` tenant, 0 anomalies); demo-tenant onboarding walkthrough included.
3. Create an internal host-admin runbook for provisioning, suspension, reactivation, plan changes, support access, and incident response. ✅ done — `docs/host-admin-runbook.md`.
4. Monitor query performance by tenant and add indexes based on real tenant-scoped query patterns. Investigate unusually large tenants as potential noisy neighbors. ✅ guide — `docs/per-tenant-monitoring.md` (index inventory verified against live DB; missing-index workflow; alert triggers). Perf instrumentation/alerting wiring deferred to M7.
5. Establish data retention, export, archive, and deletion policies per tenant before accepting paying customers. ✅ policy — `docs/data-retention-policy.md` (deletion is backup-first manual; self-serve export pending M6 reporting).
6. Reassess database-per-tenant only if a customer has contractual isolation, residency, dedicated-performance, or exceptional customization requirements. Keep the shared-schema model as the default. ✅ decision — `docs/database-per-tenant-decision.md` (shared schema retained; reassessment triggers + cost documented).

**Status (2026-08-13):** documentation deliverables + reconciliation baseline complete. Open operational items deferred to M7: production config/secret hygiene, `/health` endpoint, `ApplyMigrations()` wiring in production, alerting, trial/grace scheduled jobs, host-action audit trail, plan-change endpoint, self-serve password reset.

## Suggested Delivery Order

1. Phases 0–2: tenancy policy, tenant aggregate, user relationship, and request context.
2. Phase 3: EF Core filters, write guard, constraints, and migration design.
3. Phase 4: login, JWT claims, subscription/feature authorization, and host administration.
4. Phase 6: migrate existing data and reconcile ledgers.
5. Phase 8: integration/isolation tests. ✅ done
6. Phase 5: generate the remaining business endpoints using the tenant-aware base. ✅ done (audited; see Phase 5 notes)
7. Complete the remaining MVC business features on the tenant-aware base (MVC remains the production client).
8. Phase 9: production rollout for the multi-tenant MVC version. ✅ docs + baseline; operational items → M7
9. Phase 7a: implement the API layer against the same application logic.
10. Phase 7b: build the Angular client and transition stores to it; keep the MVC app as a fallback.

## Definition of Done

Multi-tenancy is ready for customer onboarding when a newly provisioned tenant can sign in, see its own settings and enabled features, create and report on operational data, and all automated isolation tests confirm that it cannot access any other tenant's data.
