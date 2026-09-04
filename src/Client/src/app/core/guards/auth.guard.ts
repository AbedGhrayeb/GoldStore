import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';
import { createUrlTreeFromSnapshot } from '@angular/router';

import { AuthStore } from '../auth/auth-store';

/**
 * Tenant-area guard. Redirects to /login (preserving the attempted URL for post-login
 * redirect) when no /auth/me session is loaded.
 */
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthStore);
  if (auth.isAuthenticated()) {
    return true;
  }
  return createUrlTreeFromSnapshot(route, ['/login'], { returnUrl: state.url });
};
