Plan: Complete Tenant & Subscription Management (host surface)
Read-only. No files touched. Based on Domain/Tenants/*, Application/Features/Tenants/Provision|UpdateStatus|Reconciliation, WebUI/Endpoints/HostEndpoints.cs:30, Infrastructure/Database/ApplicationDbContextInitializer.cs:95, Client/features/host-admin/*, docs/host-admin-runbook.md:39.
0) Goal — project owner can self-serve from /host/admin/tenants
Today provision works only via raw POST /host/api/v1/tenants (HostEndpoints.cs:58 → ProvisionTenantCommand.cs:11) and status via PATCH .../status (HostEndpoints.cs:65 → UpdateTenantStatusCommand.cs:12). No UI for provision, no plan catalog, no renewal (TenantSubscription.cs:58 Renew() orphaned), no subscription visibility (TenantSummaryResponse:178 omits plan/period). Owner must use SQL/Swagger for plans/renewals (host-admin-runbook.md:115).
1) Clarifying questions (pick scope)
1. Plans: seed Standard unlimited only (ApplicationDbContextInitializer.cs:106). Need full plan CRUD (create/edit/disable plans with limits SubscriptionPlan.cs:12-18) or just list existing?
2. Provision fields: keep all 11 fields (ProvisionTenantCommandValidator.cs:9) + auto Active+Features.All (ProvisionTenantCommandHandler.cs:77), or allow choosing EnabledFeatures on create?
3. Subscription lifecycle beyond status: Renew (TenantSubscription.cs:58), change plan mid-term, PastDue/Expired grace (SubscriptionGate.cs:69 quota MaxUsers/MaxInvoices) — need UI for extend/change vs DB-only?
4. Billing integration: BillingProvider/Reference (TenantSubscription.cs:20) external gateway or manual dates only?
Default proposal = (1) list+create plans + (2) keep auto features + (3) renew/extend + change plan.
2) Architecture
Backend (host-only HostOnlyAttribute):
- Reuse /host/api/v1 group (HostEndpoints.cs:30). Keep HostAuthDefaults cookie GoldStore.HostAccessToken + AntiforgeryEndpointFilter (xsrf.interceptor.ts:12 → X-XSRF-TOKEN). Refresh interceptor already bypasses host (refresh.interceptor.ts:19 isHostRequest).
- Tenant global vs tenant-owned split (Tenancy/TenantHostOptions.cs:11, Domain/Tenants/Features.cs:8). Plans global (SubscriptionPlanConfiguration.cs:14 unique Key), subscriptions tenant-owned (TenantSubscriptionConfiguration.cs:18 FK restrict).
Frontend: extend features/host-admin/ (isolated HostApi base /host/api/v1 host-api.service.ts:118 never mixes tenant ApiClient).
3) Plan (tasks)
M1 — Backend catalog (no UI yet):
- GET /host/api/v1/subscription-plans → SubscriptionPlanResponse[] (Id,Name,Key,MaximumActiveUsers,MaximumPostedInvoicesPerPeriod,MaximumActiveBranches,MaximumStorageBytes,IsActive) from IApplicationDbContext.SubscriptionPlans (IApplicationDbContext.cs:70). Order by Name.
- POST /host/api/v1/subscription-plans (XSRF) → CreateSubscriptionPlanCommand(Name,Key,limits) validator mirrors SubscriptionPlan.Create:37 (name/key required, limits >=0 TenantErrors.cs:21).
- Extend GET /host/api/v1/tenants or add GET /host/api/v1/tenants/{id}/subscription → TenantSubscriptionResponse + SubscriptionPlan snapshot (expose StartsAtUtc/EndsAtUtc/BillingCycle/Status/PlanName/limits for host dashboard).
- POST /host/api/v1/tenants/{id}/subscription/renew → RenewTenantSubscriptionCommand(TenantId, StartsAtUtc, EndsAtUtc) calls TenantSubscription.Renew:58; alternative PATCH plan change reusing same.
M2 — Frontend transport: HostApi.listPlans/createPlan/getSubscription/renewSubscription/provisionTenant (HostApi:114 pattern encodeURIComponent, toQueryString:98, ApiRequestOptions SKIP_ERROR_TOAST), HostStore signals plans/plansLoading/plansError/subscription/subscriptionLoading/saving/saveError (host-store.ts:29 NO_TOAST, mutate:99 toast Arabic).
M3 — Host UI (host-admin-page.ts:39 OnPush):
- Keep two tabs, add third الاشتراكات or provision button over tenants tab. host-admin-page.ts:72 tabs → ProvisionTenantDialog (fields Name/Key slug+https://{key}.goldstore.app preview, TimeZoneId select, admin First/Last/Email/Password with EmailAddress + unique 409, Plan dropdown from listPlans, BillingCycle radio Monthly|Annual, StartsAtUtc/EndsAtUtc datetime-local → toISOString(); validation MaximumLength 200/63 regex ^[a-z0-9]([a-z0-9-]*[a-z0-9])?$ Validator.cs:11). Submit → POST /tenants with X-XSRF-TOKEN.
- Tenants table add planName/billingCycle/period  quota columns via new getSubscription per row (lazy) or expanded detail drawer (preferred: host table stays key/name/status, row → drawer with `GET