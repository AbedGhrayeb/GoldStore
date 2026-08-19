import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environment/environment';

export interface ApiRequestOptions {
  context?: HttpContext;
  withCredentials?: boolean;
}

/**
 * Thin typed wrapper over HttpClient that resolves the API base URL from the environment
 * (absolute URL in dev, same-origin in production). Feature facades use this service —
 * never HttpClient directly.
 */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.API_BASE_URL;

  get<T>(path: string, options?: ApiRequestOptions): Observable<T> {
    return this.http.get<T>(this.resolve(path), this.toHttpOptions(options));
  }

  post<T>(path: string, body?: unknown, options?: ApiRequestOptions): Observable<T> {
    return this.http.post<T>(this.resolve(path), body, this.toHttpOptions(options));
  }

  patch<T>(path: string, body?: unknown, options?: ApiRequestOptions): Observable<T> {
    return this.http.patch<T>(this.resolve(path), body, this.toHttpOptions(options));
  }

  delete<T>(path: string, options?: ApiRequestOptions): Observable<T> {
    return this.http.delete<T>(this.resolve(path), this.toHttpOptions(options));
  }

  private resolve(path: string): string {
    return `${this.baseUrl}${path}`;
  }

  private toHttpOptions(options?: ApiRequestOptions): {
    context?: HttpContext;
    withCredentials?: boolean;
  } {
    return options ?? {};
  }
}
