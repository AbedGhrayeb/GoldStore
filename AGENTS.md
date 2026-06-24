# AGENTS.md — GoldStore ERP

## Project Overview

Gold Store ERP — a web-based gold jewelry store management platform. Modular monolith built on Clean Architecture. Single branch, single company. RTL-first Arabic UI.

## Architecture

```
src/SharedKernel   (no dependencies — Result, Error, Entity, IDomainEvent)
src/Domain         → SharedKernel
src/Application    → Domain, SharedKernel
src/Infrastructure → Application
src/WebUI          → Infrastructure
src/Client         (Angular 21, standalone, Tailwind CSS)
tests/ArchitectureTests
```

- **Backend**: ASP.NET Core 10, C# (file-scoped namespaces), EF Core, SQL Server (moving to PostgreSQL), JWT auth, Serilog + Seq
- **Frontend**: Angular 21 (standalone components, lazy-loaded routes), SCSS, Tailwind CSS v3, RtL layout, `IBM Plex Sans Arabic` font
- **Clean Architecture** with CQRS: `ICommandHandler<>`, `IQueryHandler<,>`, `IDomainEventHandler<>`, validation via FluentValidation decorator, logging decorator
- **Endpoints**: `IEndpoint` interface — each feature maps endpoints via `MapEndpoint()`, registered via `AddEndpoints(Assembly)` / `MapEndpoints()`
- **Domain events**: dispatched after `SaveChangesAsync` (eventual consistency)

### Key Domain Modules (ERD-aligned)

| Module | Domain Folder | Key Entities |
|--------|-------------|--------------|
| Identity | `Users/` | User, Role, Permission |
| Configuration | `Catalog/` | Karat, Category (self-ref), GoldPrice |
| Suppliers | `Suppliers/`, `SupplierOperations/` | Supplier, SupplierDelivery, SupplierPayment |
| Inventory | `Inventory/` | GoldLedgerEntry, InventoryAdjustment |
| Finance | `Finance/` | FinancialAccount, FinancialTransaction |
| Sales | (M4 — upcoming) | SalesInvoice, SalesInvoiceItem |
| Purchases | (M4 — upcoming) | CustomerPurchaseInvoice |
| HR | (M5 — upcoming) | Employee, SalaryPayment |
| Expenses | (M5 — upcoming) | Expense, ExpenseCategory |

### Critical Business Rules

- **Equivalent21Weight** = Weight × (Karat / 21) — always calculated server-side, never trusted from client
- **Balances are never stored directly** — calculated from ledger entries (GoldLedgerEntry IN − OUT, FinancialLedgerEntry IN − OUT)
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
- `InternalsVisibleTo`: `Application.UnitTests` from Application, `ArchitectureTests` from Infrastructure
- Unit/integration test projects for Application layer are expected but not yet created

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
- **M2** (Configuration — Karats, Categories, GoldPrices): ✓ complete
- **M3** (Suppliers & Inventory): in progress — entities exist, CRUD not yet
- **M3.5** (Debts — Receivables/Payables): ✓ complete — dual-write ledger + financial transactions
- **M4** (Sales Invoices): ✓ complete — Create with per-item gold entries, payment/debt
- **M5–M7**: not started