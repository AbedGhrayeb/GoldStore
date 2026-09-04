import { isDevMode } from '@angular/core';
import { inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';

import type { ApplicationConfig } from '@angular/core';

import { routes } from './app.routes';
import { AuthStore } from './core/auth/auth-store';
import { errorInterceptor } from './core/http/error.interceptor';
import { refreshInterceptor } from './core/http/refresh.interceptor';
import { xsrfInterceptor } from './core/http/xsrf.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAppInitializer(() => inject(AuthStore).restoreSessions()),
    provideRouter(routes),
    // Response order: refresh (401 rotation) → error (toast) — so rotated 401s never toast.
    // withFetch makes requests fetch-based so the service worker's dataGroups (P3.4 STW)
    // can cache/intercept the gold-price feed in production.
    provideHttpClient(
      withFetch(),
      withInterceptors([errorInterceptor, refreshInterceptor, xsrfInterceptor]),
    ),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerImmediately',
    }),
  ],
};
