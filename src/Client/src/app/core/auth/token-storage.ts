import { Injectable } from '@angular/core';

import type { components } from '../../shared/api/schema';

type TokenResponse = components['schemas']['TokenResponse'];

/**
 * Memory-only JWT holder (ADR-3). Tokens are never persisted to localStorage/sessionStorage,
 * so a page reload requires re-login.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private refreshExpiresAt: string | null = null;

  get access(): string | null {
    return this.accessToken;
  }

  get refresh(): string | null {
    return this.refreshToken;
  }

  hasTokens(): boolean {
    return this.accessToken !== null && this.refreshToken !== null;
  }

  set(tokens: TokenResponse): void {
    this.accessToken = tokens.accessToken;
    this.refreshToken = tokens.refreshToken;
    this.refreshExpiresAt = tokens.refreshExpiresAt;
  }

  clear(): void {
    this.accessToken = null;
    this.refreshToken = null;
    this.refreshExpiresAt = null;
  }
}
