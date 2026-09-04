import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import { UsersApi, type UserResponse } from './users-api.service';
import { AuthorizationApi, type PermissionResponse, type RoleResponse } from './authorization-api.service';

function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

@Injectable({ providedIn: 'root' })
export class PermissionsStore {
  private readonly usersApi = inject(UsersApi);
  private readonly authApi = inject(AuthorizationApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly usersSignal = signal<UserResponse[] | null>(null);
  private readonly rolesSignal = signal<RoleResponse[] | null>(null);
  private readonly permissionsSignal = signal<PermissionResponse[] | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);
  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);

  // per-user role cache
  private readonly userRolesCache = new Map<string, string[]>();
  private readonly userPermissionsCache = new Map<string, string[]>();

  readonly users = this.usersSignal.asReadonly();
  readonly roles = this.rolesSignal.asReadonly();
  readonly permissions = this.permissionsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();

  async load(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const [users, roles, permissions] = await Promise.all([
        firstValueFrom(this.usersApi.getUsers({ context: PermissionsStore.NO_TOAST })),
        firstValueFrom(this.authApi.getRoles({ context: PermissionsStore.NO_TOAST })),
        firstValueFrom(this.authApi.getPermissions({ context: PermissionsStore.NO_TOAST })),
      ]);
      this.usersSignal.set(users);
      this.rolesSignal.set(roles);
      this.permissionsSignal.set(permissions);
    } catch (error) {
      this.errorSignal.set(asApiError(error));
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async getUserRoles(userId: string): Promise<string[]> {
    if (this.userRolesCache.has(userId)) return this.userRolesCache.get(userId) ?? [];
    try {
      const roles = await firstValueFrom(this.authApi.getUserRoles(userId, { context: PermissionsStore.NO_TOAST }));
      this.userRolesCache.set(userId, roles);
      return roles;
    } catch (error) {
      throw asApiError(error);
    }
  }

  async getUserPermissions(userId: string): Promise<string[]> {
    if (this.userPermissionsCache.has(userId)) return this.userPermissionsCache.get(userId) ?? [];
    try {
      const perms = await firstValueFrom(this.authApi.getUserPermissions(userId, { context: PermissionsStore.NO_TOAST }));
      this.userPermissionsCache.set(userId, perms);
      return perms;
    } catch (error) {
      throw asApiError(error);
    }
  }

  clearCache(): void {
    this.userRolesCache.clear();
    this.userPermissionsCache.clear();
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async setUserRoles(userId: string, roleIds: string[]): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.authApi.setUserRoles(userId, roleIds, { context: PermissionsStore.NO_TOAST }));
      this.userRolesCache.set(userId, roleIds);
      this.toasts.success('تم تحديث الصلاحيات بنجاح');
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async setUserPermissions(userId: string, permissionKeys: string[]): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.authApi.setUserPermissions(userId, permissionKeys, { context: PermissionsStore.NO_TOAST }));
      this.userPermissionsCache.set(userId, permissionKeys);
      this.toasts.success('تم تحديث الصلاحيات المباشرة بنجاح');
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }
}
