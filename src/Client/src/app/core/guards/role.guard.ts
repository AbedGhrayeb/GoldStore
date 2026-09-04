import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';
import { createUrlTreeFromSnapshot } from '@angular/router';

import { AuthStore } from '../auth/auth-store';

/**
 * Role guard factory. Grants access only when the /auth/me claims contain the role
 * (e.g. `store_admin`), otherwise redirects to the 403 page.
 */
export function roleGuard(role: string): CanActivateFn {
  return (route) => {
    const auth = inject(AuthStore);
    if (auth.user()?.roles.includes(role) ?? false) {
      return true;
    }
    return createUrlTreeFromSnapshot(route, ['/403']);
  };
}
