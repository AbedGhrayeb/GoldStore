# Tenant & Subscription — AI Agent Task List

> This file is the **executable task list** for an AI coding agent to implement
> missing Tenant + Subscription host capabilities end-to-end.
>
> **Plan source:** `docs/tenant-subscription-management-plan.md` (decisions D1-D7, API contracts, order)
> **Policy source:** `docs/tenancy-policy.md`, `docs/host-admin-runbook.md`
> **Run this file top-to-bottom.** Each task has `Files`, `Pattern to clone`, `Agent steps`, `Verify`.

**Stack rules (from `AGENTS.md` + `Directory.Build.props`):**
file-scoped namespaces, `internal sealed`, `is null` / `is not null`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `SonarAnalyzer`, no `TenantId` in any client payload, Host routes under `/host/api/v1` with `HostOnlyAttribute` + `AntiforgeryEndpointFilter`, Angular 22 standalone `OnPush` + signals + `ApiClient` + `SKIP_ERROR_TOAST`, RTL Arabic labels, `data-mono` for numbers, `app-*` prefix.

---

## 0) Pre-flight (agent does first, no code yet)

- [ ] Read `docs/host-admin-runbook.md`, `docs/tenancy-policy.md`, `Domain/Tenants/Tenant.cs`, `Domain/Tenants/TenantSubscription.cs`, `Domain/Tenants/SubscriptionPlan.cs`, `Domain/Tenants/TenantErrors.cs`, `Domain/Tenants/Features.cs:8`, `Application/Features/Tenants/Provision/ProvisionTenantCommand.cs` + `ProvisionTenantCommandHandler.cs:63-116` + `ProvisionTenantCommandValidator.cs`, `Application/Features/Tenants/UpdateStatus/UpdateTenantStatusCommand.cs:12` + `UpdateTenantStatusCommandHandler.cs:26-99`, `Application/Features/Tenants/Reconciliation/GetTenantReconciliationQueryHandler.cs` (host `IgnoreQueryFilters` pattern), `Infrastructure/Database/ApplicationDbContextInitializer.cs:95-110`, `Infrastructure/Subscriptions/SubscriptionGate.cs:16-105`, `WebUI/Endpoints/HostEndpoints.cs:30-78`, `src/Client/src/app/features/host-admin/host-api.service.ts:1-118`, `host-store.ts:1-100`, `host-admin-page.ts:1-522`, `host-update-status-dialog.ts`, `core/http/api-client.service.ts`, `core/http/request.util.ts`, `shared/ui/*`, `proxy.conf.json`.
- [ ] `dotnet build GoldStore.slnx` must be green before touching code. Keep `src/WebUI` as Swagger/Scalar host — never delete MVC.

---

## Phase A — Backend: Subscription Plan Catalog

### A1 — `GET /host/api/v1/subscription-plans` (list)

- **Files to create:**
  `src/Application/Features/SubscriptionPlans/GetSubscriptionPlans/GetSubscriptionPlansQuery.cs`
  `src/Application/Features/SubscriptionPlans/GetSubscriptionPlans/GetSubscriptionPlansQueryHandler.cs`
  `src/Application/Features/SubscriptionPlans/SubscriptionPlanResponse.cs`
- **Pattern to clone:** `Application/Features/Tenants/Reconciliation/*` (global read) + `Application/Features/Catalog/*` response shape.
- **Agent steps:**
  1. Record `GetSubscriptionPlansQuery : IQuery<IReadOnlyList<SubscriptionPlanResponse>>` — no params.
  2. Response record `SubscriptionPlanResponse(Guid Id, string Name, string Key, int? MaximumActiveUsers, int? MaximumPostedInvoicesPerPeriod, int? MaximumActiveBranches, long? MaximumStorageBytes, bool IsActive)` — mirrors `Domain/Tenants/SubscriptionPlan.cs:12-18`.
  3. Handler `internal sealed` — inject `IApplicationDbContext`, query `db.SubscriptionPlans.AsNoTracking().OrderBy(p => p.Name)` — **no tenant filter** (global table, like `SubscriptionPlanConfiguration.cs:14` unique Key).
  4. Map to response via projection.
- **Verify:** `dotnet build` clean; handler returns seeded `Standard` plan.

### A2 — `POST /host/api/v1/subscription-plans` (create, XSRF)

- **Files to create:**
  `src/Application/Features/SubscriptionPlans/Create/CreateSubscriptionPlanCommand.cs`
  `src/Application/Features/SubscriptionPlans/Create/CreateSubscriptionPlanCommandHandler.cs`
  `src/Application/Features/SubscriptionPlans/Create/CreateSubscriptionPlanCommandValidator.cs`
- **Pattern to clone:** `ProvisionTenantCommand` validator + `SubscriptionPlan.Create:37` factory.
- **Agent steps:**
  1. `CreateSubscriptionPlanCommand(Name, Key, MaximumActiveUsers?, MaximumPostedInvoicesPerPeriod?, MaximumActiveBranches?, MaximumStorageBytes?) : ICommand<Guid>`. All limits `int?/long?` nullable = unlimited.
  2. Validator: `Name` required max 100, `Key` required max 50 regex `^[a-z0-9]+(?:-[a-z0-9]+)*$` (lowercase kebab, same rule as `ProvisionTenantCommandValidator`), limits `>=0` when not null. Reuse messages from `TenantErrors`.
  3. Handler: check unique `Key` (case-insensitive) — on conflict return `TenantErrors.KeyNotUnique`. Else `SubscriptionPlan.Create(key, name, maxUsers, maxInvoices, maxBranches, maxStorage)` (factory validates non-negative), `db.SubscriptionPlans.Add(plan)`, `SaveChanges(cancellationToken)`, return `plan.Id`.
  4. Handle `IsActive` default `true`.
- **Verify:** duplicate `Key` (different case) → `409` via `TenantProblemDetails` mapping; `dotnet test` still green.

### A3 — Wire catalog endpoints into `HostEndpoints`

- **File to edit:** `src/WebUI/Endpoints/HostEndpoints.cs`
- **Pattern to clone:** existing `Map // tenants:30`, `Map // auth:34`, `tenantGroup.MapPost 58`, `MapPatch 65`, `MapGet reconciliation 72` — same `RequireAuthorization()` + `.AddEndpointFilter<AntiforgeryEndpointFilter>()` for mutations.
- **Agent steps:**
  1. In `Map` add section `// subscription plans`:
     ```csharp
     var plansGroup = host.MapGroup("/subscription-plans");
     plansGroup.MapGet("/", async (ISender sender, CancellationToken ct) => { ... })
         .RequireAuthorization().Produces<SubscriptionPlanResponse[]>();
     plansGroup.MapPost("/", async (CreateSubscriptionPlanRequest req, ISender sender, CancellationToken ct) => { ... })
         .RequireAuthorization().AddEndpointFilter<AntiforgeryEndpointFilter>().Produces<CreatedResponse>();
     ```
  2. Define request/response records at bottom of file (same file as `TenantSummaryResponse:178`): `CreateSubscriptionPlanRequest`, reuse `SubscriptionPlanResponse` from Application or duplicate thin WebUI record.
  3. Commands go through `ISender` (MediatR), map `Result` → `TypedResults` via `ToProblemDetails` (see `TenantProblemDetails.cs:40` ForbiddenType pattern).
  4. Add `Produces` metadata so OpenAPI at `GET /openapi/v1.json` includes the paths (needed for `npm run gen:api`).
- **Verify:** `dotnet run --project src/WebUI --urls http://localhost:5999` → Scalar shows `GET /host/api/v1/subscription-plans` + `POST /host/api/v1/subscription-plans`; `curl` after `POST /host/api/v1/auth/login` with `X-XSRF-TOKEN` succeeds, without it `400`.

---

## Phase B — Backend: Subscription Read + Renew

### B1 — `GET /host/api/v1/tenants/{tenantId}/subscription` (host read)

- **Files to create:**
  `src/Application/Features/Subscriptions/GetTenantSubscription/GetTenantSubscriptionQuery.cs`
  `src/Application/Features/Subscriptions/GetTenantSubscription/GetTenantSubscriptionQueryHandler.cs`
  `src/Application/Features/Subscriptions/TenantSubscriptionResponse.cs`
- **Pattern to clone:** `UpdateTenantStatusCommandHandler.cs:88-99` (host lookup: `IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId)`) + `GetTenantReconciliationQueryHandler` (IgnoreQueryFilters).
- **Agent steps:**
  1. Query `GetTenantSubscriptionQuery(Guid TenantId) : IQuery<Result<TenantSubscriptionResponse>>`.
  2. Response `TenantSubscriptionResponse(Guid Id, Guid TenantId, string Status, string BillingCycle, DateTime StartsAtUtc, DateTime EndsAtUtc, string? BillingProvider, string? BillingProviderReference, SubscriptionPlanSnapshot Plan)` + `SubscriptionPlanSnapshot(Guid Id, string Name, string Key, int? MaximumActiveUsers, int? MaximumPostedInvoicesPerPeriod)`.
  3. Handler: `IgnoreQueryFilters` load `Tenant` + latest `TenantSubscription` for that tenant ordered by `StartsAtUtc` desc — if none → `TenantErrors.SubscriptionNotFound` (404). Join `SubscriptionPlans` to fill snapshot. Do not apply tenant filter (host is global scope).
  4. Return `Result.Success(response)`.
- **Verify:** unknown `tenantId` → 404 `Tenants.NotFound` (`TenantErrors.NotFound:31`); tenant with no subscription → 404 `SubscriptionNotFound`.

### B2 — `POST /host/api/v1/tenants/{tenantId}/subscription/renew` (extend / change plan, XSRF)

- **Files to create:**
  `src/Application/Features/Subscriptions/Renew/RenewTenantSubscriptionCommand.cs`
  `src/Application/Features/Subscriptions/Renew/RenewTenantSubscriptionCommandValidator.cs`
  `src/Application/Features/Subscriptions/Renew/RenewTenantSubscriptionCommandHandler.cs`
- **Pattern to clone:** `TenantSubscription.Renew:58` (`ends > starts` or Fail) + `UpdateTenantStatusCommandHandler:88-99` active/latest lookup + `TenantErrors.InvalidSubscriptionPeriod:35`.
- **Agent steps:**
  1. Command `RenewTenantSubscriptionCommand(Guid TenantId, Guid? NewPlanId, string BillingCycle, DateTime StartsAtUtc, DateTime EndsAtUtc) : ICommand<Guid>` — `BillingCycle` is `Monthly|Annual` (parse to `SubscriptionBillingCycle` enum).
  2. Validator: `StartsAtUtc` required, `EndsAtUtc` required `> StartsAtUtc` else `TenantErrors.InvalidSubscriptionPeriod`, `BillingCycle` must parse, `NewPlanId` when not null must be valid GUID.
  3. Handler:
     - `IgnoreQueryFilters` load tenant → 404 if missing.
     - Load latest subscription for tenant (same query as B1). If none, **create** new via `TenantSubscription.Create(tenantId, planId, billingCycle, starts, ends)`.
     - If `NewPlanId` provided, validate plan exists + `IsActive` → 404 `PlanNotFound:39` if not.
     - Else reuse existing `PlanId`.
     - If subscription exists: if plan changed, set `subscription.PlanId = newPlanId` (or recreate — pick one and keep consistent), then `subscription.Renew(starts, ends)` (keep `BillingCycle` update). If `Renew` returns failure, propagate.
     - `SaveChanges(ct)`, return `subscription.Id`.
  4. Note: do not touch `BillingProvider` fields (D6 deferred).
- **Verify:** `EndsAt <= StartsAt` → 400 validation with `InvalidSubscriptionPeriod`; `NewPlanId` unknown → 404; happy path updates period and optionally plan.

### B3 — Wire subscription endpoints into `HostEndpoints`

- **File to edit:** `src/WebUI/Endpoints/HostEndpoints.cs`
- **Agent steps:**
  1. Under same `host` group:
     ```csharp
     host.MapGet("/tenants/{tenantId:guid}/subscription", ...)
         .RequireAuthorization().Produces<TenantSubscriptionResponse>();
     host.MapPost("/tenants/{tenantId:guid}/subscription/renew", ...)
         .RequireAuthorization().AddEndpointFilter<AntiforgeryEndpointFilter>().Produces<RenewResponse>();
     ```
  2. Bottom records: `RenewSubscriptionRequest(Guid? NewPlanId, string BillingCycle, DateTime StartsAtUtc, DateTime EndsAtUtc)`.
  3. Regenerate OpenAPI: `npm run gen:api` from `src/Client` (script `openapi-typescript http://localhost:5999/openapi/v1.json -o src/app/shared/api/schema.d.ts` per `frontend-plan.md P1.1:368`). Commit `schema.d.ts` change. If generator not run, hand-write DTOs matching the request records exactly (keys `newPlanId`, `billingCycle`, `startsAtUtc`, `endsAtUtc`).
- **Verify:** `dotnet build GoldStore.slnx && dotnet test GoldStore.slnx` green; `GET /host/api/v1/tenants/{id}/subscription` returns plan snapshot; `POST .../renew` with `X-XSRF-TOKEN` rotates period.

---

## Phase C — Frontend: Transport + Store (no UI yet)

### C1 — `host-api.service.ts` (extend `HostApi`)

- **File to edit:** `src/Client/src/app/features/host-admin/host-api.service.ts`
- **Pattern to clone:** existing `HostApi:118` (`base '/host/api/v1'`, `ApiClient` injection, `encodeURIComponent`, `toQueryString:98`, `ApiRequestOptions { context: NO_TOAST }`, DTO hand-write or generated from `schema.d.ts`).
- **Agent steps:**
  1. Add DTOs: `SubscriptionPlanResponse`, `CreatePlanInput`, `TenantSubscriptionResponse`, `SubscriptionPlanSnapshot`, `RenewSubscriptionInput`.
  2. Add methods (all `withCredentials` inherited via `ApiClient`):
     ```ts
     listPlans(): Observable<SubscriptionPlanResponse[]>
     createPlan(input: CreatePlanInput): Observable<string>
     getSubscription(tenantId: string): Observable<TenantSubscriptionResponse>
     renewSubscription(tenantId: string, input: RenewSubscriptionInput): Observable<string>
     provisionTenant(input: ProvisionTenantInput): Observable<string>  // already exists? if not, add it — mirrors ProvisionTenantRequest
     ```
     Keys exactly `name`, `key`, `maximumActiveUsers`, etc. — never `tenantId` in body except URL.
  3. Keep `NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true)` for store-owned error surfacing.
- **Verify:** `npx tsc --noEmit` clean.

### C2 — `host-store.ts` (facade signals)

- **File to edit:** `src/Client/src/app/features/host-admin/host-store.ts`
- **Pattern to clone:** existing `HostStore:29` (`signal` + `computed`, `loading`/`error` pair per resource, private `mutate()` for POST/PATCH, `ToastStore` success toasts in Arabic, `NO_TOAST` context).
- **Agent steps:**
  1. Add signals: `plans = signal<SubscriptionPlanResponse[] | null>(null)`, `plansLoading`, `plansError`, `subscription`, `subscriptionLoading`, `subscriptionError`. Keep shared `saving`/`saveError`.
  2. Add `loadPlans()`, `createPlan(input)`, `loadSubscription(tenantId)`, `renewSubscription(tenantId, input)`, `provisionTenant(input)` — each sets loading/error, calls `HostApi`, on success updates signal + toast (`تم إنشاء خطة الاشتراك` / `تم تجديد الاشتراك بنجاح` / `تم تأسيس المتجر بنجاح`), on error sets `saveError`/`plansError` with `ApiError` from `toApiError`.
  3. `reloadAll()` for tenants+plans if needed.
- **Verify:** `npx tsc --noEmit` still clean.

---

## Phase D — Frontend: Host UI

### D1 — `provision-tenant-dialog.ts` (new)

- **File to create:** `src/Client/src/app/features/host-admin/provision-tenant-dialog.ts`
- **Pattern to clone:** `host-update-status-dialog.ts` (status 0..3 + `datetime-local` → `toISOString()`) + `features/catalog/*` form dialogs (ReactiveForms, `app-dialog`, `app-button`, inline 400/409 errors, `data-mono` preview).
- **Agent steps:**
  1. Standalone `OnPush` dialog with `Dialog` + `Card` + `Button`.
  2. Inputs: `open: input<boolean>`, `saving: input<boolean>` (from store), outputs `openChange`, `saved`, `saveError`.
  3. Form mirrors `ProvisionTenantRequest` 11 fields key-for-key: `name` (≤200), `key` slug regex `^[a-z0-9]([a-z0-9-]*[a-z0-9])?$` 3–63 live preview `https://{key}.goldstore.app`, `timeZoneId` select (shortlist: `Asia/Amman`, `Asia/Riyadh`, `Europe/Istanbul`, `UTC` — default `Asia/Amman`), `adminFirstName`, `adminLastName`, `adminEmail` (email), `adminPassword` (≥6), `subscriptionPlanId` dropdown from `store.plans()` (required), `billingCycle` radio `Monthly=0 / Annual=1`, `startsAtUtc` + `endsAtUtc` `datetime-local` → `toISOString()` (client check `ends > starts`, show خطأ `تاريخ الانتهاء يجب أن يكون بعد البداية`).
  4. Submit: build exact `ProvisionTenantInput` keys `name/key/timeZoneId/adminFirstName/...` and call `store.provisionTenant(input)`. On 409 `Tenants.Key.NotUnique` surface inline under key field; 400 validation under field; 409 duplicate email under email field.
  5. On success emit `saved`, close, clear form.
  6. RTL labels in Arabic, Tailwind `@theme` tokens, `app-*` primitives.

### D2 — Tenants tab: provision entry point

- **File to edit:** `src/Client/src/app/features/host-admin/host-admin-page.ts`
- **Pattern to clone:** existing `P3.13` page (`HostAdminPage:39` single page two tabs `المستأجرون`/`المطابقة`, lazy-load set `loadedTabs`, `HostStore` computed rows, `Table` + `Badge` + `RetryButton`).
- **Agent steps:**
  1. Header of المستأجرون card: add `app-button` `مستأجر جديد` with `plus` icon, `icon="plus"` next to count, opens `provision-tenant-dialog`.
  2. Wire `statusDialogOpen` + `provisionDialogOpen` + `provisionSaveError` from `store.saveError`.
  3. On provision `saved`: `store.loadTenants()` + close dialog + toast already from store.

### D3 — Subscription details drawer + renew dialog

- **Files to create:**
  `src/Client/src/app/features/host-admin/renew-subscription-dialog.ts` (new)
  `src/Client/src/app/features/host-admin/subscription-drawer.ts` (optional; or inline in `host-admin-page.ts`)
- **Pattern to clone:** `features/suppliers/*` payment dialogs + `host-update-status-dialog` period handling.
- **Agent steps:**
  1. Add row action تفاصيل الاشتراك in tenants `Table` actions cell (next to pencil). On click: `store.loadSubscription(tenant.id)` → open drawer.
  2. Drawer (or inline card under table) shows: status badge (`Active success / PastDue warning / Cancelled error / Expired neutral`), plan name + key + limits `data-mono` (مستخدمون/فواتير), `billingCycle` (شهري/سنوي), `startsAtUtc`/`endsAtUtc` formatted `ar-JO-u-nu-latn`, days-remaining chip (`متبقي N يوم` green / `منتهي منذ N` red).
  3. Button تجديد / تغيير الخطة opens `renew-subscription-dialog.ts`: optional `newPlanId` dropdown from `store.plans()`, `billingCycle` radio, `startsAtUtc`/`endsAtUtc` `datetime-local` → ISO. Submit → `store.renewSubscription(tenantId, input)` → on success reload subscription + `loadTenants()`.
  4. Loading: `subscriptionLoading` skeleton; error: `app-empty-state` with `retry`.

### D4 — Plans management card (inside الاشتراكات tab)

- **File to edit:** `src/Client/src/app/features/host-admin/host-admin-page.ts` (add third tab)
- **Agent steps:**
  1. Add third tab `الاشتراكات` (`tags` icon, key `plans`) to `tabs` array; lazy-load flag in `loadedTabs`.
  2. Inside it: plans table (`Table` columns: `name` / `key` `data-mono` / limits `maximumActiveUsers` `maximumPostedInvoicesPerPeriod` `data-mono` (`—` when null = غير محدود) / `isActive` badge). Provide `active` badge green/gray.
  3. Button خطة جديدة opens create-plan dialog (`name`, `key`, limits nullable). Limits inputs `number` min 0, empty = unlimited (null). Submit → `store.createPlan(input)` → `loadPlans()`.
  4. Keep toggle-active as stretch only if backend exposes it (D2 already — otherwise omit).

### D5 — Shell / routing / proxy

- **Files to check (no change expected):**
  `src/Client/src/app/app.routes.ts:22` `hostGuard` on `host/admin/tenants` (outside `AppShell`, host session only), `src/Client/proxy.conf.json` already forwards `/host` to `http://localhost:5999`, `src/Client/src/app/shared/api/schema.d.ts` regenerated.
- **Agent steps:** keep route outside `AppShell`; nav `إدارة المنصة` (`server` icon) stays `isHostAdmin`-only (`core/navigation/nav-items.ts:70 hostAdmin`). No new route needed.

### D6 — Specs (MSW, contract assertions)

- **Files to edit/create:**
  `src/Client/src/app/features/host-admin/host-store.spec.ts`
  `src/Client/src/app/features/host-admin/host-admin-page.spec.ts`
  optionally `provision-tenant-dialog.spec.ts`, `renew-subscription-dialog.spec.ts`
- **Pattern to clone:** existing `host-store.spec.ts` / `host-admin-page.spec.ts` — MSW handlers assert exact request keys, `tenantId` never in body, 400/409 inline errors, `params['id']` access, host `XSRF-TOKEN` → `X-XSRF-TOKEN` via `xsrf.interceptor.ts:12`.
- **Agent steps:**
  1. MSW: `POST /host/api/v1/subscription-plans` assert body has `name/key/maximumActiveUsers/...` (no extra), `POST /host/api/v1/tenants` assert 11 keys, `POST .../subscription/renew` assert `newPlanId/billingCycle/startsAtUtc/endsAtUtc`.
  2. Store specs: createPlan 409 surfaces `saveError.validation`, renew success updates `subscription`, provision success clears `saveError` + reloads tenants.
  3. Page specs: plans dropdown populated, drawer renders days-remaining math, renew dialog period validation blocks submit when `ends <= starts`.

---

## Verify (run in order after each phase)

1. **A+B backend:** `dotnet build GoldStore.slnx && dotnet test GoldStore.slnx` — 0 warnings (`TreatWarningsAsErrors`). `dotnet run --project src/WebUI --urls http://localhost:5999` → Scalar lists 4 new host paths; `curl` host login → capture `GoldStore.HostAccessToken` + `XSRF-TOKEN`; `POST /subscription-plans`, `GET /subscription-plans`, `POST /tenants` (provision), `GET /tenants/{id}/subscription`, `POST .../renew` all succeed with `X-XSRF-TOKEN`.
2. **C transport/store:** `npx tsc --noEmit` clean (strictTemplates).
3. **D UI:** `npx ng build --configuration development` emits updated `host-admin-page` chunk (no `**` redirect regression). `npx ng test --include '**/host-admin/**'` green. `npm run lint` clean.
4. **Manual E2E (full stack):** `dotnet run --project src/WebUI --urls http://localhost:5999` + `npm start` → `/host/login` with seeded platform user (`platform@goldstore.app`) → الاشتراكات → خطة جديدة → المستأجرون → مستأجر جديد (fills 11 fields) → login as new tenant admin → confirm 3 seed accounts (JOD/USD/ILS) + dashboard loads → back as host → تفاصيل الاشتراك → تجديد → confirm new period → PATCH status Cancelled → confirm subscription flips to Cancelled (server rule `UpdateTenantStatusCommandHandler:88-99`).

---

## Definition of done for this work

- [ ] `GET /host/api/v1/subscription-plans` + `POST /host/api/v1/subscription-plans` + `GET /host/api/v1/tenants/{id}/subscription` + `POST /host/api/v1/tenants/{id}/subscription/renew` all live, XSRF-protected, `Produces` + OpenAPI documented, `schema.d.ts` regenerated.
- [ ] Host UI: tenants tab has مستأجر جديد (provision dialog); الاشتراكات tab shows plans table + create-plan; row تفاصيل الاشتراك drawer with renew/change-plan; all via `HostApi`/`HostStore` with Arabic toasts, never sending `tenantId` in body.
- [ ] Tests: MSW store + page specs assert exact payload keys + 400/409 handling + dropdown population + days-remaining math, green.
- [ ] `dotnet build` + `dotnet test` + `npx tsc --noEmit` + `npx ng build` + `npm run lint` all green.
- [ ] No tenant/host cookie mixing (`host-api.service.ts` base stays `/host/api/v1`, tenant `ApiClient` stays `/api/v1`).

## Out of scope (do not build now)

- Feature toggling at provision (`Features.All` stays auto — `ProvisionTenantCommandHandler:77`).
- Billing provider fields (`BillingProvider/Reference` — D6).
- Expiry scheduler flipping `Expire()/MarkPastDue()` (`docs/m7-plan.md:84`).
- Per-tenant usage vs quota dashboard (`SubscriptionGate` counts stay reconciliation-only).

## File checklist for the agent (tick as you go)

- [ ] `src/Application/Features/SubscriptionPlans/*` (A1-A2)
- [ ] `src/Application/Features/Subscriptions/*` (B1-B2)
- [ ] `src/WebUI/Endpoints/HostEndpoints.cs` (A3 + B3)
- [ ] `src/Client/src/app/shared/api/schema.d.ts` (regen)
- [ ] `src/Client/src/app/features/host-admin/host-api.service.ts` (C1)
- [ ] `src/Client/src/app/features/host-admin/host-store.ts` (C2)
- [ ] `src/Client/src/app/features/host-admin/provision-tenant-dialog.ts` (D1)
- [ ] `src/Client/src/app/features/host-admin/renew-subscription-dialog.ts` (D3)
- [ ] `src/Client/src/app/features/host-admin/host-admin-page.ts` (D2 + D4)
- [ ] `src/Client/src/app/features/host-admin/*.spec.ts` (D6)
