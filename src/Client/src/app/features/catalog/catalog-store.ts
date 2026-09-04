import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import { CatalogApi, type CategoryInput, type CategoryResponse } from './catalog-api.service';

export { type CategoryResponse };

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the categories group (feature: catalog). Owns the category list
 * (tree-flattened in the page) plus the create/update/toggle/delete mutations. Create/update
 * errors are surfaced inline in the form dialog (`saveError`); toggle/delete failures use the
 * global error toast.
 */
@Injectable({ providedIn: 'root' })
export class CatalogStore {
  private readonly api = inject(CatalogApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly categoriesSignal = signal<CategoryResponse[] | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);
  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);
  private readonly mutatingIdSignal = signal<string | null>(null);
  private loadPromise: Promise<void> | null = null;

  readonly categories = this.categoriesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();
  readonly mutatingId = this.mutatingIdSignal.asReadonly();

  /** Loads the category list once per session (single-flight). */
  ensureLoaded(): Promise<void> {
    if (this.categoriesSignal() !== null) {
      return Promise.resolve();
    }
    this.loadPromise ??= this.load();
    return this.loadPromise;
  }

  async load(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const categories = await firstValueFrom(
        this.api.getCategories({ context: CatalogStore.NO_TOAST }),
      );
      this.categoriesSignal.set(categories);
    } catch (error) {
      this.errorSignal.set(asApiError(error));
    } finally {
      this.loadingSignal.set(false);
      this.loadPromise = null;
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createCategory(input: CategoryInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.createCategory(input, { context: CatalogStore.NO_TOAST }));
      this.toasts.success('تمت إضافة التصنيف بنجاح');
      await this.load();
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async updateCategory(id: string, input: CategoryInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.updateCategory(id, input, { context: CatalogStore.NO_TOAST }));
      this.toasts.success('تم تحديث التصنيف بنجاح');
      await this.load();
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async toggleActive(id: string): Promise<void> {
    this.mutatingIdSignal.set(id);
    try {
      await firstValueFrom(this.api.toggleActive(id));
      this.toasts.success('تم تحديث حالة التصنيف');
      await this.load();
    } catch {
      // The global error interceptor already toasted the failure.
    } finally {
      this.mutatingIdSignal.set(null);
    }
  }

  async deleteCategory(id: string): Promise<void> {
    this.mutatingIdSignal.set(id);
    try {
      await firstValueFrom(this.api.deleteCategory(id));
      this.toasts.success('تم حذف التصنيف بنجاح');
      await this.load();
    } catch {
      // The global error interceptor already toasted the failure.
    } finally {
      this.mutatingIdSignal.set(null);
    }
  }
}
