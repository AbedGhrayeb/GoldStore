import { isDevMode } from '@angular/core';
import { provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';

import type { ApplicationConfig } from '@angular/core';

import { routes } from './app.routes';
import { bearerInterceptor } from './core/http/bearer.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { refreshInterceptor } from './core/http/refresh.interceptor';
import { xsrfInterceptor } from './core/http/xsrf.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // Response order: refresh (401 rotation) → error (toast) — so rotated 401s never toast.
    provideHttpClient(
      withInterceptors([bearerInterceptor, errorInterceptor, refreshInterceptor, xsrfInterceptor]),
    ),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerImmediately',
    }),
  ],
};
