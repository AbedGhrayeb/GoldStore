import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { LoginRequest, LoginResponse, User, RefreshTokenResponse } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly user = signal<User | null>(this.getStoredUser());
  readonly isAuthenticated = signal(!!this.getStoredToken());


login(data: LoginRequest) {
  return this.http.post<LoginResponse>(
    `${environment.apiUrl}/users/login`,
    data
  ).pipe(
    tap(res => {
      localStorage.setItem('auth_token', res.token);
      localStorage.setItem('refresh_token', res.refreshToken);
      localStorage.setItem('token_expires', res.expiresAt);
      this.isAuthenticated.set(true);
    })
  );
}

  logout() {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('token_expires');
    localStorage.removeItem('current_user');
    this.user.set(null);
    this.isAuthenticated.set(false);
    this.router.navigate(['/login']);
  }

  refreshToken() {
    const refreshToken = localStorage.getItem('refresh_token');
    return this.http.post<RefreshTokenResponse>('${environment.apiUrl}/api/auth/refresh', { refreshToken }).pipe(
      tap(res => {
        localStorage.setItem('auth_token', res.token);
        localStorage.setItem('refresh_token', res.refreshToken);
        localStorage.setItem('token_expires', res.expiresAt);
      }),
    );
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  private getStoredToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  private getStoredUser(): User | null {
    try {
      const data = localStorage.getItem('current_user');
      return data ? JSON.parse(data) : null;
    } catch {
      return null;
    }
  }
}
