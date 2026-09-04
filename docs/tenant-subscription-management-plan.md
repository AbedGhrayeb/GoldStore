# Tenant & Subscription Management — Plan & Step-by-Step Tasks

> Authoritative plan for completing host-side tenant provisioning, subscription-plan
> management, and subscription lifecycle (renew/change-plan) across backend + Angular client.
>
> **Status:** `[ ]` pending · `[x]` done · `[-]` partial
>
> **Source of truth:** `docs/host-admin-runbook.md`, `docs/tenancy-policy.md`,
> `Domain/Tenants/*`, `Application/Features/Tenants/*`, `WebUI/Endpoints/HostEndpoints.cs`,
> `src/Client/src/app/features/host-admin/*`.

---

## 0. Current state (verified against code)

**What exists today:**

| Capability | Backend | Client UI |
|---|---|---|
| Host auth (`POST /host/api/v1/auth/login`, cookie `GoldStore.HostAccessToken` + `XSRF-TOKEN`) | `WebUI/Endpoints/HostEndpoints.cs:34` | `/host/login` → `features/auth/host-login-page.ts` |
| List tenants (`GET /host/api/v1/tenants`) | `HostEndpoints.cs:53` → `TenantSummaryResponse(Id,Key,Name,Status)` :178 | `host-admin-page.ts` tab المستأجرون |
| Change status (`PATCH /host/api/v1/tenants/{id}/status`) | `HostEndpoints.cs:65` → `UpdateTenantStatusCommand` (state machine in `UpdateTenantStatusCommandHandler.cs:26-81`; cancel auto-cancels active subscription :88-99) | `host-update-status-dialog.ts` |
| Reconciliation (`GET /host/api/v1/reconciliation?tenantId=`) | `HostEndpoints.cs:72` → ledger-recomputed balances | `host-admin-page.ts` tab المطابقة |
| Provision tenant (`POST /host/api/v1/tenants`) | `HostEndpoints.cs:58` → `ProvisionTenantCommandHandler.cs:63-116` atomic: Tenant(Active) + TenantSettings(All features) + TenantSubscription + admin User + UserRole(store_admin) + 3 seed cash accounts (JOD/USD/ILS) | **None** — raw API only |
| Plans catalog | Seed-only `Standard` unlimited (`ApplicationDbContextInitializer.cs:95-110`); **no list/create endpoint** | None |
| Subscription lifecycle | Domain methods orphaned: `TenantSubscription.Renew/MarkPastDue/Expire` (`TenantSubscription.cs:58-85`) — no Application command/endpoint. Quota gate enforced at runtime via `ISubscriptionGate` (`Infrastructure/Subscriptions/SubscriptionGate.cs:16-105`) | None |

**Gaps (why this plan exists):**
1. Owner cannot provision a tenant from the UI — must call the raw API with XSRF by hand
   (`docs/host-admin-runbook.md:115`: "plan edits require a DB operation or the future endpoint").
2. No way to see or create subscription plans; provisioning requires pasting a plan GUID.
3. Tenants table shows no plan/billing data (`TenantSummaryResponse` omits subscription fields).
4. No renew / change-plan flow — `Renew()` is dead domain code.

---

## 1. Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | Extend the existing `/host/api/v1` group (`HostOnlyAttribute`) — no new route namespace | Same cookie scheme, same `AntiforgeryEndpointFilter`, same client `HostApi` base (`host-api.service.ts:118`); zero new auth surface |
| D2 | Plan CRUD = **list + create + toggle-active** (no edit/delete) | Plans are global reference data with FK Restrict from `TenantSubscriptions` (`TenantSubscriptionConfiguration.cs:18`); delete would break history. Edit invites limit-gaming; disable covers retirement |
| D3 | Provision UI keeps all 11 required fields; features stay auto-enabled (`Features.All`) | Mirrors `ProvisionTenantCommandValidator.cs`; feature toggling belongs to a future settings task |
| D4 | Subscription visibility via **one new endpoint** `GET /host/api/v1/tenants/{id}/subscription` returning current subscription + plan snapshot | Avoids widening `GET /tenants` (N+1 join risk); drawer/tab fetches per selected row |
| D5 | Renewal = single command that **renews-or-changes plan in place**: `RenewTenantSubscriptionCommand(TenantId, NewPlanId?, StartsAtUtc, EndsAtUtc, BillingCycle)` calling domain `Renew()` (+ plan swap) on the active/latest subscription | One endpoint covers extend + upgrade/downgrade; `ends > starts` validated by `TenantErrors.InvalidSubscriptionPeriod` |
| D6 | No billing-provider integration now | `BillingProvider/BillingProviderReference` columns exist (`TenantSubscription.cs:20`) but no gateway is contracted; keep manual dates |
| D7 | Host UI stays one page: third tab الاشتراكات + provision dialog over tenants tab | Matches existing tabs pattern (`host-admin-page.ts` tabs + lazy-load set); avoids new routes/nav |

---

## 2. API contracts (new)

All under `/host/api/v1`, `RequireAuthorization()` (host cookie), mutations add
`AddEndpointFilter<AntiforgeryEndpointFilter>()`. Responses are plain records (7a style).

```
GET   /host/api/v1/subscription-plans
      → 200 SubscriptionPlanResponse[]
        { id, name, key, maximumActiveUsers?, maximumPostedInvoicesPerPeriod?,
          maximumActiveBranches?, maximumStorageBytes?, isActive }

POST  /host/api/v1/subscription-plans            [XSRF]
      body CreateSubscriptionPlanRequest { name, key, maximumActiveUsers?,
             maximumPostedInvoicesPerPeriod?, maximumActiveBranches?, maximumStorageBytes? }
      → 200 { planId }  |  400 validation  |  409 KeyNotUnique

GET   /host/api/v1/tenants/{tenantId}/subscription
      → 200 TenantSubscriptionResponse
        { id, status, billingCycle, startsAtUtc, endsAtUtc,
          billingProvider?, billingProviderReference?,
          plan: { id, name, key, maximumActiveUsers?, maximumPostedInvoicesPerPeriod? } }
      → 404 TenantNotFound / NoActiveSubscription (return latest by StartsAtUtc desc)

POST  /host/api/v1/tenants/{tenantId}/subscription/renew    [XSRF]
      body RenewSubscriptionRequest { newPlanId?, billingCycle, startsAtUtc, endsAtUtc }
      → 200 { subscriptionId }  |  400 InvalidSubscriptionPeriod  |  404 PlanNotFound/TenantNotFound
```

Backend work items:

- [ ] **T1** `GetSubscriptionPlansQuery` + handler (global read; order by Name).
- [ ] **T2** `CreateSubscriptionPlanCommand` + validator mirroring
      `SubscriptionPlan.Create` (`Domain/Tenants/SubscriptionPlan.cs:37`: non-negative limits,
      unique lowercase Key) + handler reusing `TenantErrors.KeyNotUnique`.
- [ ] **T3** `GetTenantSubscriptionQuery(TenantId)` — `IgnoreQueryFilters()` latest-by-start
      (host-sanctioned pattern per reconciliation handler), joins plan name.
- [ ] **T4** `RenewTenantSubscriptionCommand` — load active/latest subscription
      (`UpdateTenantStatusCommandHandler.cs:88-99` pattern), optional plan swap, call
      `subscription.Renew(start,end)`; if none exists, `Create` a new one (reactivation path).
- [ ] **T5** Wire endpoints into `HostEndpoints.Map` with `.Produces<>` metadata; regenerate
      OpenAPI (`schema.d.ts` regen via `npm run gen:api`).

---

## 3. Frontend work items (`src/Client/src/app/features/host-admin/`)

Follows existing patterns exactly: `ApiClient` transport, `SKIP_ERROR_TOAST` context,
signal facades, shared UI primitives, MSW specs asserting exact payload keys.

- [ ] **F1** `host-api.service.ts` — add DTOs + methods:
      `listPlans(): SubscriptionPlanResponse[]`, `createPlan(CreatePlanInput): string`,
      `getSubscription(tenantId): TenantSubscriptionResponse`,
      `renewSubscription(tenantId, RenewSubscriptionInput): string`.
      Keep `encodeURIComponent` + `toQueryString` conventions.
- [ ] **F2** `host-store.ts` — signals `plans/plansLoading/plansError`,
      `subscription/subscriptionLoading/subscriptionError`, reuse `saving/saveError` +
      private `mutate()` for create-plan/renew/provision; success toasts:
      «تم إنشاء خطة الاشتراك», «تم تجديد الاشتراك بنجاح», «تم تأسيس المتجر بنجاح».
- [ ] **F3** `provision-tenant-dialog.ts` — mirrors `ProvisionTenantRequest` key-for-key:
      store name (≤200), key slug (regex `^[a-z0-9]([a-z0-9-]*[a-z0-9])?$`, 3–63, live
      `https://{key}.goldstore.app` preview), timezone select (IANA shortlist, default
      `Asia/Amman`), admin first/last/email/password (≥6, email format; 409 surfaces inline),
      plan dropdown fed from `loadPlans()`, billing cycle radio Monthly|Annual, start/end
      datetime-local → ISO UTC (client check end > start). Success → toast + reload tenants.
- [ ] **F4** Tenants tab — header button مستأجر جديد opens F3 dialog.
- [ ] **F5** Subscription drawer/tab الاشتراكات — row action تفاصيل الاشتراك calls
      `getSubscription(id)`; shows status badge (Active/PastDue/Cancelled/Expired variants),
      plan name + user/invoice limits, billing cycle, period dates, days-remaining chip;
      button تجديد / تغيير الخطة opens `renew-subscription-dialog.ts` (optional new-plan
      dropdown + cycle + period pickers) → POST renew → reload subscription + tenants.
- [ ] **F6** Plans management card inside الاشتراكات tab — plans table (name, key, limits
      `data-mono`, active badge) + create-plan dialog (name/key/limits nullable = unlimited)
      + toggle-active when T2 exposes it (optional stretch).
- [ ] **F7** Route/guard unchanged (`app.routes.ts:22` `hostGuard`); proxy already forwards
      `/host` (`proxy.conf.json`). Regenerate `schema.d.ts` and switch hand-written DTOs to
      generated types where shapes match.
- [ ] **F8** Specs — extend `host-store.spec.ts` / `host-admin-page.spec.ts` (MSW):
      exact request keys for create-plan/renew/provision (never `tenantId` in body except URL),
      400/409 inline errors, plans dropdown population, drawer render, days-remaining math.

---

## 4. Task order & verify steps

1. **T1–T5 backend** → verify: `dotnet build GoldStore.slnx && dotnet test GoldStore.slnx`;
     Scalar shows new host paths; curl each endpoint with `X-XSRF-TOKEN` after host login.
2. **F1–F2 transport/store** → verify: `npx tsc --noEmit` clean.
3. **F3–F6 UI** → verify: `npx ng build --configuration development` emits updated
     `host-admin-page` chunk; unit specs green (`ng test --no-watch --include .../host-admin/**`).
4. **Manual E2E:** `dotnet run --project src/WebUI --urls http://localhost:5999` +
     `npm start` → `/host/login` (seeded platform user) → create plan → provision tenant →
     login as the new tenant admin → confirm 3 seed accounts + dashboard loads → back as
     host: open subscription drawer, renew with extended end date → confirm new period;
     PATCH status Cancelled → confirm subscription flips to Cancelled (server-side rule).

## 5. Out of scope (documented deferrals)

- Feature toggling per tenant at provision time (D3).
- Billing gateway integration / `BillingProvider` writes (D6).
- Scheduled expiry job flipping `Expire()/MarkPastDue()` (`docs/m7-plan.md:84`).
- Per-tenant usage dashboard vs quota (`SubscriptionGate`) — reconciliation counts only.
