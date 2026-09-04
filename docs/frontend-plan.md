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
2. **Two audiences, two auth schemes — both JWT-in-cookie.** Tenant users authenticate with
   `POST /api/v1/auth/login`, which returns `{ accessToken, refreshToken, refreshExpiresAt }`
   **and** sets `GoldStore.AccessToken` + `GoldStore.RefreshToken` HttpOnly cookies (path-scoped,
   `SameSite=Strict`). Platform admins authenticate on `POST /host/api/v1/auth/login`, which sets
   a `GoldStore.HostAccessToken` HttpOnly cookie and the `XSRF-TOKEN` request-token cookie; host
   mutations additionally validate the `X-XSRF-TOKEN` header. **The Angular client never touches a
   token** — it logs in via the API, lets the browser hold the HttpOnly cookies, and reads claims
   from `/auth/me`. Bearer-header credentials still work for external API clients (the handler
   prefers the Authorization header). The Angular app has **two auth contexts**: tenant (primary)
   and host admin (secondary surface), both cookie-backed.
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
8. **Auth tokens: HttpOnly cookies only.** Both login APIs (`/api/v1/auth/login`,
   `/host/api/v1/auth/login`) issue JWTs into hardened HttpOnly cookies; the client never stores
   or attaches a token (kills the XSS-exfiltration vector) and restores sessions via `/auth/me`
   on reload, with silent single-flight refresh-cookie rotation on 401. The host cookie flow
   additionally validates Angular's `X-XSRF-TOKEN` header (server sets the `XSRF-TOKEN` cookie on
   host login).
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
| ADR-3 | **JWTs live in HttpOnly cookies** (tenant access+refresh, host access); the client never receives or stores a token — memory included | cookie transport kills the XSS-exfiltration vector and the refresh-cookie rotation is silent + single-flight; bearer header still supported for API clients |
| ADR-4 | **Two auth contexts** (tenant cookie, host cookie) behind one `AuthStore` | single auth service, two transport strategies; host is a thin admin surface with its own XSRF guard |
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
        auth/                    #   AuthStore, AuthApi (cookie transport, never raw tokens)
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
- **Auth store:** the only global store. Both login APIs issue JWTs into HttpOnly cookies; the
  store never sees a token. Signals: `user = signal<MeResponse | null>(null)`,
  `permissions = computed(() => user()?.permissions ?? [])`,
  `isAuthenticated`, `isHostAdmin`; actions `login()`, `hostLogin()`, `restoreSessions()`
  (re-reads `/me` + `/host/me` on reload), `silentRefresh()` (single-flight refresh-cookie
  rotation), `logout()`.
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
- **CSRF:** tenant flow authenticates via HttpOnly `SameSite=Strict` cookies — no cross-site
  requests can attach them, so no CSRF surface. Host flow (admin surface) adds defense in depth:
  the server sets a readable `XSRF-TOKEN` cookie on host login and validates `X-XSRF-TOKEN` on
  host mutations (`AntiforgeryEndpointFilter`); Angular's `xsrfInterceptor` attaches the header
  automatically.
- **Token storage:** JWTs live **only** in HttpOnly cookies (tenant access+refresh, host
  access). The client never receives, stores, or attaches a raw token; app refresh (F5) calls
  `/auth/me` (tenant) and `/host/auth/me` (host) to restore the session, and 401s rotate the
  refresh cookie silently. Never `localStorage`/`sessionStorage`, and never a client-side token
  variable.
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
      defaults (all requests `withCredentials: true` so HttpOnly cookies flow);
      `refresh.interceptor.ts` (single-flight `/api/v1/auth/refresh` on tenant 401 — the browser
      sends the refresh cookie automatically — retry original, logout on failure);
      `error.interceptor.ts` (map ProblemDetails → typed `ApiError { status, title, detail,
      validation }` and emit to a global `ToastStore`); `xsrf.interceptor.ts` (host cookie flows
      only: `XSRF-TOKEN` cookie → `X-XSRF-TOKEN` header, validated server-side). Register via
      `provideHttpClient(withInterceptors([error, refresh, xsrf]))` in `app.config.ts`. There is
      **no bearer interceptor** — no token ever exists in the client.
- [x] **P1.3** `AuthStore` + `AuthApi`.
      **How:** cookie transport only; signals `user`, `permissions`, `isAuthenticated`,
      `isHostAdmin`; `login(email, password)` → `/api/v1/auth/login` (server sets HttpOnly JWT
      cookies) → `GET /auth/me` → set claims; `hostLogin(...)` → `/host/api/v1/auth/login`
      (JWT cookie + XSRF token cookie); `restoreSessions()` re-reads `/me` + `/host/me`;
      `logout()` → `/logout` + clear; `silentRefresh()` single-flight (refresh cookie rotates).
      **Never send `tenantId`.** Verify: unit test the cookie lifecycle with MSW.
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

- [x] **P3.1 Auth/Login pages** — `/login` (tenant) + `/host/login` (admin cookie). **How:**
      forms with `app-*` controls, `AuthStore.login/hostLogin`, error toast on 401; redirect to
      the first permitted route (from `buildNavItems(permissions)`; dashboard is always first).
      **Note:** both logins are pure API calls — the server issues JWTs into HttpOnly cookies
      (`GoldStore.AccessToken`/`.RefreshToken` for tenants, `GoldStore.HostAccessToken` for
      admins); the client stores nothing. Host login also receives the `XSRF-TOKEN` cookie, and
      host mutations send it back as `X-XSRF-TOKEN` (validated by `AntiforgeryEndpointFilter`).
      Verify: e2e logs in with a seeded tenant user and a host admin.
- [x] **P3.2 Dashboard** (`feature: none`, auth only) — `/dashboard/store-operations`: KPI
      cards from `/kpis`, operations table from `/paged` (filters: date, operationType,
      employee, account, search), employee day stats from `/today-employee-stats`, detail drawer
      from `/{id}/detail?operationType=`. Verify: cross-module counts match an MVC page.
      **Note:** `/employees` returns `{ id, name }` (StoreOperations DTO) while OpenAPI pins the
      HR-shaped `EmployeeResponse` schema — the client pins the real shape via a local
      `DashboardEmployeeOption` type. Account filter options are derived from the accounts on the
      current page rows (no account-list endpoint exists in the dashboard group); the server-side
      `accountId` filter is still sent. `app-table` gained an optional `cellTemplate` per column
      (badges/actions rendered inline while keeping the column's sort value).
- [x] **P3.3 Catalog** (`catalog`) — `/catalog/categories`: tree table (self-ref parent),
      create/edit/delete + toggle-active; reuse `/reference/karats` & `/currencies` selects.
      Verify: e2e create → toggle → delete.
      **Note:** the plan required delete, but the backend only had create/edit/toggle — added
      `DELETE /api/v1/categories/{id}` (`DeleteCategoryCommand` + handler; 409 on children or on
      invoice-item references, 404 on missing). Client flattens categories depth-first into
      `CategoryTreeRow`s (indented, Arabic-sorted, no cycle via `buildParentOptions` excluding
      self+descendants); tree order is canonical so the table is deliberately not sortable.
      Delete-confirm buttons render in the dialog body (footer projection is unreliable in
      zoneless tests); MSW `params` require `params['id']` (index signature).
- [x] **P3.4 Gold prices** (auth only) — live price chip + prices page from
      `/gold-prices/current` with `httpResource` refetch (SW STW 5m). Verify: price tile updates
      without page reload.
      **Note:** the chip already existed (interval auto-refresh). Added `/gold-prices` page
      (`GoldPricesPage`) using `httpResource` with `withCredentials`; auto-refresh via `reload()`
      on the same 5m interval, tiles update in place via signals. Added a `gold-prices` dataGroup
      (Performance/5m/5s) ahead of the generic `api-cache` in `ngsw-config.json` and enabled
      `withFetch()` in `app.config.ts` so the SW can intercept API requests (HttpClient used XHR
      before, which the SW ignores). Note: on ANY resource error `hasValue()` flips to false and
      `value()` throws — the page guards every `value()` read and shows an inline retry card.
      New `coins` icon + auth-only nav item (no feature gate; backend group requires auth only).
- [x] **P3.5 Users & settings** (`settings`) — `/settings/users`: list from `/users`, create
      (`/users`), edit (`/users/{id}`), delete (`/users/{id}`); profile from `/users/me`.
      Verify: cannot send `tenantId` in any payload (assert in MSW tests).
      **Note:** the backend only allows editing the current user
      (`UpdateUserCommandHandler` — `userContext.UserId != command.Id` → 401), so the page
      exposes edit for the current user's row only (badge "أنت" from `/users/me`) and hides
      delete for self; create/delete apply to any tenant user. Profile card (name + email +
      "تعديل بياناتي") sits above the users table, fed by `/users/me`. Password is optional
      in edit mode (blank = keep, per `UpdateUserRequest.password`); create requires ≥ 8 chars.
      `tenantId` absence is asserted inside the MSW handlers for POST/PUT in both store and
      page specs, and was verified live against the real API (request body carries only
      `{ email, firstName, lastName, password }`). New icons: `settings`, `user-plus`.
- [x] **P3.6 Suppliers** (`suppliers`) — list/create/edit/toggle from `/suppliers`;
      deliveries create from `/supplier-deliveries` (server computes 21K — mirror the command
      shape only); scrap-gold & manufacturing payments from `/supplier-payments/*`;
      financial transactions list + create + payments + kpis from
      `/supplier-financial-transactions`. Verify: delivery posts with `weightInGrams`+`karat`
      only.
      **Note:** one page (`SuppliersPage`) for the whole feature: suppliers `app-table`
      (sortable by name/balances/last activity), supplier financial KPI cards per currency
      (له/لنا/net + count), and a manual paged transactions table (filters: supplier/direction/
      search). Delivery lines post `{ karat, weightInGrams }` only — the server computes the
      21K-equivalent; asserted in MSW handlers (store + page specs) alongside the `tenantId`
      absence in every POST/PUT body. Direction radio: 1 = له (سلفة من مورد), 2 = لنا (سلفة
      لمورد). Accounts for payment forms come from `/finance/accounts` **best-effort**
      (gated `feature: finance` — a suppliers-only user sees an inline notice instead of an
      error toast; the forms degrade gracefully). There is no delete endpoint in the
      suppliers group — rows are toggled via `POST /suppliers/{id}/toggle-active`. New
      icons: `truck`, `coins`, `receipt`, `wallet` (reused), `power`, `eye`.
- [x] **P3.7 Inventory** (`inventory`) — adjustments list/create + kpis from
      `/inventory/adjustments`; gold ledger paged (karat/from/to/referenceType filters) +
      trend chart + kpis from `/inventory/gold-ledger*`. Verify: ledger IN/OUT math in e2e.
      **Note:** one page (`InventoryPage`) for the whole feature: inventory KPIs (21K-equivalent
      total + estimated value + per-karat breakdowns with the primary-21K badge), today's
      adjustment KPIs, a 7/14/30-day trend chart (pure CSS bars — gold IN / red OUT, day
      labels, net-over-period footer; no chart library), a paged adjustments table
      (type/date filters + create via `AdjustmentDialog`), and a paged gold-ledger table
      (karat/reference-type/date filters). The adjustment dialog posts the exact
      `CreateInventoryAdjustmentRequest` keys (`adjustmentType` 1..5, `karat` 18/21/24,
      `weightInGrams`, `reason`, `notes`, `date`) — asserted in MSW handlers (store + page
      specs) alongside the `tenantId` absence. `referenceType` filter values are the
      `GoldReferenceType` enum names (SupplierDelivery, CustomerGoldPurchase, Sale,
      SupplierScrapPayment, InventoryAdjustment); the trend endpoint clamps `days` 1..90.
      The server's `typeColor`/`typeBg`/`movementColor` Tailwind class names are **not**
      reused client-side — types map to `app-badge` variants (Increase → success,
      Correction → neutral, others → error) since server-only classes (e.g.
      `bg-error-container/30`) may not exist in the client theme. Adjustment rows show the
      signed weight (`+`/`-`, green/red). The server 409 `InsufficientStock` surfaces inline
      in the dialog error box. New icons: none (reuses `boxes`, `plus`, `filter`,
      `trending-up/down`).
- [x] **P3.8 Sales** (`sales`) — one `SalesPage` for the whole feature: today's KPIs +
      invoice totals from `/sales-invoices/kpis` (Display strings only — the raw values are
      sums across mixed currencies, so no unit/symbol is shown), a paged invoices table
      (search by invoice number or customer name, status + from/to date filters) and two
      dialogs — `InvoiceDialog` (create) and `InvoiceDetailDialog` (row → `GET /{id}`).
      The create dialog mirrors `CreateSalesInvoiceRequest` exactly (asserted in MSW
      handlers, store + page specs, alongside the `tenantId` absence): customer
      name/phone, a line-items editor (optional category — degraded inline when
      `feature: catalog` is missing, karat 18/21/24, weight, price — the server computes
      the 21K-equivalent, never the client), the required selling employee (best-effort
      from `/employees`, gated `feature: hr` — a sales-only user sees an inline notice
      that creating invoices needs the HR permission), invoice currency (JOD/USD/ILS from
      the reference store), the due total, and a payment section with a single method
      (نقدي / تحويل بنكي + receiving account + buyer account number) **or** multi-currency
      payment legs (account → currency → amount → rate; the base-currency rate is fixed at
      1 and the equivalent = amount × rate, rounded to 3; the paid amount equals the legs
      total and is capped by the total; first-leg account type decides Cash vs Bank and the
      primary accountId). `next-number` prefills the dialog header; the summary box shows
      total weight, gold value, paid and the remaining (debt) in the invoice currency; the
      server 400 validation / 409 insufficient-stock errors surface inline. The detail
      dialog renders the row instantly and refreshes via `GET /{id}` while open; status
      maps to `app-badge` variants (Completed → success, PartiallyPaid → warning,
      Draft → neutral, Cancelled → error); `paymentMethod` shows نقدي/مصرفي only when
      non-empty; `categoryName` is null server-side so items render without a category.
      Paginated responses use `pageNumber` (the suppliers' custom paged DTO `page` is
      correct for its own endpoint). Shared `Dialog` gained a `maxWidth` input
      (default `max-w-lg`; the invoice dialog uses `max-w-3xl`). New icons: none (reuses
      `receipt`, `plus`, `trash-2`, `eye`, `filter`, `search`, `coins`, `wallet`,
      `trending-up`, `chevrons`).
- [x] **P3.9 Purchases** (`purchases`) — `/customer-purchases/invoices` create + next-number,
      list. Verify: gold IN + financial OUT appear on the ledger.
      **Rebuilt sales-style (same shape as P3.8):** the backend group now exposes paged list
      (`GET /?page&pageSize&fromDate&toDate&search`), KPIs (`GET /kpis` — today count, total
      purchases/paid/remaining with server-computed Display strings, cached per tenant like the
      sales KPIs) and detail (`GET /{id}`), so the page is a KPI row + filterable paged table
      + `PurchaseDialog` (the create form moved into a modal, mirroring `InvoiceDialog`) +
      `PurchaseDetailDialog`. The form keeps the MVC `customer-purchase-invoices.js` builder:
      seller info (name/ID number — both required, year of birth 1940–2100, phone, address), a
      line-items editor (optional category — degraded inline when `feature: catalog` is
      missing, karat 18/21/24, weight, price — the server computes the 21K-equivalent, never
      the client), the buying employee (best-effort from `/employees`, gated `feature: hr`),
      invoice date, invoice currency (JOD/USD/ILS from the reference store), the due total,
      and a payment section with a single method (نقدي / تحويل بنكي — the API requires an
      `accountId` even for cash, so the dialog auto-selects the first account matching the
      currency + Cash/Bank type, mirroring the MVC auto-selection, with an inline notice when
      none matches) **or** multi-currency payment legs (account → currency → amount → rate;
      base-currency rate fixed at 1, equivalent = amount × rate rounded to 3, paid = legs
      total capped by the total, first-leg account type decides Cash vs Bank and the primary
      accountId). `next-number` (`PUR-{period}-…`) prefills the dialog header; the summary box
      shows total weight, gold value, per-karat weight totals, total, paid and the remaining
      (debt) in the invoice currency; the server 400 validation / account mismatch /
      insufficient-balance errors surface inline. Success toasts "تم إصدار فاتورة شراء
      الذهب بنجاح", emits `saved`, closes the dialog and the page reloads list + KPIs +
      next-number. The payload mirrors `CreateCustomerPurchaseInvoiceRequest` key-for-key
      (asserted in MSW handlers, store + page specs, alongside the `tenantId` absence). New
      icons: none (reuses `shopping-bag`, `plus`, `trash-2`, `receipt`, `trending-up`,
      `wallet`, `coins`, `eye`, `search`, `filter`, `chevrons`).
- [x] **P3.10 Finance** (`finance`) — accounts list + with-balances + create + set balance
      from `/finance/accounts`; debts paged + create + pay from `/finance/debts`; transactions
      paged + recent from `/finance/transactions`. Verify: balance equals ledger sum.
      **Implemented as one `FinancePage` in three tabs — الحركات المالية first, then الذمم,
      then الحسابات (each lazy-loads on first activation):** debt KPI cards
      (لنا / علينا / صافي from `/debts/kpis` with the server Display strings), the accounts
      table (`/accounts/with-balances?activeOnly=false` — name, نوع نقدي/مصرفي, currency,
      number, ledger-derived balance, inactive badge, per-currency totals strip computed
      client-side so "balance equals ledger sum" is visible at a glance) with a set-balance
      dialog (target ≥ 0 + reason; shows current balance and the signed difference — the
      server posts a ManualAdjustment entry for the delta), an account-create dialog
      (name/currency/number/notes/opening-balance; the server always creates Bank-type and
      auto-numbers when omitted), the paged debts table (direction filter Receivable/Payable +
      search, outstanding from the server, سداد action hidden at zero) with a create-debt
      dialog (direction radio decides the flow: receivable OUT / payable IN on creation;
      account select filtered to the chosen currency + active only — the server 409s any
      mismatch) and a payment dialog (amount capped by the outstanding client-side too),
      and the read-only paged transactions table (account-name/type/currency/date filters;
      amounts signed + colored by Inflow/Outflow — the API returns enum names as strings).
      Every successful mutation reloads accounts + KPIs + debts + transactions. Payloads
      mirror the request records key-for-key with no `tenantId` (asserted in MSW handlers,
      store + page specs). New icons: none (reuses `wallet`, `plus`, `pencil`, `filter`,
      `trending-up/down`, `chevrons`).
- [x] **P3.11 Expenses** (`expenses`) — categories CRUD + expenses CRUD + paged + kpis from
      `/expenses*`. Verify: delete reversal posts correct financial OUT.
      **Implemented as one `ExpensesPage` in two tabs — المصروفات first, then التصنيفات (each
      lazy-loads on first activation):** expense KPI cards from `/expenses/kpis` (مصروفات اليوم
      / مصروفات الشهر / أكثر تصنيف إنفاقاً — all per-currency `CurrencyTotal`s with the
      top-category name), a paged expenses table (category + accountName + from/to date
      filters; rows show `expenseDate` via `formatDate`, category badge, description,
      red OUT amount + `currencySymbol` via `data-mono`, `accountName`, edit/delete
      actions) backed by `GET /expenses?page&pageSize&categoryId&accountName&fromDate&toDate`,
      and a categories table (`GET /expenses/categories?activeOnly=false` — name + active
      badge, no toggle endpoint exists). Dialogs: `ExpenseDialog` (create/update — mirrors
      `CreateExpenseRequest`/`UpdateExpenseRequest` key-for-key: `expenseDate` `YYYY-MM-DD`,
      `categoryId` nullable + "بدون تصنيف" option, `description` nullable, `amount` > 0,
      `accountId` required — account select is best-effort from `/finance/accounts`, gated
      `feature: finance`, degraded with an inline notice when empty/gated; no `tenantId`) and
      `ExpenseCategoryDialog` (create/update — mirrors `CreateExpenseCategoryRequest`:
      `name` only, 409 on duplicate name). Delete expense removes its financial OUT
      transaction (ledger reversal, verified by `DELETE /expenses/{id}` → success toast +
      reload of expenses + KPIs + categories); delete category 409s when any expense still
      references it and surfaces `saveError` inline. Every successful mutation reloads
      expenses + KPIs + categories. Payloads mirror the request records key-for-key with no
      `tenantId` (asserted in MSW handlers, store + page specs). New icons: none (reuses
      `trending-down`, `receipt`, `wallet`, `calendar`, `plus`, `pencil`, `trash-2`,
      `filter`, `chevrons`, `alert-circle`, `inbox`).
- [x] **P3.12 HR** (`hr`) — employees CRUD/toggle/list/by-id, pay-salary, salary-payments,
      salary-period-summary, unlinked-users from `/employees*`. Verify: salary payment hits
      the ledger.
      **Implemented as one `HrPage` in two tabs — الموظفون first, then سجل الرواتب (each
      lazy-loads on first activation):** employees table from `GET /employees` (fullName +
      roleName, salary `data-mono` + `currency` JOD/USD/ILS + `salaryCycleName`, userEmail,
      active badge, lastPaymentDate/Net via `EmployeeResponse`, actions تعديل/تفعيل-إيقاف/دفع
      راتب — daily cycle (1) disables pay, inactive disables pay) backed by `EmployeeDialog`
      (create → `POST /employees` with `CreateEmployeeRequest` key-for-key: `firstName`,
      `lastName`, `role` 1..4, `salary` >0, `currency` 1..3, `salaryCycle` 1..3, `connectToUser`
      + `existingUserId` from `GET /employees/unlinked-users` best-effort or `newUserEmail`
      + `newUserPassword` ≥8; update → `PUT /employees/{id}` with `UpdateEmployeeRequest`:
      `firstName`, `lastName`, `role`, `salary`, `currency`, `salaryCycle`, `isActive`; no
      `tenantId`) and `PaySalaryDialog` (pay → `POST /employees/{id}/pay-salary` with
      `PaySalaryRequest` key-for-key: `accountId`, `amount`, `paymentDate` `YYYY-MM-DD`,
      `notes` — account select best-effort from `/finance/accounts` gated `feature: finance`,
      filtered to employee currency + active only, degraded inline notice; auto-fetches
      `GET /employees/salary-period-summary?employeeId&paymentDate` on open/date change and
      shows net/alreadyPaid/remaining/discount/scheduledDate/cycle/isFullyPaid; amount capped
      by `remaining` and disabled when fully paid; 409 insufficient-balance/currency-mismatch
      surfaces inline); toggle via `POST /employees/{id}/toggle-active`; paged salary
      payments table from `GET /employees/salary-payments?page&pageSize&employeeName&fromDate&toDate`
      (employeeName partial, date range) with `isOnSchedule` badge; `GET /employees/{id}` detail
      route exists but the page uses the list for editing (detail kept in store for future
      drawer). Every successful mutation reloads employees + payments. Payloads mirror request
      records key-for-key with no `tenantId` (asserted in MSW handlers, store + page specs).
      New icons: none (reuses `users`, `plus`, `pencil`, `power`, `wallet`, `receipt`,
      `filter`, `chevrons`, `alert-circle`, `inbox`).
- [x] **P3.13 Host admin** (cookie auth) — `/host/admin/tenants` list/status PATCH from
      `/host/api/v1/tenants` + reconciliation view. **How:** guard by `isHostAdmin`; XSRF
      interceptor active; never mixed into tenant services. Verify: host login → tenants list.
      **Implemented as one `HostAdminPage` in two tabs — المستأجرون first, then المطابقة (each
      lazy-loads on first activation):** tenants table from `GET /host/api/v1/tenants`
      (key/name/status badge + 4 variants Trial→warning/Active→success/Cancelled→error/Pending→neutral,
      action تغيير الحالة) backed by `HostUpdateStatusDialog` (posts `UpdateTenantStatusRequest`
      key-for-key: `newStatus` 0..3 + `transitionAtUtc` UTC ISO | null — `datetime-local` → `toISOString()`,
      to `PATCH /host/api/v1/tenants/{id}/status`, validated by `AntiforgeryEndpointFilter`,
      `XSRF-TOKEN`→`X-XSRF-TOKEN` via `xsrfInterceptor` which now fires for `/host`; no `tenantId` in
      any body, asserted in MSW handlers store+page); reconciliation view from
      `GET /host/api/v1/reconciliation?tenantId` (optional filter select populated from tenants list,
      all→no query) showing `generatedAtUtc` + anomalies table (`table/tenantId/count/message` — `Missing
      TenantId` → should be empty) + per-tenant blocks (`rowCounts`, `goldStock` with `totalEquivalent21K`,
      `financialTotals`+`financialBalances`, `debtTotals`, collapsible supplier balances) recomputed from
      raw ledger entries. Route `host/admin/tenants` outside `AppShell` (host session only) with
      `hostGuard` → `/host/login`; nav `إدارة المنصة` (`server` icon) visible only when `isHostAdmin`.
      Proxy `proxy.conf.json` now forwards both `/api` and `/host` to `http://localhost:5999`. Payloads
      mirror request records key-for-key with no `tenantId`. New icons: none (reuses `server`, `pencil`,
      `search`, `refresh-cw`, `check-circle`, `alert-circle`).
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
- [ ] Tenant users authenticate via `/api/v1/auth/*` and host admins via `/host/api/v1/auth/*` —
      **both** APIs issue JWTs into HttpOnly cookies (tenant access+refresh, host access); the
      client stores no tokens and never sends `TenantId`.
- [ ] All 10 feature gates have lazy, permission-driven RTL pages over the A6 endpoints
      (dashboard, catalog, gold-prices, suppliers, inventory, sales, purchases, finance,
      expenses, hr) + host-admin surface.
- [ ] PWA: offline shell + gold-price STW + reference cache-first + KPI network-first;
      Lighthouse performance ≥ 90, accessibility ≥ 95, PWA = 100.
- [ ] Vitest unit suite (coverage gates) + Playwright golden path green in CI; Lighthouse CI
      gates deploys.
- [ ] README + ADRs written; `AGENTS.md` and `docs/tasks.md §3` updated; MVC (`src/WebUI`)
      untouched and still runnable.