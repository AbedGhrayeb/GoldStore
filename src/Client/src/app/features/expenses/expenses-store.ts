import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  ExpensesApi,
  type CreateExpenseCategoryInput,
  type CreateExpenseInput,
  type ExpenseCategoryResponse,
  type ExpenseKpiResponse,
  type ExpensesQuery,
  type FinancialAccountResponse,
  type PaginatedExpenses,
  type UpdateExpenseCategoryInput,
  type UpdateExpenseInput,
} from './expenses-api.service';

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the expenses feature (`feature: expenses`). Owns the paged
 * expenses table, the expense KPIs, the categories list, the create/update/delete mutations
 * for both, and the best-effort account options the expense dialog needs. Expenses create a
 * financial OUT entry; deleting reverses it, so every successful mutation is followed by a
 * page-level reload. Components consume only this store — never {@link ExpensesApi} directly.
 */
@Injectable({ providedIn: 'root' })
export class ExpensesStore {
  private readonly api = inject(ExpensesApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly pageSignal = signal<PaginatedExpenses | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);

  private readonly kpisSignal = signal<ExpenseKpiResponse | null>(null);
  private readonly kpisLoadingSignal = signal(false);

  private readonly categoriesSignal = signal<ExpenseCategoryResponse[] | null>(null);
  private readonly categoriesLoadingSignal = signal(false);
  private readonly categoriesErrorSignal = signal<ApiError | null>(null);
  private readonly categoriesLoadedSignal = signal(false);

  private readonly accountsSignal = signal<FinancialAccountResponse[]>([]);
  private readonly accountsErrorSignal = signal<string | null>(null);
  private readonly accountsLoadedSignal = signal(false);

  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);

  private categoriesPromise: Promise<void> | null = null;
  private accountsPromise: Promise<void> | null = null;

  readonly page = this.pageSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly kpis = this.kpisSignal.asReadonly();
  readonly kpisLoading = this.kpisLoadingSignal.asReadonly();
  readonly categories = this.categoriesSignal.asReadonly();
  readonly categoriesLoading = this.categoriesLoadingSignal.asReadonly();
  readonly categoriesError = this.categoriesErrorSignal.asReadonly();
  readonly accounts = this.accountsSignal.asReadonly();
  readonly accountsError = this.accountsErrorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();

  async loadExpenses(query: ExpensesQuery): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      this.pageSignal.set(
        await firstValueFrom(this.api.getExpenses(query, { context: ExpensesStore.NO_TOAST })),
      );
    } catch (error) {
      this.errorSignal.set(asApiError(error));
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async loadKpis(): Promise<void> {
    this.kpisLoadingSignal.set(true);
    try {
      this.kpisSignal.set(await firstValueFrom(this.api.getKpis()));
    } catch {
      // KPIs are cosmetic; the table remains the source of truth.
    } finally {
      this.kpisLoadingSignal.set(false);
    }
  }

  async loadCategories(): Promise<void> {
    this.categoriesLoadingSignal.set(true);
    this.categoriesErrorSignal.set(null);
    try {
      this.categoriesSignal.set(
        await firstValueFrom(
          this.api.getCategories(false, { context: ExpensesStore.NO_TOAST }),
        ),
      );
      this.categoriesLoadedSignal.set(true);
    } catch (error) {
      this.categoriesErrorSignal.set(asApiError(error));
    } finally {
      this.categoriesLoadingSignal.set(false);
    }
  }

  /**
   * Best-effort categories — the list is gated by `feature: expenses` but is already loaded
   * for the table; keep the ensure path so dialogs do not re-fire unnecessarily.
   */
  ensureCategories(): Promise<void> {
    if (this.categoriesLoadedSignal()) {
      return Promise.resolve();
    }
    this.categoriesPromise ??= this.loadCategories().then(() => undefined);
    return this.categoriesPromise;
  }

  /**
   * Best-effort account options for the expense form. Gated by `feature: finance` — a
   * expenses-only user gets an empty list with an explanatory `accountsError` instead of an
   * error toast; the form degrades gracefully with an inline notice.
   */
  ensureAccounts(): Promise<void> {
    if (this.accountsLoadedSignal()) {
      return Promise.resolve();
    }
    this.accountsPromise ??= this.loadAccounts();
    return this.accountsPromise;
  }

  private async loadAccounts(): Promise<void> {
    try {
      this.accountsSignal.set(
        await firstValueFrom(this.api.getAccounts({ context: ExpensesStore.NO_TOAST })),
      );
    } catch {
      this.accountsErrorSignal.set(
        'تعذّر تحميل الحسابات المالية (تحتاج صلاحية "المالية" لاختيار حساب المصروف).',
      );
    } finally {
      this.accountsLoadedSignal.set(true);
      this.accountsPromise = null;
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createExpense(input: CreateExpenseInput): Promise<boolean> {
    return this.mutate((options) => this.api.createExpense(input, options), 'تم تسجيل المصروف بنجاح');
  }

  async updateExpense(id: string, input: UpdateExpenseInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.updateExpense(id, input, options),
      'تم تعديل المصروف بنجاح',
    );
  }

  async deleteExpense(id: string): Promise<boolean> {
    return this.mutate(
      (options) => this.api.deleteExpense(id, options),
      'تم حذف المصروف بنجاح',
    );
  }

  async createCategory(input: CreateExpenseCategoryInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.createCategory(input, options),
      'تم إنشاء التصنيف بنجاح',
    );
  }

  async updateCategory(id: string, input: UpdateExpenseCategoryInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.updateCategory(id, input, options),
      'تم تعديل التصنيف بنجاح',
    );
  }

  async deleteCategory(id: string): Promise<boolean> {
    return this.mutate(
      (options) => this.api.deleteCategory(id, options),
      'تم حذف التصنيف بنجاح',
    );
  }

  private async mutate(
    call: (options: { context: HttpContext }) => import('rxjs').Observable<unknown>,
    successMessage: string,
  ): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(call({ context: ExpensesStore.NO_TOAST }));
      this.toasts.success(successMessage);
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }
}
