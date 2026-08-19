import type { HttpInterceptorFn } from '@angular/common/http';

import { isHostRequest } from './request.util';
const XSRF_COOKIE = 'XSRF-TOKEN';
const XSRF_HEADER = 'X-XSRF-TOKEN';

/**
 * Adds the XSRF header to host (cookie-authenticated) requests. Tenant requests are bearer
 * authenticated and have no CSRF surface.
 */
export const xsrfInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isHostRequest(req.url)) {
    return next(req);
  }

  const token = readCookie(XSRF_COOKIE);
  if (token === null) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { [XSRF_HEADER]: token } }));
};

function readCookie(name: string): string | null {
  if (typeof document === 'undefined') {
    return null;
  }
  const match = document.cookie.match(new RegExp(`(?:^|;\\s*)${name}=([^;]*)`));
  return match === null ? null : decodeURIComponent(match[1]);
}
