import { HttpContextToken } from '@angular/common/http';
import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ToastStore } from '../toast/toast-store';
import { toApiError } from './api-error';

/** Set on a request context to suppress the global error toast (callers surface their own errors). */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

/**
 * Maps HTTP failures to a typed ApiError (ProblemDetails shape) and emits a toast for the
 * user, unless the request opted out via SKIP_ERROR_TOAST.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastStore = inject(ToastStore);

  return next(req).pipe(
    catchError((error: unknown) => {
      const apiError = toApiError(error);
      if (!req.context.get(SKIP_ERROR_TOAST)) {
        toastStore.error(apiError.detail ?? apiError.title ?? `خطأ غير متوقع (${apiError.status})`);
      }
      return throwError(() => apiError);
    }),
  );
};
