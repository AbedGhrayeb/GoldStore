import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type { MeResponse } from '../../shared/api/api-types';
import type { Login2faRequired } from './auth-api.service';
import { AuthApi } from './auth-api.service';

/**
 * The only global store (ADR-2). Holds the current tenant user (claims from /auth/me) and the
 * host-admin flag (cookie session). All state is signal-backed; components never touch tokens.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);

  private readonly userSignal = signal<MeResponse | null>(null);
  private readonly hostAdminSignal = signal(false);
  private refreshInFlight: Promise<boolean> | null = null;
  private restoreInFlight: Promise<void> | null = null;

  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null);
  readonly isHostAdmin = this.hostAdminSignal.asReadonly();
  readonly permissions = computed(() => this.userSignal()?.permissions ?? []);
  readonly roles = computed(() => this.userSignal()?.roles ?? []);
  readonly tenantKey = computed(() => this.userSignal()?.tenantKey ?? null);

  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  hasAnyPermission(...permissions: string[]): boolean {
    const current = this.permissions();
    return permissions.some((p) => current.includes(p));
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  async login(email: string, password: string): Promise<MeResponse | Login2faRequired> {
    const res = await firstValueFrom(this.api.login(email, password));
    if (res && (res as Login2faRequired).is2fa) return res as Login2faRequired;
    return this.loadMe();
  }

  async hostLogin(email: string, password: string): Promise<void | Login2faRequired> {
    const res = await firstValueFrom(this.api.hostLogin(email, password));
    if (res && (res as Login2faRequired).is2fa) return res as Login2faRequired;
    if (typeof localStorage !== 'undefined') localStorage.setItem('isHostAdmin', 'true');
    await this.restoreHostSession();
    return undefined;
  }

  /** Restores either HttpOnly-cookie session before guards execute on an application reload. */
  async restoreSessions(): Promise<void> {
    if (this.restoreInFlight) return this.restoreInFlight;

    const path = typeof window !== 'undefined' ? window.location.pathname : '';
    const isPublicRoute = ['/login', '/enroll-phone', '/verify-2fa', '/forgot-password', '/403', '/host/login'].some(
      (p) => path === p || path.startsWith(`${p}/`),
    );

    // Use persisted flags to avoid noisy 401s for anonymous users.
    // Flags are set on successful loadMe / hostMe and cleared on logout.
    const hasTenantFlag =
      typeof localStorage !== 'undefined' && localStorage.getItem('isTenantAuthenticated') === 'true';
    const hasHostFlag =
      typeof localStorage !== 'undefined' && localStorage.getItem('isHostAdmin') === 'true';

    // On public pages before login, don't hit the API at all — just clear state.
    if (isPublicRoute) {
      // Still restore if we have a flag (e.g., refresh on /login while already authenticated
      // will be redirected by guard, but we want the session available).
      if (!hasTenantFlag && !hasHostFlag) {
        this.userSignal.set(null);
        this.hostAdminSignal.set(false);
        return;
      }
    }

    const tasks: Promise<void>[] = [];
    if (hasTenantFlag) {
      tasks.push(this.restoreTenantSession());
    } else {
      this.userSignal.set(null);
    }

    if (hasHostFlag) {
      tasks.push(this.restoreHostSession());
    } else {
      this.hostAdminSignal.set(false);
    }

    if (tasks.length === 0) return;

    this.restoreInFlight = Promise.all(tasks)
      .then(() => {})
      .finally(() => (this.restoreInFlight = null));
    return this.restoreInFlight;
  }

  /** Rotates the HttpOnly refresh cookie. Concurrent 401s share one rotation. */
  async silentRefresh(): Promise<boolean> {
    if (this.refreshInFlight === null) {
      this.refreshInFlight = firstValueFrom(this.api.refresh())
        .then(() => true)
        .catch(() => {
          this.clearSession();
          return false;
        })
        .finally(() => {
          this.refreshInFlight = null;
        });
    }
    return this.refreshInFlight;
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.api.logout());
    } catch {
      // Revocation failure is not worth blocking the local sign-out.
    }
    this.clearSession();
  }

  async hostLogout(): Promise<void> {
    try {
      await firstValueFrom(this.api.hostLogout());
    } catch {
      // Cookie may already be gone.
    }
    this.hostAdminSignal.set(false);
    if (typeof localStorage !== 'undefined') localStorage.removeItem('isHostAdmin');
  }

  clearSession(): void {
    this.userSignal.set(null);
    if (typeof localStorage !== 'undefined') localStorage.removeItem('isTenantAuthenticated');
  }

  private async loadMe(): Promise<MeResponse> {
    const me = await firstValueFrom(this.api.me());
    this.userSignal.set(me);
    if (typeof localStorage !== 'undefined') localStorage.setItem('isTenantAuthenticated', 'true');
    return me;
  }

  private async restoreTenantSession(): Promise<void> {
    try {
      await this.loadMe();
    } catch (error) {
      // On 401 the HttpOnly access cookie is expired – try single-flight refresh once
      // before deciding the session is gone. This prevents the refreshInterceptor
      // from creating a second visible 401+retry for the same restore.
      if (isHttpError(error, 401)) {
        const refreshed = await this.silentRefresh();
        if (refreshed) {
          try {
            await this.loadMe();
            return;
          } catch {
            // still unauthorized after refresh
          }
        }
      }
      this.userSignal.set(null);
    }
  }

  private async restoreHostSession(): Promise<void> {
    // Host session is only relevant for platform admins. Avoid a noisy 401 on every
    // page refresh for regular store users by checking the persisted flag.
    if (typeof localStorage !== 'undefined' && localStorage.getItem('isHostAdmin') !== 'true') {
      this.hostAdminSignal.set(false);
      return;
    }
    try {
      await firstValueFrom(this.api.hostMe());
      this.hostAdminSignal.set(true);
      if (typeof localStorage !== 'undefined') localStorage.setItem('isHostAdmin', 'true');
    } catch {
      this.hostAdminSignal.set(false);
      if (typeof localStorage !== 'undefined') localStorage.removeItem('isHostAdmin');
    }
  }
}

function isHttpError(error: unknown, status: number): boolean {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    (error as { status: number }).status === status
  );
}
