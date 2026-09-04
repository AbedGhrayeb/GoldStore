# AGENTS.md — GoldStore ERP

## Project Overview

Gold Store ERP — a web-based gold jewelry store management platform. Modular monolith built on Clean Architecture. Single branch, single company. RTL-first Arabic UI.

## Architecture

```
src/SharedKernel   (no dependencies — Result, Error, Entity, IDomainEvent)
src/Domain         → SharedKernel
src/Application    → Domain, SharedKernel
src/Infrastructure → Application
src/WebUI          → Infrastructure   (MVC + Razor + jQuery — the production client)
tests/ArchitectureTests
tests/Application.UnitTests
tests/Application.IntegrationTests
```

- **Backend**: ASP.NET Core 10, C# (file-scoped namespaces), EF Core, SQL Server (moving to PostgreSQL), JWT + cookie auth, Serilog + Seq
- **Frontend (current)**: server-rendered MVC Razor views + jQuery/Bootstrap in `src/WebUI`, RTL layout, `IBM Plex Sans Arabic` font
- **Frontend (planned)**: Angular 21 client under `src/Client` is **Phase 7b (deferred)** — not created yet; `npm`/`src/proxy.conf.json` references in the Build section are for that future client
- **Clean Architecture** with CQRS: `ICommandHandler<>`, `IQueryHandler<,>`, `IDomainEventHandler<>`, validation via FluentValidation decorator, logging decorator
- **Endpoints**: MVC controllers in `WebUI.Controllers` (the API layer with `IEndpoint` minimal endpoints is Phase 7a, deferred)
- **Domain events**: dispatched after `SaveChangesAsync` (eventual consistency); events carry `TenantId` so handlers in fresh scopes stay tenant-aware

### Key Domain Modules (ERD-aligned)

| Module | Domain Folder | Key Entities |
|--------|-------------|--------------|
| Identity | `Users/` | User, Role, Permission |
| Configuration | `Catalog/` | Karat (global, read-only), Category (self-ref) — gold price is a **live external feed** (`IGoldPriceService`), not a tenant-owned entity |
| Suppliers | `Suppliers/`, `SupplierOperations/` | Supplier, SupplierDelivery, SupplierScrapGoldPayment, SupplierManufacturingPayment, SupplierGoldLedgerEntry, SupplierManufacturingLedgerEntry |
| Inventory | `Inventory/` | GoldLedgerEntry, InventoryAdjustment |
| Finance | `Finance/` | FinancialAccount, FinancialTransaction, Debt, DebtLedgerEntry |
| Sales | `Sales/` | SalesInvoice, SalesInvoiceItem |
| Purchases | `CustomerPurchases/` | CustomerPurchaseInvoice, CustomerPurchaseInvoiceItem |
| HR | `Employees/` | Employee, SalaryPayment |
| Expenses | `Expenses/` | Expense, ExpenseCategory |
| Tenancy | `Tenants/` | Tenant, TenantSettings, TenantSubscription, SubscriptionPlan, PlatformUser |

### Critical Business Rules

- **Equivalent21Weight** — calculated server-side in `Domain.Common.GoldWeight.CalculateEquivalent21KWeight`, never trusted from client (pinned by `tests/Application.UnitTests`):
  - 21K → Weight (unchanged)
  - 24K → Weight × 1000 / 875 (= Weight × 24/21)
  - 18K → Weight × 700 / 875 (= × 0.8) — **store convention**, deliberately NOT Weight × 18/21
- **Balances are never stored directly** — calculated from ledger entries (GoldLedgerEntry IN − OUT, FinancialLedgerEntry IN − OUT, DebtLedgerEntry IN − OUT)
- **Currencies**: JOD, USD, ILS
- **Karat values**: 18K, 21K, 24K (seeded, read-only)

## Build & Run

```bash
# Backend
dotnet build GoldStore.slnx
dotnet test GoldStore.slnx
dotnet run --project src/WebUI

# Frontend
cd src/Client
npm install
npm start          # ng serve (proxies /api → localhost:5000)
npm run build      # production build
npm test           # vitest
```

### Docker (full stack)

```bash
docker-compose up
# API: localhost:5000 (HTTP), localhost:5001 (HTTPS)
# PostgreSQL: localhost:5432 (user: postgres / postgres, db: clean-architecture)
# Seq: localhost:8081
```

### Database Migrations

```bash
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/WebUI
dotnet ef database update --project src/Infrastructure --startup-project src/WebUI
```

Migrations auto-apply in Development via `ApplyMigrations()` + `InitializeDatabaseAsync()`.

## Code Conventions (enforced by .editorconfig + analyzers)

- **TreatWarningsAsErrors**: `true` — all warnings break the build
- **EnforceCodeStyleInBuild**: `true`
- **SonarAnalyzer.CSharp** is globally included via `Directory.Build.props`
- File-scoped namespaces (`csharp_style_namespace_declarations = file_scoped:error`)
- `var` only when type is apparent; explicit typing otherwise
- Types: `internal` + `sealed` by default
- GUID identifiers preferred
- `is null` / `is not null` instead of `== null` / `!= null`
- Primary constructors for DI
- Central package management: `Directory.Packages.props`
- Controllers available, but **prefer minimal API endpoints** (`IEndpoint`)
- Records for immutable DTOs/value objects

## Frontend Conventions

- Angular 21 standalone components with lazy-loaded feature routes
- RTL: sidebar on **right**, icons right of text labels, gold active indicator on right edge
- Design tokens in `tailwind.config.js` (gold palette, spacing, shadows, radii)
- Proxy config: `src/proxy.conf.json` → `/api` proxies to `http://localhost:5000`
- Testing: **Vitest** (not Karma/Jasmine)
- Component prefix: `app-`

## Testing

- **ArchitectureTests**: NetArchTest.Rules + Shouldly + xUnit — validates layer dependency rules
- **Application.UnitTests**: xUnit — pure unit tests (currently pins the `Equivalent21KWeight` formula)
- **Application.IntegrationTests**: xUnit + `WebApplicationFactory` — real SQL Server-backed tenant-isolation tests (throwaway DB per factory instance)
- `InternalsVisibleTo`: `Application.UnitTests` from Application

## Style / Design System (from DESIGN.md)

- Primary: Gold `#D4AF37`, Primary container: `#FFE088`
- Background: warm off-white `#FCFAFA`, Cards: pure `#FFFFFF`
- Error: `#EF4444`, Success: `#10B981`
- Font: **IBM Plex Sans Arabic** — `data-mono` style for numerical data (grams, currency)
- RTL throughout — the entire ERP is Arabic-first
- Cards: 24px padding, 4px border-radius for inputs, 8px for modals
- DataTables: sticky headers, zebra rows, gold tint on hover, right-aligned numbers

## Current Progress (from docs/tasks.md)

- **M1** (Foundation & Identity): ✓ complete
- **M2** (Configuration — Karats, Categories, Gold prices): ✓ complete (gold price is a live external feed, not a stored entity)
- **M3** (Suppliers & Inventory): ✓ complete — suppliers, deliveries, scrap-gold & manufacturing payments, financial transactions, gold ledger
- **M3.5** (Debts — Receivables/Payables): ✓ complete — dual-write ledger + financial transactions
- **M4** (Sales Invoices): ✓ complete — Create with per-item gold entries, payment/debt
- **M4.5** (Customer Gold Purchases): ✓ complete — purchase invoices with gold IN + financial OUT
- **M5** (Finance, Expenses & HR): ✓ complete — accounts, debts, transactions, expenses, employees, salary payments
- **M6** (Dashboard & Reporting): partial — KPI pages exist (store operations, gold ledger, debts, expenses); report pages with PDF/Excel export **not started**
- **M7** (Optimization & Production): not started