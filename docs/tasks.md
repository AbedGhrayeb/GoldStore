# GoldStore ERP — Phase & Task Tracker

> Working doc for driving the remaining work. Each section is a phase with ordered,
> checkable tasks plus a **How:** block describing exactly how to implement and verify
> that task in this codebase. Completed work is listed as a compact reference so the
> active tasks stay in focus.
>
> **Status legend:** `[x]` done · `[ ]` pending · `[-]` partial (see note)
>
> **Convention references:** `AGENTS.md` (architecture, code rules), `docs/multi-tenancy-implementation-plan.md`
> (tenancy policy + non-negotiables), `docs/host-admin-runbook.md` (host API usage), `src/WebUI/Endpoints/*` (endpoint pattern).

---

## 0. How to run and verify

```bash
dotnet build GoldStore.slnx            # TreatWarningsAsErrors=true — must be 0 errors
dotnet test GoldStore.slnx             # Integration + Unit + Architecture suites
dotnet run --project src/WebUI --no-build --urls http://localhost:5999
```

- Integration tests use a real SQL Server (`Server=.;Database=GoldStoreDb`, Trusted) with a throwaway DB per factory instance — the DB must be reachable.
- Live smoke: scripts in `C:\Users\ABDULR~1\AppData\Local\Temp\opencode\verify-host-flow.ps1` (host flow) — extend this pattern for new routes.
- Platform admin (host): `platform@goldstore.app` / `GoldStoreHost@321!` (seeded only while `PlatformUsers` is empty).
- API routes: tenant `api/v1` (JWT bearer), host `host/api/v1` (PlatformUser cookie).

---

## 1. Completed reference

### M1 — Foundation & Identity ✅
Clean Architecture scaffold · EF migrations · Identity (User/Role/Permission) · login/register/refresh/me · RBAC permission policies · Serilog + Seq · **global exception handler → RFC 9457 ProblemDetails**.

### M2 — Configuration ✅
Karat (seeded global 18/21/24) · Category (tenant-scoped self-ref hierarchy) · live gold-price feed (`IGoldPriceService`, replaces stored entity) · CRUD + FluentValidation · MVC Categories tree.

### M3 — Suppliers & Inventory ✅
Supplier CRUD · SupplierDelivery (+21K equiv auto-calc) · scrap-gold + manufacturing payments · multi-currency accounts · GoldLedgerEntry (IN delivery/purchase, OUT sale/scrap) · InventoryAdjustment · gold stock view.

### M4 — Sales & Purchases ✅
SalesInvoice (items, payment legs, multi-currency, gold OUT + financial IN, debt on unpaid) · CustomerPurchaseInvoice (gold IN + financial OUT) · per-tenant invoice numbering · MVC forms.

### M5 — Finance, Expenses & HR ✅
ExpenseCategory/Expense → financial OUT · Employee + user linking · SalaryPayment → financial OUT · balances derived from ledgers (never stored) · supplier gold + manufacturing balances · Debts (Receivables/Payables dual-write).

### M6 — Dashboard & KPIs (reporting pending → §4)
Live price, gold stock per karat + 21K total, financial KPIs, supplier balances — all delivered as KPI queries + MVC pages. **Report pages + PDF/Excel export NOT started.**

### Multi-Tenancy — Phases 0–9 ✅
Shared-schema model · tenant lifecycle · EF global query filter + write-guard interceptor · JWT tenant claims · tenant-unique indexes · data migration + reconciliation baseline 2026-08-13 · isolation tests (30 integration, 4 architecture) · rollout docs. **Operational items (M7) pending → §5.**

### Phase 7a — API layer: A1–A8 ✅
- **A1** Result-returning `ICommandDispatcher`/`IQueryDispatcher` (scoped, exact closed-generic resolution).
- **A2** `IEndpoint` + `ApiRoutes` (`api/v1`, `host/api/v1`) + auto-discovery (`AddEndpoints`/`MapEndpoints`).
- **A3** `ApiResults` (Result<T> → RFC 9457 ProblemDetails, validation → model-state 400).
- **A4** Built-in OpenAPI (`Microsoft.AspNetCore.OpenApi` + `BearerSecuritySchemeTransformer`) + Scalar UI (Development) + CORS `AllowAngularApp`.
- **A5** `AuthEndpoints` (`/api/v1/auth` login/refresh/logout/me) + `HostEndpoints` (`/host/api/v1/auth` login GET page + POST, logout, tenants, status PATCH, reconciliation) — replaced `AuthController`/`HostController`; host cookie redirect paths fixed; docs updated. **All suites green; live-verified.**
- **A6** Module endpoint batches B1–B6 (users/categories/gold-prices/reference, suppliers/inventory, sales/purchases, finance/expenses/HR, dashboard store-operations) + 56-test isolation suite + OpenAPI review + live smoke. **72 tests green.**
- **A7** Tenant-isolation hardening audit — middleware order confirmed (`UseRouting → RateLimiter → Authentication → TenantResolution → Authorization`), scoped dispatch confirmed (no captured tenant; domain-event handlers scope from the event's own `TenantId`). No gap → no new tests.
- **A8** Final verification — build 0 errors, 72 tests pass, OpenAPI/Scalar live, host reconciliation healthy (`anomalies: []`), runbook + tasks + session updated. **Phase 7a complete.**

---

## 2. Phase 7a — API layer ✅ complete

> Pattern reference for future API work (7b client, host additions): one `IEndpoint` class per module → `MapGroup($"/{ApiRoutes.Tenant}/<module>")`
> → `.RequireAuthorization()` + `.RequireAuthorization("feature:<key>")` (feature keys from `Domain.Tenants.Features`,
> mirrors MVC `[RequireFeature]`) → `dispatcher.DispatchAsync<TCommand, TResult>(...)` → `ApiResults.From(result)`.
> Never accept a `TenantId` in body/route/query. Response DTOs = the Application response records, returned as-is.

### A6 — Module endpoint batches
> One batch = endpoints + per-batch isolation tests (`ApiIsolationTests.cs`) + live smoke. Do in order.

#### A6-B1 — Identity & Configuration
- [x] `UsersEndpoints.cs` — group `/api/v1/users`, feature `settings`
      **How:** `GET /users` → `GetUsersQuery` (`List<UserResponse>`); `GET /users/me` → `GetUserByIdQuery` (profile from `IUserContext`, no ID in route); `POST /users` → `CreateUserCommand` (return `ApiResults.Created`); `PUT /users/{id:guid}` → `UpdateUserCommand`; `DELETE /users/{id:guid}` → `DeleteUserCommand`. Request DTOs = thin records mirroring the command constructors (`CreateUserRequest(Email, FirstName, LastName, Password)`, `UpdateUserRequest(FirstName, LastName, Password?)`).
- [x] `CategoriesEndpoints.cs` — group `/api/v1/categories`, feature `catalog`
      **How:** `GET /categories` → `GetCategoriesQuery` (flat `List<CategoryResponse>`); `POST /categories` → `CreateCategoryCommand`; `PUT /categories/{id:guid}` → `UpdateCategoryCommand`; `POST /categories/{id:guid}/toggle-active` → `ToggleActiveCategoryCommand`. `ApiResults.From` on all.
- [x] `GoldPricesEndpoints.cs` — group `/api/v1/gold-prices`, auth only (no feature gate)
      **How:** `GET /gold-prices/current` → `GetGoldPricesQuery` → `GoldPricesResponse`.
- [x] `ReferenceEndpoints.cs` — group `/api/v1/reference`, auth only
      **How:** `GET /reference/karats` → map `Domain.Common.SupportedValues.Karats` to `{ value, label }` (karats are a hardcoded domain list, **not** a table); `GET /reference/currencies` → `SupportedValues.Currencies`.
- [x] `ApiIsolationTests.cs` (new) + `CreateJwtClientAsync(email, password)` factory helper
      **How:** helper POSTs `/api/v1/auth/login` and sets `Authorization: Bearer <token>` (factor from `AuthEligibilityTests`). Tests: (1) tenant A/B each list only their own categories+users (seed near-identical data); (2) **direct-ID attack** — A `PUT`/toggle's B's category id → 404, and A `DELETE`s B's user id → 404 (categories are deactivated, not deleted, so the delete path is exercised on users); (3) **write stamping** — POST body carries a bogus `tenantId`, row still lands on caller's tenant; (4) **tenant-unique** — duplicate category name in A → 409, same name in B → 201; (5) **feature gate** — tenant with `catalog` disabled → 403; (6) `GET /users/me` returns own profile.
- [x] Verify: `dotnet build GoldStore.slnx` (0 errors) → `dotnet test tests\Application.IntegrationTests` → live smoke of all 6 routes.

#### A6-B2 — Suppliers & Inventory
- [x] `SuppliersEndpoints.cs` — `/api/v1/suppliers`, feature `suppliers`: GET list (`GetSuppliersQuery`), GET `{id}` (`GetSupplierByIdQuery`), POST (`CreateSupplierCommand`), PUT `{id}` (`UpdateSupplierCommand`), POST `{id}/toggle-active`.
- [x] `SupplierDeliveriesEndpoints.cs` — `/api/v1/supplier-deliveries`, feature `suppliers`: POST (`CreateSupplierDeliveryCommand` — items carry Weight/KaratId, 21K calc is server-side; inspect the command signature before mirroring).
- [x] `SupplierPaymentsEndpoints.cs` — `/api/v1/supplier-payments/scrap-gold` + `/manufacturing`, feature `suppliers`: POST each (`CreateSupplierScrapGoldPaymentCommand`, `CreateSupplierManufacturingPaymentCommand`).
- [x] `SupplierFinancialTransactionsEndpoints.cs` — `/api/v1/supplier-financial-transactions`, feature `suppliers`: POST create, GET paged, GET `{transactionId}/payments`, POST `{transactionId}/payments`, GET `kpis`.
- [x] `InventoryEndpoints.cs` — `/api/v1/inventory`, feature `inventory`: adjustments POST + paged + kpis; gold ledger paged + trend + kpis (map `GetGoldLedgerQuery(page, pageSize, karat?, fromDate?, toDate?, referenceType?)` query-string params — **no TenantId**).
- [x] Tests: same five isolation shapes as B1 (cross-tenant ID attack on a supplier, write stamping on a delivery, unique supplier name per tenant, feature gate, list scoping). Also covers malformed collection payloads and persisted supplier-financial transaction IDs/currency handling; `dotnet test GoldStore.slnx --no-build` → 59 passed.

#### A6-B3 — Sales & Customer Purchases
- [x] `SalesInvoicesEndpoints.cs` — `/api/v1/sales-invoices`, feature `sales`: POST create (header + items + payment legs), GET paged/by-id, GET `next-number`, GET `kpis`.
- [x] `CustomerPurchaseInvoicesEndpoints.cs` — `/api/v1/customer-purchases/invoices`, feature `purchases`: POST create, GET `next-number`.
- [x] Tests: cross-tenant invoice ID attack; caller-tenant gold-ledger OUT on sale / IN on purchase; returned purchase ID consistency; partial-payment debt/account isolation; independent feature gates; malformed collections; tenant-local numbering; parallel create uniqueness via durable tenant/module/month sequences. `dotnet test GoldStore.slnx --no-build` → 66 passed.

#### A6-B4 — Finance, Expenses & HR
- [x] `FinanceAccountsEndpoints.cs` — `/api/v1/finance/accounts`, feature `finance`: GET list, GET `with-balances`, GET `{id}/balance`, POST create, PUT `{id}/balance` (`SetAccountBalanceCommand`).
- [x] `FinanceDebtsEndpoints.cs` — `/api/v1/finance/debts`, feature `finance`: GET paged, POST, PUT `{id}`, POST `{id}/payments`, GET `kpis`.
- [x] `FinanceTransactionsEndpoints.cs` — `/api/v1/finance/transactions`, feature `finance`: GET paged, GET `recent`.
- [x] `ExpensesEndpoints.cs` — `/api/v1/expenses` (+ `/expenses/categories`), feature `expenses`: category create/update/delete/list; expense create/update/delete/paged/kpis.
- [x] `EmployeesEndpoints.cs` — `/api/v1/employees`, feature `hr`: create/update/toggle/list/by-id/pay-salary/salary-payments/salary-period-summary/unlinked-users (one group, sub-resources at `/employees/salary-payments` etc.).
- [x] Tests: cross-tenant account/debt/employee ID attack → 404; balance scoping per tenant; feature gates (`finance`, `expenses`, `hr`); full expense+HR write/ledger workflow (expense → financial OUT, delete reversal, salary payment). `dotnet test GoldStore.slnx --no-build` → 70 passed.

#### A6-B5 — Dashboard (Store Operations)
- [x] `StoreOperationsEndpoints.cs` — `/api/v1/dashboard/store-operations`, auth only (KPIs are cross-module): GET `kpis`, GET `paged`, GET `{id}/detail`, GET `employees`, GET `today-employee-stats`. Each returns the existing query response records.
- [x] Tests: two tenants with deliberately similar data return their own KPIs; no cross-tenant leakage in detail lookups (direct-ID → 404).

#### A6-B6 — API layer completion
- [x] OpenAPI doc review: every new route present under `api/v1`, security applied, no `TenantId` in any parameter.
- [x] Full `dotnet test GoldStore.slnx` green + live smoke of a representative route per module.
- [x] Update this file (check A6 boxes) + `session.md`.

### A7 — Tenant isolation hardening (after A6)
- [x] Audit middleware order (`UseRouting → RateLimiter → Authentication → TenantResolution → Authorization`) — confirmed exactly in `Program.cs` (lines 107/111/114/118/120). No module endpoint bypasses it: every tenant `IEndpoint` group uses `.RequireAuthorization()` + feature gate; the only exemptions are the pre-auth anonymous login/refresh/logout routes (`AuthEndpoints`), the `[HostOnly]` host group (`HostEndpoints`, cross-tenant by design, all routes authorized), and the health probes via `Tenancy:ExemptPaths`. `[HostOnly]` appears nowhere else in the codebase.
- [x] Confirm dispatch scopes: `ICommandDispatcher`/`IQueryDispatcher` + all handlers are **scoped** and resolved from the request-scoped `IServiceProvider` — never a singleton/static. `CurrentTenant` is scoped (claims at query time or explicit `Set`, per-scope snapshot cache only). EF query filter reads `ICurrentTenant` at query time (deny-by-default: no tenant → matches nothing). `TenantEntityInterceptor` is the single write guard (stamps Added, rejects foreign/no tenant, TenantId immutable, guards Modified/Deleted). Domain-event handlers run in a fresh scope per event and scope tenant-owned work from the event's own `TenantId` via `ICurrentTenantSetter` (`UserRegisteredDomainEventHandler`) — no captured ambient tenant.
- [x] No gap found → no new tests; the isolation suite (56 integration tests) remains the gate.

### A8 — Final verification
- [x] Full test run + build; OpenAPI + Scalar live check; `GET /host/api/v1/reconciliation` healthy.
      **Verified (2026-08-15):** `dotnet build GoldStore.slnx` → 0 errors; `dotnet test GoldStore.slnx` → 72 passed (12 unit + 4 arch + 56 integration); live `/openapi/v1.json` → OpenAPI 3.1.1 with 59 tenant + 5 host paths and a Bearer scheme; `/scalar/v1` → 200 HTML; `/host/api/v1/reconciliation` → 200 with `anomalies: []`; health probes `/health`, `/health/ready` (DB check), `/health/live` all 200.
- [x] Update runbook + tasks.md + session.md; mark Phase 7a complete.

---

## 3. Phase 7b — Angular client (deferred; create `src/Client`)

> Standalone components, lazy feature routes, RTL sidebar on the right, Tailwind v4 tokens in
> `tailwind.config.js`, Vitest, proxy `src/proxy.conf.json` → `/api` → `http://localhost:5000`.
> API contract = the Phase 7a OpenAPI document. Auth via `/api/v1/auth/*`, tokens in memory/secure
> storage, `TenantId` never sent.

- [ ] Scaffold: `ng new` standalone + Tailwind v4 + proxy + Vitest setup (see `angular-new-app` skill).
- [ ] Auth flow: login/refresh/logout interceptors, route guards, claims-based nav.
- [ ] Shell: RTL layout, sidebar (right), tenant branding from `/reference/*` + settings.
- [ ] Module pages per feature (config, suppliers/inventory, sales/purchases, finance/HR, dashboard) over the A6 endpoints.
- [ ] Error handling + loading states; PWA (manifest, service worker) via Angular CLI.
- [ ] Keep MVC (`src/WebUI`) as fallback — do not delete.

---

## 4. M6 — Reporting (MVC first, then mirrored in 7b)

- [ ] Sales report: `GetSalesInvoicesQuery` with date-range + filters + totals in a query/report DTO.
- [ ] Expense report: `GetExpensesQuery` with date-range + category filter + totals.
- [ ] PDF export (server-side render → `ITagHelper`/view to PDF) + Excel export (ClosedXML or similar — add via `Directory.Packages.props`).
- [ ] MVC report pages with export buttons.
- [ ] (7b) Angular report pages calling the same query endpoints.

---

## 5. M7 — Optimization & Production

> Checked items below are already done — verify, then update this file.

- [x] Account lockout (5 failed attempts / 15 min) — `User`/`PlatformUser` `RecordFailedLoginAttempt` + `LockoutDuration`, migration, unit + integration tests.
- [x] Rate limiting on auth endpoints — `AddRateLimiter` + `LoginLimiter` policy on `/api/v1/auth/login|refresh` and `/host/api/v1/auth/login`.
- [-] Database indexes — tenant-aware `(TenantId, …)` unique indexes shipped in the tenancy migration; finish ERD-spec indexes as query patterns demand (see `docs/per-tenant-monitoring.md`).
- [ ] HybridCache: gold prices, karats, account balances.
- [ ] Audit logging + audit trail (host-action audit + write audit).
- [ ] Password reset flow (host resets via DB today; runbook §5).
- [ ] Dockerfile (SDK `PublishContainer`) + docker-compose production (PostgreSQL, Seq).
- [ ] Nginx: HTTPS termination, reverse proxy, static files (deferred by owner).
- [x] Health checks: DB connectivity + `/health` endpoint — `/health` (all checks, UI writer), `/health/ready` (DB via `DatabaseHealthCheck`, `ready` tag), `/health/live` (liveness); all tenant-exempt; live-verified 200.
- [ ] pg_dump backup strategy.
- [ ] Frontend error handling, loading states, RTL QA pass.
- [ ] Production hygiene: secrets → env vars (committed `appsettings.json` holds dev conn string, goldapi key, placeholder JWT secret; `sarhangold.runasp.net-WebDeploy.publishSettings` contains creds).

---

## 6. Definition of done for Phase 7

- [x] Phase 7a: every business module has a tenant-isolated `api/v1` endpoint; host endpoints stay on `host/api/v1`; isolation suite green; OpenAPI + Scalar live.
- [ ] Phase 7b: Angular client serves all stores RTL-first over the 7a API; MVC remains the fallback.
