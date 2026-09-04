import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  UsersApi,
  type CreateUserInput,
  type UpdateUserInput,
  type UserResponse,
} from './users-api.service';

export { type UserResponse };

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the users group (feature: settings). Owns the user list, the
 * current user's profile (`/users/me`) and the create/update/delete mutations. The server
 * only allows editing your own profile (`UpdateUserCommandHandler`), so the page exposes
 * edit for the current user only; create/delete apply to any tenant user.
 */
@Injectable({ providedIn: 'root' })
export class UsersStore {
  private readonly api = inject(UsersApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly usersSignal = signal<UserResponse[] | null>(null);
  private readonly meSignal = signal<UserResponse | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);
  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);
  private readonly mutatingIdSignal = signal<string | null>(null);
  private loadPromise: Promise<void> | null = null;

  readonly users = this.usersSignal.asReadonly();
  readonly me = this.meSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();
  readonly mutatingId = this.mutatingIdSignal.asReadonly();

  /** Loads the user list once per session (single-flight). Profile is best-effort. */
  ensureLoaded(): Promise<void> {
    if (this.usersSignal() !== null) {
      return Promise.resolve();
    }
    this.loadPromise ??= this.load();
    return this.loadPromise;
  }

  async load(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const [users, me] = await Promise.all([
        firstValueFrom(this.api.getUsers({ context: UsersStore.NO_TOAST })),
        firstValueFrom(this.api.getMe({ context: UsersStore.NO_TOAST })).catch(() => null),
      ]);
      this.usersSignal.set(users);
      this.meSignal.set(me);
    } catch (error) {
      this.errorSignal.set(asApiError(error));
    } finally {
      this.loadingSignal.set(false);
      this.loadPromise = null;
    }
  }

  /** Reloads only the profile (`/users/me`) — used after editing the current user. */
  async reloadMe(): Promise<void> {
    try {
      this.meSignal.set(await firstValueFrom(this.api.getMe({ context: UsersStore.NO_TOAST })));
    } catch {
      // Profile is cosmetic; keep the stale value.
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createUser(input: CreateUserInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.createUser(input, { context: UsersStore.NO_TOAST }));
      this.toasts.success('تمت إضافة المستخدم بنجاح');
      await this.load();
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async updateUser(id: string, input: UpdateUserInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.updateUser(id, input, { context: UsersStore.NO_TOAST }));
      this.toasts.success('تم تحديث بياناتك بنجاح');
      await this.load();
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async deleteUser(id: string): Promise<void> {
    this.mutatingIdSignal.set(id);
    try {
      await firstValueFrom(this.api.deleteUser(id));
      this.toasts.success('تم حذف المستخدم بنجاح');
      await this.load();
    } catch {
      // The global error interceptor already toasted the failure.
    } finally {
      this.mutatingIdSignal.set(null);
    }
  }
}
