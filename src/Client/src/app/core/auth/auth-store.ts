import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type { MeResponse } from '../../shared/api/api-types';
import { AuthApi } from './auth-api.service';
import { TokenStorage } from './token-storage';

/**
 * The only global store (ADR-2). Holds the current tenant user (claims from /auth/me) and the
 * host-admin flag (cookie session). All state is signal-backed; components never touch tokens.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly tokens = inject(TokenStorage);

  private readonly userSignal = signal<MeResponse | null>(null);
  private readonly hostAdminSignal = signal(false);
  private refreshInFlight: Promise<boolean> | null = null;

  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null);
  readonly isHostAdmin = this.hostAdminSignal.asReadonly();
  readonly permissions = computed(() => this.userSignal()?.permissions ?? []);
  readonly tenantKey = computed(() => this.userSignal()?.tenantKey ?? null);

  async login(email: string, password: string): Promise<MeResponse> {
    const tokens = await firstValueFrom(this.api.login(email, password));
    this.tokens.set(tokens);
    return this.loadMe();
  }

  async hostLogin(email: string, password: string): Promise<void> {
    await firstValueFrom(this.api.hostLogin(email, password));
    this.hostAdminSignal.set(true);
  }

  /** Rotates the refresh token. Single-flight: concurrent 401s share one rotation. */
  async silentRefresh(): Promise<boolean> {
    if (this.tokens.refresh === null) {
      return false;
    }
    if (this.refreshInFlight === null) {
      this.refreshInFlight = firstValueFrom(this.api.refresh(this.tokens.refresh))
        .then((tokens) => {
          this.tokens.set(tokens);
          return true;
        })
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
    const refreshToken = this.tokens.refresh;
    if (refreshToken !== null) {
      try {
        await firstValueFrom(this.api.logout(refreshToken));
      } catch {
        // Revocation failure is not worth blocking the local sign-out.
      }
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
    this.tokens.clear();
    this.userSignal.set(null);
  }

  private async loadMe(): Promise<MeResponse> {
    const me = await firstValueFrom(this.api.me());
    this.userSignal.set(me);
    return me;
  }
}
