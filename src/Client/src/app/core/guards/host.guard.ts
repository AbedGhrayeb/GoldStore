import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';
import { createUrlTreeFromSnapshot } from '@angular/router';

import { AuthStore } from '../auth/auth-store';

/**
 * Host-admin guard. Grants access only to platform admins with an active cookie session
 * (isHostAdmin), otherwise redirects to /host/login.
 */
export const hostGuard: CanActivateFn = (route) => {
  const auth = inject(AuthStore);
  if (auth.isHostAdmin()) {
    return true;
  }
  return createUrlTreeFromSnapshot(route, ['/host/login']);
};
