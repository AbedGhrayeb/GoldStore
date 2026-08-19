import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { TokenStorage } from '../auth/token-storage';
import { isHostRequest } from './request.util';

/** Attaches the in-memory access token to tenant API requests. Host requests use cookies only. */
export const bearerInterceptor: HttpInterceptorFn = (req, next) => {
  if (isHostRequest(req.url)) {
    return next(req);
  }

  const accessToken = inject(TokenStorage).access;
  if (accessToken === null) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }));
};
