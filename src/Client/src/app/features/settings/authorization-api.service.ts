import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';

export interface RoleResponse {
  id: string;
  key: string;
  name: string;
  permissionKeys: string[];
}

export interface PermissionResponse {
  id: string;
  key: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class AuthorizationApi {
  private readonly api = inject(ApiClient);

  getRoles(options?: ApiRequestOptions): Observable<RoleResponse[]> {
    return this.api.get<RoleResponse[]>('/api/v1/roles', options);
  }

  getPermissions(options?: ApiRequestOptions): Observable<PermissionResponse[]> {
    return this.api.get<PermissionResponse[]>('/api/v1/permissions', options);
  }

  getUserRoles(userId: string, options?: ApiRequestOptions): Observable<string[]> {
    return this.api.get<string[]>(`/api/v1/users/${encodeURIComponent(userId)}/roles`, options);
  }

  setUserRoles(userId: string, roleIds: string[], options?: ApiRequestOptions): Observable<boolean> {
    return this.api.put<boolean>(`/api/v1/users/${encodeURIComponent(userId)}/roles`, { roleIds }, options);
  }

  getUserPermissions(userId: string, options?: ApiRequestOptions): Observable<string[]> {
    return this.api.get<string[]>(`/api/v1/users/${encodeURIComponent(userId)}/permissions`, options);
  }

  setUserPermissions(userId: string, permissionKeys: string[], options?: ApiRequestOptions): Observable<boolean> {
    return this.api.put<boolean>(`/api/v1/users/${encodeURIComponent(userId)}/permissions`, { permissionKeys }, options);
  }
}
