import { HttpClient, HttpContext, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import type { components } from '../../shared/api/schema';
import type { HostMeResponse, MeResponse } from '../../shared/api/api-types';
import { ApiClient } from '../http/api-client.service';
import { SKIP_ERROR_TOAST } from '../http/error.interceptor';

type LoginRequest = components['schemas']['LoginRequest'];
const NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

export type Login2faRequired = {
  requiresEnrollment: boolean;
  requiresTwoFactor: boolean;
  maskedPhone: string | null;
  tempTicket: string;
  is2fa: true;
};

/**
 * Browser auth transport. Both login APIs set HttpOnly JWT cookies; this service deliberately
 * never receives, stores, or attaches raw tokens.
 */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly api = inject(ApiClient);
  private readonly http = inject(HttpClient);

  login(email: string, password: string): Observable<void | Login2faRequired> {
    const body: LoginRequest = { email, password };
    // Use raw HttpClient to detect 202 2FA response (ApiClient swallows status)
    return this.http
      .post('/api/v1/auth/login', body, { context: NO_TOAST, observe: 'response' as const })
      .pipe(
        map((res: HttpResponse<unknown>) => {
          if (res.status === 202) {
            const b = res.body as Record<string, unknown>;
            return {
              requiresEnrollment: Boolean(b['requiresEnrollment']),
              requiresTwoFactor: Boolean(b['requiresTwoFactor']),
              maskedPhone: (b['maskedPhone'] as string | null) ?? null,
              tempTicket: b['tempTicket'] as string,
              is2fa: true as const,
            };
          }
          return undefined;
        }),
      );
  }

  refresh(): Observable<void> {
    return this.api.post<void>('/api/v1/auth/refresh', {}, { context: NO_TOAST });
  }

  logout(): Observable<void> {
    return this.api.post<void>('/api/v1/auth/logout', {}, { context: NO_TOAST });
  }

  me(): Observable<MeResponse> {
    return this.api.get<MeResponse>('/api/v1/auth/me', { context: NO_TOAST });
  }

  hostLogin(email: string, password: string): Observable<void | Login2faRequired> {
    const body: LoginRequest = { email, password };
    return this.http
      .post('/host/api/v1/auth/login', body, { context: NO_TOAST, observe: 'response' as const, withCredentials: true })
      .pipe(
        map((res: HttpResponse<unknown>) => {
          if (res.status === 202) {
            const b = res.body as Record<string, unknown>;
            return {
              requiresEnrollment: Boolean(b['requiresEnrollment']),
              requiresTwoFactor: Boolean(b['requiresTwoFactor']),
              maskedPhone: (b['maskedPhone'] as string | null) ?? null,
              tempTicket: b['tempTicket'] as string,
              is2fa: true as const,
            };
          }
          return undefined;
        }),
      );
  }

  // Phone 2FA
  setupPhone2fa(tempTicket: string, idToken: string, isHost = false): Observable<{ phoneNumber: string; recoveryCodes: string[] }> {
    const url = isHost ? '/host/api/v1/auth/2fa/phone/setup' : '/api/v1/auth/2fa/phone/setup';
    return this.api.post<{ phoneNumber: string; recoveryCodes: string[] }>(url, { tempTicket, idToken }, { context: NO_TOAST });
  }

  verifyPhone2fa(tempTicket: string, idToken: string, isHost = false): Observable<void> {
    const url = isHost ? '/host/api/v1/auth/2fa/phone/verify' : '/api/v1/auth/2fa/phone/verify';
    return this.api.post<void>(url, { tempTicket, idToken }, { context: NO_TOAST });
  }

  verifyRecovery(tempTicket: string, recoveryCode: string, isHost = false): Observable<void> {
    const url = isHost ? '/host/api/v1/auth/2fa/phone/verify-recovery' : '/api/v1/auth/2fa/phone/verify-recovery';
    return this.api.post<void>(url, { tempTicket, recoveryCode }, { context: NO_TOAST });
  }

  forgotPasswordPhone(emailOrPhone: string, idToken: string, newPassword: string, isHost = false): Observable<void> {
    const url = isHost ? '/host/api/v1/auth/forgot-password/phone' : '/api/v1/auth/forgot-password/phone';
    return this.api.post<void>(url, { emailOrPhone, idToken, newPassword }, { context: NO_TOAST });
  }

  getFirebaseConfig(isHost = false): Observable<{ projectId: string; webApiKey: string; authDomain: string; appId: string }> {
    const url = isHost ? '/host/api/v1/auth/config/firebase' : '/api/v1/auth/config/firebase';
    return this.api.get<{ projectId: string; webApiKey: string; authDomain: string; appId: string }>(url, { context: NO_TOAST });
  }

  hostMe(): Observable<HostMeResponse> {
    return this.api.get<HostMeResponse>('/host/api/v1/auth/me', { context: NO_TOAST });
  }

  hostLogout(): Observable<void> {
    return this.api.post<void>('/host/api/v1/auth/logout', undefined, {
      context: NO_TOAST,
      withCredentials: true,
    });
  }
}
