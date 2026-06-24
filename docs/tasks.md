u# GoldStore ERP — Milestones

## M1 — Foundation & Identity
- [ ] Clean Architecture scaffold (Domain, Application, Infrastructure, Web.Api)
- [ ] AppDbContext + PostgreSQL + EF migrations
- [ ] Identity entities: User, Role, Permission + many-to-many
- [ ] JWT auth: login, register, refresh-token, /me endpoint
- [ ] RBAC: permission-based `[Authorize]` policies
- [ ] Serilog + Seq logging
- [ ] Global exception handler → RFC 9457 ProblemDetails
- [ ] Angular scaffold: RTL sidebar, Tailwind, IBM Plex Sans Arabic, auth flow

## M2 — Configuration
- [ ] Karat entity (seed 18K, 21K, 24K — read-only)
- [ ] Category entity (self-referencing hierarchy, unlimited depth)
- [ ] GoldPrice entity (unique per date + currency)
- [ ] CRUD endpoints + FluentValidation
- [ ] Frontend: Karats list, Category tree, Gold Prices entry + history

## M3 — Suppliers & Inventory
- [ ] Supplier entity + CRUD
- [ ] SupplierDelivery header + items (Weight, KaratId, Equivalent21Weight auto-calc)
- [ ] SupplierPayment: scrap gold items + manufacturing payments
- [ ] Account entity (Cash/Bank, multi-currency) + CRUD
- [ ] GoldLedgerEntry: movement-based (IN: SupplierDelivery, CustomerPurchase / OUT: Sale, SupplierScrapPayment)
- [ ] Equivalent21Weight = Weight × (Karat / 21) — **always server-side**
- [ ] InventoryAdjustment header + items (Increase/Decrease/Damage/Loss/Correction)
- [ ] On delivery → GoldLedgerEntry IN; on scrap payment → OUT; on adjustment → IN/OUT
- [ ] Frontend: Suppliers CRUD, Delivery form, Payment form, Accounts, Inventory Adjustments, Gold Stock view

## M4 — Sales & Customer Gold Purchases
- [ ] SalesInvoice header + items (Category, Weight, Karat, GramPrice, TotalAmount)
- [ ] SalesInvoicePayment (Account, Method: Cash/BankTransfer, multi-currency)
- [ ] On invoice finalize → GoldLedgerEntry OUT + FinancialLedgerEntry IN
- [ ] CustomerPurchaseInvoice header + items
- [ ] CustomerPurchasePayment (source Account)
- [ ] On purchase finalize → GoldLedgerEntry IN + FinancialLedgerEntry OUT
- [ ] Invoice number auto-generation
- [ ] Frontend: Sales invoice list + form, Customer purchase list + form

## M5 — Finance, Expenses & HR
- [ ] ExpenseCategory (seed defaults: Rent, Electricity, Water, etc.)
- [ ] Expense entity → FinancialLedgerEntry OUT
- [ ] Employee entity (Name, Phone, Position, Salary)
- [ ] SalaryPayment → FinancialLedgerEntry OUT
- [ ] Account balance calculation: SUM(FinancialLedgerEntry IN − OUT) per account/currency
- [ ] Supplier gold balance: delivered gold IN − scrap gold OUT (21K equiv)
- [ ] Supplier manufacturing balance: fees owed − payments
- [ ] Frontend: Expense categories, Expense entry, Employees, Salary payments, Balance views

## M6 — Dashboard & Reporting
- [ ] Dashboard: live gold price, total gold stock (21K equiv), today's revenue/expenses
- [ ] Gold stock report: balance per karat + 21K total
- [ ] Financial summary: balance per account per currency
- [ ] Supplier balance report: gold + manufacturing balances
- [ ] Sales/Expense reports: date range, filters, totals
- [ ] PDF + Excel export per report
- [ ] Frontend: Dashboard KPIs, all report pages with export buttons

## M7 — Optimization & Production
- [ ] Database indexes per ERD spec
- [ ] HybridCache: gold prices, karats, account balances (Redis L2)
- [ ] Audit logging + IAuditInterceptor
- [ ] Account lockout (5 failed attempts, 15-min)
- [ ] Password reset flow
- [ ] Rate limiting on auth endpoints
- [ ] Dockerfile (SDK container publish) + docker-compose production
- [ ] Nginx: HTTPS termination, reverse proxy, Angular static files
- [ ] Health checks: DB + Redis connectivity
- [ ] pg_dump backup strategy
- [ ] Frontend: production build optimization, error handling, loading states, RTL QA pass