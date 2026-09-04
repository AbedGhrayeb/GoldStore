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
    await this.restoreHostSession();
    return undefined;
  }

  /** Restores either HttpOnly-cookie session before guards execute on an application reload. */
  async restoreSessions(): Promise<void> {
    await Promise.all([this.restoreTenantSession(), this.restoreHostSession()]);
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
  }

  clearSession(): void {
    this.userSignal.set(null);
  }

  private async loadMe(): Promise<MeResponse> {
    const me = await firstValueFrom(this.api.me());
    this.userSignal.set(me);
    return me;
  }

  private async restoreTenantSession(): Promise<void> {
    try {
      await this.loadMe();
    } catch {
      this.userSignal.set(null);
    }
  }

  private async restoreHostSession(): Promise<void> {
    try {
      await firstValueFrom(this.api.hostMe());
      this.hostAdminSignal.set(true);
    } catch {
      this.hostAdminSignal.set(false);
    }
  }
}
