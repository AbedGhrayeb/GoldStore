# GoldStore ERP — Milestones

> Status refreshed after the Phase 5 tenant-awareness audit (2026-08). MVC + Razor + jQuery is the production client; the Angular client (`src/Client`) is deferred to Phase 7b.

## M1 — Foundation & Identity
- [x] Clean Architecture scaffold (Domain, Application, Infrastructure, WebUI)
- [x] AppDbContext + EF migrations (SQL Server; PostgreSQL migration planned)
- [x] Identity entities: User, Role, Permission + many-to-many
- [x] Auth: login, register, refresh-token, /me endpoint, tenant-host separation (JWT + cookies)
- [x] RBAC: permission-based authorization policies
- [x] Serilog + Seq logging
- [ ] Global exception handler → RFC 9457 ProblemDetails
- [ ] Angular scaffold (deferred to Phase 7b)

## M2 — Configuration
- [x] Karat entity (seed 18K, 21K, 24K — read-only, global)
- [x] Category entity (self-referencing hierarchy, tenant-scoped unique)
- [x] Gold prices via live external feed (`IGoldPriceService`) — replaced the planned stored GoldPrice entity
- [x] CRUD endpoints + FluentValidation
- [x] Frontend: Categories tree (MVC)

## M3 — Suppliers & Inventory
- [x] Supplier entity + CRUD (tenant-scoped unique name, backed by `(TenantId, Name)` index)
- [x] SupplierDelivery header + items (Weight, KaratId, Equivalent21Weight auto-calc)
- [x] SupplierPayment: scrap gold items + manufacturing payments
- [x] Account entity (Cash/Bank, multi-currency) + CRUD
- [x] GoldLedgerEntry: movement-based (IN: SupplierDelivery, CustomerPurchase / OUT: Sale, SupplierScrapPayment)
- [x] Equivalent21Weight — server-side in `Domain.Common.GoldWeight` (pinned by unit tests)
- [x] InventoryAdjustment header + items (Increase/Decrease/Damage/Loss/Correction)
- [x] On delivery → GoldLedgerEntry IN; on scrap payment → OUT; on adjustment → IN/OUT
- [x] Frontend: Suppliers CRUD, Delivery form, Payment form, Accounts, Inventory Adjustments, Gold Stock view

## M4 — Sales & Customer Gold Purchases
- [x] SalesInvoice header + items (Category, Weight, Karat, GramPrice, TotalAmount)
- [x] Payment legs (Account, Method: Cash/BankTransfer, multi-currency, exchange rate)
- [x] On invoice finalize → GoldLedgerEntry OUT + FinancialLedgerEntry IN (+ debt if unpaid)
- [x] CustomerPurchaseInvoice header + items
- [x] CustomerPurchasePayment (source Account)
- [x] On purchase finalize → GoldLedgerEntry IN + FinancialLedgerEntry OUT
- [x] Invoice number auto-generation (per tenant)
- [x] Frontend: Sales invoice list + form, Customer purchase list + form

## M5 — Finance, Expenses & HR
- [x] ExpenseCategory + Expense → FinancialLedgerEntry OUT
- [x] Employee entity (Name, Phone, Position, Salary) + user linking
- [x] SalaryPayment → FinancialLedgerEntry OUT
- [x] Account balance calculation: SUM(FinancialLedgerEntry IN − OUT) per account/currency
- [x] Supplier gold balance: delivered gold IN − scrap gold OUT (21K equiv)
- [x] Supplier manufacturing balance: fees owed − payments
- [x] Debts (Receivables/Payables): dual-write DebtLedgerEntry + financial transactions
- [x] Frontend: Expense categories, Expense entry, Employees, Salary payments, Balance views

## M6 — Dashboard & Reporting
- [x] Dashboard: live gold price, total gold stock (21K equiv), today's revenue/expenses (KPI pages)
- [x] Gold stock view: balance per karat + 21K total
- [x] Financial summary KPIs: balance per account per currency
- [x] Supplier balance view: gold + manufacturing balances
- [ ] Sales/Expense reports: date range, filters, totals
- [ ] PDF + Excel export per report
- [ ] Frontend: report pages with export buttons

## M7 — Optimization & Production
- [ ] Database indexes per ERD spec (tenant-aware indexes added in tenancy migration)
- [ ] HybridCache: gold prices, karats, account balances
- [ ] Audit logging + audit trail
- [ ] Account lockout (5 failed attempts, 15-min)
- [ ] Password reset flow
- [ ] Rate limiting on auth endpoints
- [ ] Dockerfile (SDK container publish) + docker-compose production
- [ ] Nginx: HTTPS termination, reverse proxy, static files
- [ ] Health checks: DB connectivity
- [ ] pg_dump backup strategy
- [ ] Frontend: error handling, loading states, RTL QA pass

## Multi-Tenancy (see docs/multi-tenancy-implementation-plan.md)
- [x] Phases 0–4, 6: policy, tenant model, request context, EF isolation, auth, data migration
- [x] Phase 8: tenant-isolation integration tests (23) + architecture tests (4)
- [x] Phase 5: tenant-aware application features (audited)
- [x] Phase 9: rollout docs (host-admin runbook, retention policy, rollout checklist, per-tenant monitoring, DB-per-tenant decision) + reconciliation baseline 2026-08-13
  - [ ] M7: prod config/secret hygiene, /health, ApplyMigrations wiring, alerting, trial/grace jobs, host audit trail, plan-change endpoint
- [ ] Phase 7a: minimal API layer (deferred)
- [ ] Phase 7b: Angular client (deferred)
