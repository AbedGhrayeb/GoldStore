import { HttpErrorResponse } from '@angular/common/http';
import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';

import { AuthStore } from '../auth/auth-store';
import { TokenStorage } from '../auth/token-storage';
import { isHostRequest } from './request.util';

const AUTH_ENDPOINTS = ['/api/v1/auth/login', '/api/v1/auth/refresh', '/api/v1/auth/logout'];

/**
 * Single-flight token rotation (ADR-3): on a 401 from a tenant API call, rotate the refresh
 * token once (concurrent 401s share the same rotation) and retry the original request.
 * A failed rotation clears the session; the caller receives the original 401.
 *
 * Registered after the error interceptor so it sees 401s before any toast is emitted.
 */
export const refreshInterceptor: HttpInterceptorFn = (req, next) => {
  if (isHostRequest(req.url) || AUTH_ENDPOINTS.some((endpoint) => req.url.endsWith(endpoint))) {
    return next(req);
  }

  const tokenStorage = inject(TokenStorage);
  const authStore = inject(AuthStore);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (
        !(error instanceof HttpErrorResponse) ||
        error.status !== 401 ||
        !tokenStorage.hasTokens()
      ) {
        return throwError(() => error);
      }
      return from(authStore.silentRefresh()).pipe(
        switchMap((refreshed) => {
          if (!refreshed) {
            return throwError(() => error);
          }
          const accessToken = tokenStorage.access;
          return next(
            accessToken === null
              ? req
              : req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }),
          );
        }),
      );
    }),
  );
};
