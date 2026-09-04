import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type UserResponse = components['schemas']['UserResponse'];
export type CreateUserRequest = components['schemas']['CreateUserRequest'];
export type UpdateUserRequest = components['schemas']['UpdateUserRequest'];

/** Payload for creating a user (schema shape; every field is required). */
export interface CreateUserInput {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  phoneNumber?: string | null;
  whatsappNumber?: string | null;
}

/** Payload for updating a user; a blank password means "keep the current one". */
export interface UpdateUserInput {
  firstName: string;
  lastName: string;
  password: string | null;
  phoneNumber?: string | null;
  whatsappNumber?: string | null;
}

/**
 * Typed transport for the users group (`/api/v1/users`, feature: settings). The tenant
 * always comes from the HttpOnly-session claims server-side — no payload here carries a
 * `tenantId`. Components never call this directly — they go through {@link UsersStore}.
 */
@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly api = inject(ApiClient);
  private static readonly base = '/api/v1/users';

  getUsers(options?: ApiRequestOptions): Observable<UserResponse[]> {
    return this.api.get<UserResponse[]>(UsersApi.base, options);
  }

  getMe(options?: ApiRequestOptions): Observable<UserResponse> {
    return this.api.get<UserResponse>(`${UsersApi.base}/me`, options);
  }

  createUser(input: CreateUserInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(UsersApi.base, input, options);
  }

  updateUser(id: string, input: UpdateUserInput, options?: ApiRequestOptions): Observable<boolean> {
    return this.api.put<boolean>(`${UsersApi.base}/${encodeURIComponent(id)}`, input, options);
  }

  deleteUser(id: string, options?: ApiRequestOptions): Observable<boolean> {
    return this.api.delete<boolean>(`${UsersApi.base}/${encodeURIComponent(id)}`, options);
  }
}
