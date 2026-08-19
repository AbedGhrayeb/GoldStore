import { HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { components } from '../../shared/api/schema';
import type { MeResponse } from '../../shared/api/api-types';
import { ApiClient } from '../http/api-client.service';
import { SKIP_ERROR_TOAST } from '../http/error.interceptor';

type LoginRequest = components['schemas']['LoginRequest'];
type RefreshTokenRequest = components['schemas']['RefreshTokenRequest'];
type TokenResponse = components['schemas']['TokenResponse'];

const NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

/** Auth transport: tenant bearer endpoints under /api/v1/auth and host cookie endpoints under /host/api/v1/auth. */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly api = inject(ApiClient);

  login(email: string, password: string): Observable<TokenResponse> {
    const body: LoginRequest = { email, password };
    return this.api.post<TokenResponse>('/api/v1/auth/login', body, { context: NO_TOAST });
  }

  refresh(refreshToken: string): Observable<TokenResponse> {
    const body: RefreshTokenRequest = { refreshToken };
    return this.api.post<TokenResponse>('/api/v1/auth/refresh', body, { context: NO_TOAST });
  }

  logout(refreshToken: string): Observable<void> {
    const body: RefreshTokenRequest = { refreshToken };
    return this.api.post<void>('/api/v1/auth/logout', body, { context: NO_TOAST });
  }

  me(): Observable<MeResponse> {
    return this.api.get<MeResponse>('/api/v1/auth/me');
  }

  hostLogin(email: string, password: string): Observable<void> {
    const body: LoginRequest = { email, password };
    return this.api.post<void>('/host/api/v1/auth/login', body, {
      context: NO_TOAST,
      withCredentials: true,
    });
  }

  hostLogout(): Observable<void> {
    return this.api.post<void>('/host/api/v1/auth/logout', undefined, {
      context: NO_TOAST,
      withCredentials: true,
    });
  }
}
