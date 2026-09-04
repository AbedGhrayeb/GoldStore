# GoldStore Multi-Tenancy Policy

## Scope

GoldStore will use a shared database and shared schema. Tenant-owned records must have a required `TenantId`, and every normal customer user belongs to exactly one tenant through `User.TenantId`.

## Identity and Access

- Customer users use the existing `User` identity and must have a non-null `TenantId`.
- Normalized email addresses are unique across the system.
- Customer login is performed at `goldstore.app` with email and password; it does not require a tenant header or tenant key.
- On successful login, the API issues a JWT containing the user ID, tenant ID, and tenant key. The client redirects to `https://{tenant-key}.goldstore.app`.
- Tenant subdomains proxy API requests while preserving the request host. The API verifies that the authenticated token tenant and request hostname resolve to the same tenant.
- Host/platform administrators use a separate `PlatformUser` identity. They can use dedicated host endpoints only, must select a tenant explicitly for support operations, and all such operations are audited.

## Tenant Lifecycle

| Status | Access |
| --- | --- |
| `Pending` | No customer login or operational API access. Used while onboarding and awaiting activation. |
| `Trial` | Full access to enabled features within plan limits until the trial end date. |
| `Active` | Full access to enabled features within plan limits for the current paid subscription period. |
| `Cancelled` | Read-only access for `CancellationReadOnlyGracePeriodDays`, initially configured as 30 days. After the grace period, customer access is blocked while data remains retained. |

Status changes, plan changes, grace-period changes, and host support access must be recorded in an audit trail.

## Subscription and Plan Limits

Tenants may subscribe monthly or annually. Each plan grants full access to its enabled features and defines these limits:

- **Active users:** enabled customer users assigned to the tenant; disabled users do not count.
- **Invoices per billing period:** final/posted sales and customer-purchase invoices created in the current subscription period. Drafts do not count; voided or cancelled documents remain auditable and do not release quota.
- **Active branches:** enabled branches belonging to the tenant. This limit is stored now and enforced when the branch module is introduced.
- **Storage:** total bytes of tenant-owned uploaded files. This limit is stored now and enforced when file uploads are introduced.

Plans must not limit generic add or edit operations. Financial and gold-ledger corrections must remain possible through authorized, auditable workflows.

## Tenant Key and Domains

- Each tenant has an immutable, system-wide unique key used in subdomains, for example `al-noor-gold`.
- Tenant keys use lowercase ASCII letters, numbers, and hyphens; they begin and end with a letter or number.
- `goldstore.app` is the public login and onboarding domain.
- The canonical tenant URL is `https://{tenant-key}.goldstore.app`.
- Custom domains are a later feature and require a verified tenant-domain mapping before they are accepted for routing.

## Data Ownership

- **Global data:** seeded karats, configured permission definitions, and subscription plan definitions.
- **Tenant-owned data:** users, categories, suppliers, prices, inventory, gold ledgers, financial accounts and ledgers, debts, sales, purchases, employees, expenses, settings, files, reports, and audit records.
- **Host-only data:** tenants, subscriptions, platform users, tenant-domain mappings, and platform-level audit records.

## Phase 0 Completion Criteria

Phase 0 is complete when this policy is approved and the implementation work accepts the following boundaries:

1. Customer users have one tenant; platform administrators are separate identities.
2. Tenant routing is based on a validated JWT and canonical request hostname, not a client-controlled tenant header.
3. Tenant states, the 30-day cancellation read-only grace period, and retention behavior are defined.
4. Monthly and annual subscriptions enforce the agreed users, invoices, branches, and storage limits.
5. Every existing and future entity is classified as global, tenant-owned, or host-only before it is persisted.
