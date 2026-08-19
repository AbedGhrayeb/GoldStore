# Phase 7b — Angular Client: Plan & Step-by-Step Tasks

> Authoritative working plan for building the production Angular client under `src/Client`.
> This document is both the **implementation plan** (task list with `How:` blocks, mirroring
> `docs/tasks.md` style) and the **agent brief** that an AI agent can execute verbatim.
>
> **Status:** `[ ]` pending · `[x]` done · `[-]` partial
>
> **Source of truth:** `docs/tasks.md §3`, `AGENTS.md` (frontend conventions), `docs/DESIGN.md`
> (tokens), the Phase 7a OpenAPI document at `GET /openapi/v1.json` (the API contract).

---

## 0. Senior review of Phase 7b (as scoped in tasks.md)

The tasks.md Phase 7b scope is correct but under-specified for production. It must be expanded
from 5 bullet points into a full engineering plan. Findings that shape this plan:

1. **API contract is ready and stable** — Phase 7a (A1–A8) is complete: 59 tenant paths under
   `api/v1` (JWT bearer), 5 host paths under `host/api/v1` (PlatformUser cookie). Responses are
   Application records returned as-is; requests are thin DTOs. **No request ever carries
   `TenantId`** — the client must never send it. Client types must be generated from
   `/openapi/v1.json` to stay in sync.
2. **Two audiences, two auth schemes.** Tenant users authenticate with `POST /api/v1/auth/login`
   → `{ accessToken, refreshToken, refreshExpiresAt }` (bearer, token rotation on `/refresh`,
   revocation on `/logout`). Platform admins authenticate on `/host/api/v1/auth/login` which sets a
   **cookie** (server-rendered GET login page exists in MVC, but the POST is SPA-callable with
   `credentials: 'include'` + XSRF header). The Angular app needs **two auth contexts**: tenant
   (primary) and host admin (secondary surface).
3. **Claims drive the UI.** `GET /api/v1/auth/me` returns `{ userId, email, tenantId, tenantKey,
   roles[], permissions[] }`. Feature availability maps 1:1 to the permission claims
   (`permission` claim, `CustomClaims.Permission`) that back the server's
   `RequireAuthorization("feature:<key>")` gates — `catalog, suppliers, inventory, finance, sales,
   purchases, hr, expenses, reports, settings` (`Domain.Tenants.Features`). **Nav must be
   permission-driven, not hard-coded.**
4. **Environment mismatch — pin versions.** `AGENTS.md`/`tasks.md` say "Angular 21", but the
   machine's `npx ng` is **Angular CLI 22.0.1** and npm resolves `@angular/core@22.1.2`. Pin the
   project to **Angular 22.x** and update AGENTS.md (`s/Angular 21/Angular 22/`). Node 24 LTS,
   npm 11. Build system is **esbuild** (`@angular/build:application`), dev server is Vite — no
   webpack.
5. **Tailwind v4 is CSS-first.** AGENTS.md mentions `tailwind.config.js`, but Tailwind v4 config
   lives in CSS via `@theme`. Map the DESIGN.md tokens (`#D4AF37` gold, `#FCFAFA` background,
   `IBM Plex Sans Arabic`, `data-mono` numerals) into an `@theme` block. A JS config is not
   needed.
6. **Testing tooling is fixed by repo convention: Vitest + Playwright.** The user brief said
   "Jest + Cypress/Playwright" — supersede with the repo's declared stack: **Vitest** (unit,
   via the `@angular/build:vitest` builder) + **Playwright** (e2e). Use MSW for API mocking.
7. **State management: Signals, not NgRx.** Complexity here is CRUD-over-REST plus KPIs — moderate.
   NgRx's actions/reducers/effects machinery is unjustified. Use **Angular signals** +
   `httpResource` for server state + **facade services** (typed, observable/signal-backed) that
   components consume. NgRx stays a documented fallback if cross-feature state explodes
   (reporting).
8. **Auth token storage: memory only** (per tasks.md) + silent single-flight refresh rotation.
   JWT never in `localStorage`/`sessionStorage` → kills the XSS-exfiltration vector. Host cookie
   flow gets Angular's built-in XSRF interceptor (XSRF-TOKEN cookie + `X-XSRF-TOKEN` header).
9. **PWA is real here** — a store counter needs the app to survive flaky connections. `ng add
   @angular/pwa` + `ngsw-config.json` with three-tier data caching (live gold price STW, reference
   cache-first, KPI network-first), offline shell, manifest + icons. Native background-sync is
   out of scope of the Angular service worker; note Workbox as a stretch for offline sales capture.
10. **Deployment target.** The backend deploys to `sarhangold.runasp.net` (Windows WebDeploy).
    The static Angular build should go to **S3 + CloudFront** (primary, enterprise cache-control)
    with **Netlify** as the fast alternative. GitHub Actions produces the PWA build and uploads
    artifacts; cache headers are part of the infra config, not the build.
11. **Keep MVC.** `src/WebUI` stays as the fallback and as the Swagger/Scalar host. Do not delete
    it; do not route-own it from Angular.

---

## 1. Decisions (ADR summary)

| # | Decision | Rationale |
|---|----------|-----------|
| ADR-1 | Angular 22.x, TypeScript 5.8+, standalone components, **Strict + AOT + esbuild** | matches installed CLI (22.0.1); esbuild is the Angular default since v17; strict/AOT are defaults and required by CSP/SRI hygiene |
| ADR-2 | **Signals + `httpResource` + facade services**; no NgRx | moderate CRUD complexity; facades keep components dumb and testable; NgRx documented fallback |
| ADR-3 | Tokens **in memory** only; refresh rotation via single-flight interceptor | XSS cannot read memory; rotation limits replay |
| ADR-4 | **Two auth contexts** (tenant bearer, host cookie) behind one `AuthStore` | single auth service, two transport strategies; host is a thin admin surface |
| ADR-5 | Permission-driven nav from `/auth/me` claims | mirrors server `feature:*` policies; no hard-coded menu |
| ADR-6 | **Vitest** (unit) + **Playwright** (e2e) + MSW (mocks) | repo convention overrides the generic Jest/Cypress brief |
| ADR-7 | Types generated from `/openapi/v1.json` (`openapi-typescript`) | keeps the client in lockstep with Phase 7a contracts |
| ADR-8 | Tailwind v4 CSS-first `@theme` tokens from DESIGN.md | v4 removed the JS config; keeps tokens in one place |
| ADR-9 | PWA via `@angular/pwa` + custom `ngsw-config.json` (3-tier dataGroups) | store-first UX on flaky connections |
| ADR-10 | Static deploy to S3+CloudFront (primary) / Netlify (alt) via GitHub Actions | immutable hashed assets + `ngsw` cache rules; backend stays on runasp.net |
| ADR-11 | Sentry + GA + Lighthouse CI gates | error/UX stability KPIs per monitoring brief |
| ADR-12 | Multi-repo (Nx) **deferred** | single-app monolith; add Nx only if a second app (host admin SPA) materializes as a separate build |

---

## 2. Project structure (`src/Client`)

```
src/Client/
  angular.json
  package.json
  public/                        # ngsw-worker.js, manifest.webmanifest, icons/, favicon
  proxy.conf.json                # /api → http://localhost:5000 (dev)
  src/
    app/
      core/                      # singletons, loaded once
        auth/                    #   AuthStore, token storage, AuthApi
        http/                    #   api client, interceptors (bearer, refresh, error, xsrf)
        guards/                  #   authGuard, featureGuard (permission claim)
        navigation/              #   NavItem model + factory from /me claims
        config/                  #   AppConfig, environment bridging
      shared/                    # pure, reusable, dumb
        ui/                      #   card, button, table (sticky/rtl/zebra), badge,
                                 #   dialog, toast, skeleton, empty-state, kpi-card
        formatters/              #   gold grams (21K equiv), currency (JOD/USD/ILS), dates
        pipes/                   #   dataMono (IBM Plex Sans Arabic), localized dates
        validators/              #   decimal, weight, currency
        api/                     #   generated schema.d.ts + typed endpoint service classes
      shell/                     # layout
        sidebar/  topbar/  tenant-banner/  router-outlet wrapper
      features/                  # ONE lazy route folder per feature gate (see §4)
        dashboard/
        catalog/
        gold-prices/
        suppliers/
        inventory/
        sales/
        purchases/
        finance/
        expenses/
        hr/
        host-admin/              # cookie-auth admin surface (small)
      app.routes.ts              # lazy route map (feature routes + guards)
      app.config.ts              # providers (httpClient, service-worker, sentry…)
      environment/               # environment.ts + .production.ts (fileReplacements)
    assets/fonts/                # self-hosted IBM Plex Sans Arabic (offline/PWA)
    styles/                      # tailwind.css (@theme tokens), base.css, rtl.css
  e2e/                           # Playwright (specs + playwright.config.ts)
```

---

## 3. Tech stack & build settings

- **Angular 22.1.x** (`@angular/core@~22.1`), **TypeScript 5.8**, Node 24 LTS, npm 11.
- `angular.json`: `builder: "@angular/build:application"` (esbuild + Vite dev server);
  `"optimization": true`, `"aot": true`, `"strict": true`, `"outputHashing": "all"`,
  `"budgets": [{ "type": "initial", "maximumWarning": "500kB", "maximumError": "1MB" },
                { "type": "anyComponentStyle", "maximumWarning": "4kB", "maximumError": "8kB" },
                { "type": "any", "maximumWarning": "1.5MB", "maximumError": "2MB" }]`.
- `tsconfig.json`: `"strict": true`, `"strictTemplates": true`, `"noImplicitOverride": true`,
  `"forceConsistentCasingInFileNames": true`, `"verbatimModuleSyntax": true`.
- Style: **Tailwind v4** via `@tailwindcss/postcss`; tokens in `styles/tailwind.css` `@theme`.
- RTL: `dir="rtl"` on `<html>`, `lang="ar"`, `[dir="ltr"]` isolated for numeric cells only.
- Fonts: self-hosted **IBM Plex Sans Arabic** (woff2) in `assets/fonts`; `data-mono` class
  (font-variant-numeric: tabular-nums) on every gram/currency number.
- Proxy (dev): `proxy.conf.json` → `/api/*` → `http://localhost:5000` (the 7a API default port;
  update to 5999 in the run script if the WebUI is launched there).

---

## 4. State management design

**Signals-first, facade pattern.** No NgRx unless cross-feature state grows (documented ADR-2).

- **Server state:** `httpResource<T>(() => url, { headers })` for GETs (caching, abort, refetch);
  mutations via plain `HttpClient` calls that push a `refresh$`/reload trigger signal.
- **Feature stores (facades):** `@Injectable({ providedIn: 'root' })` per feature, exposing
  `readonly kpis = signal<Kpis | undefined>(undefined)`, `readonly loading = signal(false)`,
  `readonly error = signal<ApiError | null>(null)`, plus `loadKpis()`, `create(dto)`, `refresh()`.
  Components consume only these facades — never `HttpClient` directly.
- **Auth store:** the only global store. Signals: `user = signal<MeResponse | null>(null)`,
  `permissions = computed(() => user()?.permissions ?? [])`,
  `isAuthenticated`, `isHostAdmin`; actions `login()`, `silentRefresh()`, `logout()`.
- **Why not NgRx:** every screen is "fetch list/KPIs → CRUD → refetch". Typed actions/selectors/
  effects add indirection without payoff; they also slow the Vitest/Playwright loop. The facade +
  signal contract keeps the same testability (store = injectable seam, MSW mocks HTTP).
- **Testability:** facades are plain classes → unit-test with MSW (no component). Components are
  dumb → tested via signals + `changeDetection: ChangeDetectionStrategy.OnPush`.

---

## 5. Performance best practices

- `ChangeDetectionStrategy.OnPush` on **all** components; signals replace manual `markForCheck`.
- `@for` blocks with `track` (the modern `trackBy`); avoid `*ngFor`.
- `httpResource` gives request dedup + refetch strategies; add `SwrService` (stale-while-revalidate)
  for KPI/live-price tiles — show cached value immediately, refresh in background.
- Lazy loading: every `features/*` folder is a lazy route with `loadComponent`. Strategy:
  `PreloadAllModules` would defeat laziness — use **custom preload strategy** for the two next
  expected modules (dashboard + catalog) only.
- Responsive images: the app is data-dense, not image-dense — use inline SVG icons (no image
  requests), and `srcset`/`loading="lazy"` anywhere an asset is added.
- Packet reduction: reuse JSON responses (KPI tiles served from the same payload the list page
  already loaded where the API allows), gzip/brotli on the CDN, minify + hashing on build.
- Budgets in §3 enforce the perf contract in CI.

---

## 6. PWA & service worker

- Bootstrap with `ng add @angular/pwa` (installs `@angular/service-worker`, manifest, icons,
  default `ngsw-config.json`).
- `ngsw-config.json`:
  - `index` + `assetGroups`:
    - `app-shell` (prefetch, `installMode: 'prefetch'`): `index.html`, fonts, manifest, icons,
      core JS/CSS.
    - `app-code` (`installMode: 'lazy'`): hashed bundles.
  - `dataGroups` (the critical bit for this domain):
    - `gold-prices`: `POST/GET /api/v1/gold-prices/*` → `staleWhileRevalidate`, `maxAge: 5m`,
      `timeout: 2s`.
    - `reference`: `/api/v1/reference/*` → `cacheFirst`, `maxAge: 24h`.
    - `kpis`: `/api/v1/*/kpis`, `/api/v1/dashboard/*` → `networkFirst`, `maxAge: 10m`,
      `timeout: 5s`, fallback to cache.
    - `auth`: `/api/v1/auth/*` → **never cached** (`cacheConfig` omitted → no dataGroup).
  - `navigationUrls`: allow `/**` so any deep route serves the offline shell; add explicit
    `exclude: ['/api/**']`.
- Offline behavior: app shell + cached KPIs render; a "أنت غير متصل" (offline) banner via
  `SwUpdate`/`navigator.onLine`; writes disabled with a toast.
- Background sync: **not** provided by `@angular/service-worker`. Stretch task: Workbox
  `BackgroundSyncPlugin` on `POST` mutations to queue offline sales — explicitly deferred
  (ADR-9); do not block Phase 7b on it.
- Manifest: `name: "Gold Store"`, `dir: "rtl"`, `lang: "ar"`, `theme_color: "#D4AF37"`,
  `background_color: "#FCFAFA"`, 192/512 icons + maskable.

---

## 7. Development & testing environment

- **Lint/format:** `@angular-eslint` flat config (from `ng add @angular-eslint/schematics`) +
  Prettier. `npm run lint`, `npm run format:check`.
- **Husky + lint-staged:** pre-commit runs eslint + prettier + vitest on changed files.
- **Unit:** Vitest via `@angular/build:vitest` builder (`ng test --no-watch` in CI). MSW
  (`msw`) intercepts `/api` in test to serve fixtures — one `handlers.ts` per feature.
- **E2E:** Playwright (`@playwright/test`). Web server config starts the WebUI
  (`dotnet run --project src/WebUI --urls http://localhost:5999`) behind the Angular dev server
  so e2e hits the real API. Login spec uses seeded creds
  (`platform@goldstore.app` is host-only; use a seeded tenant user).
- **Coverage gate:** unit ≥ 80% lines/branches on `core/` and `shared/`; ≥ 60% on features;
  e2e covers the golden path (login → dashboard → create sale → gold ledger).
- **Mock modules:** MSW handlers per feature + a `FeatureGateway` abstraction so a feature can
  run headless in Storybook-style dev without the API.

---

## 8. CI/CD (GitHub Actions)

`.github/workflows/client.yml`:
1. `setup-node` (24), `npm ci`.
2. `npm run lint`, `npm run format:check`.
3. `npx playwright install --with-deps` → `ng e2e` (starts WebUI on 5999).
4. `ng test --no-watch --browsers=ChromeHeadless --code-coverage` (Vitest runner).
5. `ng build --configuration production` (AOT, SW enabled, budgets enforced).
6. **Lighthouse CI** (lighthouseci.com action) on the deployed preview URL; assert
   performance ≥ 90, accessibility ≥ 95, PWA ≥ 100.
7. **Deploy** to S3 + CloudFront (aws-actions, `s3 sync --delete dist/`) with cache headers:
   - `/assets/*`, `/media/*`, `*.js|css|woff2` (hashed): `Cache-Control: public, max-age=31536000, immutable`.
   - `/ngsw-worker.js`, `/ngsw.json`, `/index.html`, `/manifest.webmanifest`: `no-cache, no-store, must-revalidate`.
   - S3 object metadata + CloudFront `Managed-CachingOptimized` behavior override for the two rules above.
   - Alternative (one-line swap): Netlify deploy with `_headers`/`netlify.toml` equivalents.
8. Tag + notify (Slack/Discord) on failure.

---

## 9. Security & privacy

- **CSP** (production, via `index.html` meta or CloudFront headers):
  `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; font-src 'self';
  img-src 'self' data:; connect-src 'self' https://sentry.io https://www.google-analytics.com;
  object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'`.
  Dev CSP loosened only via a separate `environment` (never in the same build).
- **SRI:** Angular does not emit `integrity` by default. Add a post-build step
  (script or `@ngx-builders` transform) that hashes `dist/**/*.js|css` and injects
  `integrity="sha384-…"` + `crossorigin="anonymous"` into `index.html`. Verify in CI before deploy.
- **XSS:** Angular's default sanitization everywhere; no `bypassSecurityTrust*` without a
  security review; no `innerHTML` with interpolated user data; escape supplier/customer names in
  reports. CSP `script-src 'self'` (no unsafe-inline/eval) blocks the top vector.
- **CSRF:** JWT tenant flow is bearer — no CSRF surface. Host cookie flow uses Angular's built-in
  `withXsrfConfiguration()` (`XSRF-TOKEN` cookie + `X-XSRF-TOKEN` header) against
  `/host/api/v1/*`.
- **Token storage:** access + refresh tokens live in a memory-only service (`AuthStore`); app
  refresh (F5) uses `/refresh` with the in-memory token only while the tab lives — document that
  a page reload requires re-login (or an optional session cookie flag if the product wants it).
  Never `localStorage`/`sessionStorage`.
- **Variable isolation:** `environment.ts` (dev) vs `environment.production.ts` via
  `fileReplacements`; `API_BASE_URL`, `SENTRY_DSN`, `GA_ID`, `SITE_URL` only. Secrets never
  committed; deployment secrets via GitHub Actions secrets / environment variables.

---

## 10. Monitoring & measurement

- **Sentry:** `@sentry/angular` — `ErrorHandler`, `SentryErrorHandler`, `traceServices`,
  `withSentryConfig`; breadcrumbs on API calls; release = git SHA; source maps uploaded in CI.
- **GA:** `@angular/common`'s no direct GA — use the `ga` snippet guarded by `environment.gaId`,
  or `@analytics/google-analytics`. Track page views (Router events) + key events
  (invoice created, export run).
- **KPIs:** error rate (Sentry), JS load time (Navigation Timing), LCP/CLS/INP (Lighthouse CI),
  SW cache hit ratio. Set alarms: error rate > 1%, Lighthouse perf < 90 = fail CI.
- **Lighthouse CI** in the pipeline (§8.6) doubles as the perf/stability gate.

---

## 11. Delivery outputs & documentation

- `src/Client/README.md` — prerequisites, `npm install`, `npm start` (proxy to 5999/5000),
  `npm run build`, `npm test`, `npm run e2e`, deploy commands, the two runbooks (tenant user,
  host admin), troubleshooting.
- `docs/architecture-decisions/` — ADR-1..12 above as individual files.
- **Upgrade strategy:** Angular majors twice a year — adopt the current LTS cycle; CI runs
  `ng update` in a maintenance branch per major; the SW enables in-place app updates
  (`SwUpdate.checkForUpdate()` on idle + banner). TypeScript/Node pinned in `.nvmrc` + engines.
- **Owner notes:** keep `src/WebUI` (MVC) as fallback; run both `dotnet run` and `ng serve`
  during transition; the proxy keeps `/api` pointing at the backend.

---

## 12. Step-by-step task list

> Ordered; each block is executable independently and ends with a verify step. Feature tasks
> follow the API groups in the same order as Phase 7a A6-B1…B6 so the contract work stays fresh.

### P0 — Scaffold & toolchain

- [ ] **P0.1** `ng new client --directory src/Client --style css --ssr false --standalone
      true --routing true --strict true --skip-git` (Angular 22, esbuild builder).
      **How:** run from repo root; verify `src/Client/angular.json` uses
      `@angular/build:application`; add `budgets` from §3; then remove the default demo
      component/pages. Pin deps to `~22.1` in `package.json`; add `.nvmrc` (`24`).
- [ ] **P0.2** Tailwind v4 + DESIGN.md tokens.
      **How:** `npm i -D tailwindcss @tailwindcss/postcss`; `postcss.config.mjs` with the plugin;
      `styles/tailwind.css` with `@import "tailwindcss"; @theme { --color-gold: #D4AF37;
      --color-gold-container: #FFE088; --color-surface: #FCFAFA; --color-card: #FFFFFF;
      --color-error: #EF4444; --color-success: #10B981; --font-sans: "IBM Plex Sans Arabic", …;
      --radius-input: 4px; --radius-modal: 8px; }`; wire the styles array in `angular.json`;
      `assets/fonts/` self-hosted woff2 + `@font-face` in `styles/base.css`; `.data-mono`
      (tabular-nums) class. Verify: `npm start` renders gold on `#D4AF37`.
- [ ] **P0.3** `proxy.conf.json` → `{ "/api": { "target": "http://localhost:5999",
      "secure": false, "changeOrigin": true, "logLevel": "debug" } }`; set `"serve": {
      "builder": "@angular/build:dev-server", "options": { "proxyConfig": "proxy.conf.json" } }`.
      Verify: `dotnet run --project src/WebUI --no-build --urls http://localhost:5999` then
      `curl http://localhost:4200/api/v1/reference/karats` (401 = reachable).
- [ ] **P0.4** ESLint (`ng add @angular-eslint/schematics`), Prettier + `.prettierrc`,
      Husky + lint-staged, `.editorconfig`. **How:** `npm i -D prettier husky lint-staged`;
      `npx husky init`; pre-commit runs `lint-staged` (eslint + prettier). Verify:
      `npm run lint` clean.
- [ ] **P0.5** Vitest unit runner + Playwright.
      **How:** in `angular.json` add `"test": { "builder": "@angular/build:vitest" }`;
      `npm i -D @playwright/test msw`; `playwright.config.ts` with `webServer` array
      (WebUI on 5999 + `ng serve`), baseURL `http://localhost:4200`. Verify: `ng test` runs a
      trivial spec; `npx playwright test` launches.
- [ ] **P0.6** Environment files: `src/app/environment/environment.ts` +
      `environment.production.ts` (stub `API_BASE_URL: ''` meaning same-origin in prod, dev
      `http://localhost:5999`), `fileReplacements` in `angular.json` production config.
      **How:** keep `API_BASE_URL` empty in production (CDN + API on same tenant subdomain per
      the CORS comment in `WebUI/DependencyInjection.cs`); dev uses absolute URL.
      Verify: `ng build --configuration production` has no `environment.production.ts` reference
      unresolved.

### P1 — API contract & core HTTP layer

- [x] **P1.1** Generate types from OpenAPI.
      **How:** `npm i -D openapi-typescript`; script `"gen:api": "openapi-typescript
      http://localhost:5999/openapi/v1.json -o src/app/shared/api/schema.d.ts"`; run it against
      the live 7a document. Add a `npm run gen:api:check` (regen + `git diff --exit-code`) in CI
      so contract drift fails the build. Verify: schema.d.ts contains `TokenResponse`,
      `SalesInvoiceResponse`, `PaginatedList`, etc.
- [x] **P1.2** Core `ApiClient` + interceptors.
      **How:** `core/http/api-client.service.ts` wrapping `HttpClient` with base URL + JSON
      defaults; `bearer.interceptor.ts` (attach in-memory access token); `refresh.interceptor.ts`
      (single-flight `/api/v1/auth/refresh` on 401, retry original, logout on failure);
      `error.interceptor.ts` (map ProblemDetails → typed `ApiError { status, title, detail,
      validation }` and emit to a global `ToastStore`); `xsrf.interceptor.ts` (host cookie
      flows only). Register via `provideHttpClient(withInterceptors([...]))` in `app.config.ts`.
- [x] **P1.3** `AuthStore` + `AuthApi`.
      **How:** memory token holder; signals `user`, `permissions`, `isAuthenticated`,
      `isHostAdmin`; `login(email, password)` → `/api/v1/auth/login` → store tokens in memory →
      `GET /auth/me` → set claims; `hostLogin(...)` → `/host/api/v1/auth/login` (cookie, XSRF);
      `logout()` → `/logout` + clear; `silentRefresh()` single-flight. **Never send
      `tenantId`.** Verify: unit test token lifecycle with MSW.
- [x] **P1.4** Guards.
      **How:** `authGuard` (redirect `/login` when not authenticated); `featureGuard('catalog'…)`
      reads `AuthStore.permissions()` for the **bare feature key** (claims carry `catalog`, not
      `feature:catalog` — verified against `TokenProvider.cs`) and redirects to a 403 page when
      absent; host section guarded by `isHostAdmin`. Apply on the lazy routes (§P3).
- [x] **P1.5** `navigation/` factory: `NavItem[] = dashboard, catalog, suppliers, inventory,
      sales, purchases, finance, expenses, hr, host-admin` filtered by permission claims +
      localized labels + icons. Verify: nav unit test with mocked claim sets (no `catalog` claim
      → item absent).

### P2 — Shell & shared UI

- [x] **P2.1** RTL shell: `dir="rtl"` html, sidebar on the **right**, topbar, `<router-outlet>`,
      `AppShellComponent`. Gold active indicator on the right edge of the active nav item;
      icons right of labels. Verify: screenshot at 1440px.
      **Note:** `@angular/build` v22 only accepts PostCSS via `postcss.config.json` (JS-based
      `postcss.config.js/.mjs` are silently ignored) — Tailwind v4 utilities were missing until
      converted. Also requires `@source "../app";` in `styles/tailwind.css`. Lucide icons via
      `lucide-angular@1.0.0` (`LucideAngularModule`, non-standalone).
- [x] **P2.2** Shared primitives (`shared/ui/`): `app-card`, `app-button`, `app-badge`,
      `app-table` (sticky header, zebra, gold hover, right-aligned numeric columns via
      `data-mono`), `app-dialog`, `app-toast`, `app-skeleton`, `app-empty-state`, `app-kpi-card`.
      All `OnPush` + standalone + `app-` prefix. Verify: unit tests for `app-table` sorting.
- [x] **P2.3** Formatters + validators: gold weight (grams, 21K-equivalent), currency
      (JOD/USD/ILS, 2–3 dp), localized dates (ar-JO style), decimal input validators, weight
      validators. Pipes for `data-mono`. Verify: formatter unit tests incl. 21K equivalence.
      **Note:** client 21K formula mirrors `Domain.Common.GoldWeight` exactly (24K ×1000/875,
      18K ×700/875, half-to-even to 3 dp); dates/currency use `ar-JO-u-nu-latn` (latin digits)
      with `data-mono`-friendly output.
- [x] **P2.4** Tenant branding: banner + settings placeholders fed from `/api/v1/auth/me`
      (`tenantKey`) + `/api/v1/reference/*` (currencies/karats for form selects). Live gold price
      chip in the topbar via the gold-prices service (§P3 gold-prices). Verify: login renders
      tenant brand.
      **Note:** `ReferenceStore` (karats/currencies, single-flight) + `GoldPriceStore`
      (`/api/v1/gold-prices/current`, 5-min auto-refresh, 21K-per-gram preferred over spot for
      the chip) in `core/`; `app-tenant-brand` is the reusable brand block (sidebar now, login
      in P3.1). Topbar chip shows `displayPrice` + up/down change, click-to-refresh.
- [x] **P2.5** Loading + error states globally: `ToastStore`, skeleton cards, retry buttons on
      `ApiError`, offline banner (SW). Verify: e2e kills network → banner + cached KPIs show.
      **Note:** `OfflineStore` (window online/offline events + `checkNow()` retry probe) +
      global `app-offline-banner` mounted in `app.ts`; `app-retry-button` primitive (secondary,
      `refresh-cw` icon, loading state). SW via `@angular/service-worker` + `ngsw-config.json`
      (app-shell + lazy-chunks prefetch, `api-cache` dataGroup Performance/1d for `/api/**`),
      registered with `registerImmediately`. Verified: unit tests (offline/online transitions,
      retry, banner render), dev-server CDP offline → banner appears/disappears, prod-build SW
      registers + serves the full app from cache on an offline reload.
      **Note (tests):** the unit-test builder runs **zoneless** (`NoopNgZone`) — plain field
      writes do NOT mark views dirty; test hosts must expose signal fields (`signal()` + `()` in
      templates) and mutate via `.set()`, or use intra-component signal flows (clicks), for
      post-render updates to propagate.

### P3 — Feature modules (lazy routes over the A6 endpoints)

> Each feature: `list` (paged table + filters + kpis) and `form` (create/edit) pages,
> backed by a typed facade service + MSW test handlers. Build order mirrors A6-B1…B5.

- [ ] **P3.1 Auth/Login pages** — `/login` (tenant) + `/host/login` (admin cookie). **How:**
      forms with `app-*` controls, `AuthStore.login/hostLogin`, error toast on 401; redirect to
      the first permitted route. Verify: e2e logs in with a seeded tenant user.
- [ ] **P3.2 Dashboard** (`feature: none`, auth only) — `/dashboard/store-operations`: KPI
      cards from `/kpis`, operations table from `/paged` (filters: date, operationType,
      employee, account, search), employee day stats from `/today-employee-stats`, detail drawer
      from `/{id}/detail?operationType=`. Verify: cross-module counts match an MVC page.
- [ ] **P3.3 Catalog** (`catalog`) — `/catalog/categories`: tree table (self-ref parent),
      create/edit/delete + toggle-active; reuse `/reference/karats` & `/currencies` selects.
      Verify: e2e create → toggle → delete.
- [ ] **P3.4 Gold prices** (auth only) — live price chip + prices page from
      `/gold-prices/current` with `httpResource` refetch (SW STW 5m). Verify: price tile updates
      without page reload.
- [ ] **P3.5 Users & settings** (`settings`) — `/settings/users`: list from `/users`, create
      (`/users`), edit (`/users/{id}`), delete (`/users/{id}`); profile from `/users/me`.
      Verify: cannot send `tenantId` in any payload (assert in MSW tests).
- [ ] **P3.6 Suppliers** (`suppliers`) — list/create/edit/toggle from `/suppliers`;
      deliveries create from `/supplier-deliveries` (server computes 21K — mirror the command
      shape only); scrap-gold & manufacturing payments from `/supplier-payments/*`;
      financial transactions list + create + payments + kpis from
      `/supplier-financial-transactions`. Verify: delivery posts with `weightInGrams`+`karat`
      only.
- [ ] **P3.7 Inventory** (`inventory`) — adjustments list/create + kpis from
      `/inventory/adjustments`; gold ledger paged (karat/from/to/referenceType filters) +
      trend chart + kpis from `/inventory/gold-ledger*`. Verify: ledger IN/OUT math in e2e.
- [ ] **P3.8 Sales** (`sales`) — invoice create (header + items + payment legs — mirror
      `CreateSalesInvoiceRequest`), list/paged, detail, `next-number` prefill, kpis from
      `/sales-invoices`. Verify: partial payment creates a debt and financial leg.
- [ ] **P3.9 Purchases** (`purchases`) — `/customer-purchases/invoices` create + next-number,
      list. Verify: gold IN + financial OUT appear on the ledger.
- [ ] **P3.10 Finance** (`finance`) — accounts list + with-balances + create + set balance
      from `/finance/accounts`; debts paged + create + pay from `/finance/debts`; transactions
      paged + recent from `/finance/transactions`. Verify: balance equals ledger sum.
- [ ] **P3.11 Expenses** (`expenses`) — categories CRUD + expenses CRUD + paged + kpis from
      `/expenses*`. Verify: delete reversal posts correct financial OUT.
- [ ] **P3.12 HR** (`hr`) — employees CRUD/toggle/list/by-id, pay-salary, salary-payments,
      salary-period-summary, unlinked-users from `/employees*`. Verify: salary payment hits
      the ledger.
- [ ] **P3.13 Host admin** (cookie auth) — `/host/admin/tenants` list/status PATCH from
      `/host/api/v1/tenants` + reconciliation view. **How:** guard by `isHostAdmin`; XSRF
      interceptor active; never mixed into tenant services. Verify: host login → tenants list.
- [ ] **P3.14 403 / 404 / offline pages** + route fallback. Verify: nav test clicks a
      permission-gated route → 403 page.

### P4 — PWA

- [ ] **P4.1** `ng add @angular/pwa`; icons (192/512 + maskable), manifest RTL/ar/gold,
      `theme_color`/`background_color` per DESIGN.md. Verify: Lighthouse PWA ≥ 100.
- [ ] **P4.2** Rewrite `ngsw-config.json` per §6 (assetGroups + 3 dataGroups + navigationUrls
      excluding `/api/**`). Verify: `ng build` emits `ngsw.json`; DevTools Application tab
      shows the groups.
- [ ] **P4.3** Update check + offline banner: `SwUpdate.checkForUpdate()` on idle + version
      toast ("تم تحديث التطبيق — أعد التحميل"). Verify: deployed build → reload detects update.
- [ ] **P4.4** Offline smoke: install → `navigator.onLine = false` (Playwright emulate) →
      reload deep route → shell + cached KPIs render, gold-price chip shows cached value,
      mutations show offline toast. Verify: e2e green offline.

### P5 — Testing & hardening

- [ ] **P5.1** MSW handlers per feature (fixtures from live API captures); facade unit tests
      (≥80% core/shared, ≥60% features); coverage gate wired into `package.json` scripts.
- [ ] **P5.2** Playwright golden path: tenant login → dashboard KPIs → create sale invoice →
      gold ledger shows OUT → inventory KPI changes; host login → tenants list. Verify: full e2e
      green against real WebUI on 5999.
- [ ] **P5.3** Security pass: no `localStorage` tokens (grep ban in lint), no
      `bypassSecurityTrust*` without review, CSP header present in prod build, SRI step (§9)
      run and verified, `TenantId` absent from every generated request body. Verify: run the
      ban-check script in CI.

### P6 — CI/CD, monitoring, docs

- [ ] **P6.1** `.github/workflows/client.yml` (§8): lint → format → unit (coverage) → e2e →
      production build (SW) → Lighthouse CI → deploy S3+CloudFront/Netlify with cache headers.
      Verify: PR branch run is green end-to-end.
- [ ] **P6.2** Sentry setup (`@sentry/angular`, error handler, release mapping, source-map
      upload) + GA events. Verify: test error appears in Sentry; page-view fires in GA debug.
- [ ] **P6.3** README + `docs/architecture-decisions/ADR-1..12.md` + update `AGENTS.md`
      (Angular 22, Tailwind v4 CSS-first, Vitest) + update `docs/tasks.md §3` with real
      completion. Verify: `npm run build` + `npm test` + `npm run e2e` documented and green.

---

## 13. Definition of done for Phase 7b

- [ ] `src/Client` builds production-ready (AOT, strict, budgets, SW) with **0 lint errors**.
- [ ] Tenant users authenticate via `/api/v1/auth/*` (in-memory tokens, silent refresh); host
      admins via `/host/api/v1/auth/*` (cookie + XSRF). No `TenantId` ever sent.
- [ ] All 10 feature gates have lazy, permission-driven RTL pages over the A6 endpoints
      (dashboard, catalog, gold-prices, suppliers, inventory, sales, purchases, finance,
      expenses, hr) + host-admin surface.
- [ ] PWA: offline shell + gold-price STW + reference cache-first + KPI network-first;
      Lighthouse performance ≥ 90, accessibility ≥ 95, PWA = 100.
- [ ] Vitest unit suite (coverage gates) + Playwright golden path green in CI; Lighthouse CI
      gates deploys.
- [ ] README + ADRs written; `AGENTS.md` and `docs/tasks.md §3` updated; MVC (`src/WebUI`)
      untouched and still runnable.