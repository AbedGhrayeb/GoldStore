import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';
import { createUrlTreeFromSnapshot } from '@angular/router';

import type { FeatureKey } from '../../shared/api/api-types';
import { AuthStore } from '../auth/auth-store';

/**
 * Feature-area guard factory. Grants access only when the /auth/me claims contain the
 * feature key (bare keys, e.g. `catalog` — no `feature:` prefix), otherwise redirects
 * to the 403 page.
 */
export function featureGuard(feature: FeatureKey): CanActivateFn {
  return (route) => {
    const auth = inject(AuthStore);
    if (auth.permissions().includes(feature)) {
      return true;
    }
    return createUrlTreeFromSnapshot(route, ['/403']);
  };
}
