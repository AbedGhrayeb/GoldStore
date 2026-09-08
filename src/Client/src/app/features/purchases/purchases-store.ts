import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  PurchasesApi,
  type CategoryResponse,
  type CreateCustomerPurchaseInput,
  type CustomerPurchaseInvoiceKpiResponse,
  type CustomerPurchaseInvoiceQuery,
  type CustomerPurchaseInvoiceResponse,
  type EmployeeResponse,
  type FinancialAccountResponse,
  type PaginatedCustomerPurchaseInvoices,
} from './purchases-api.service';

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the purchases feature (`feature: purchases`). Owns the paged
 * invoice list, the invoice KPIs, the invoice detail, the
 * create-invoice mutation, and the best-effort option lists (employees/categories/accounts)
 * the invoice dialog needs. Components consume only this store — never {@link PurchasesApi}
 * directly.
 */
@Injectable({ providedIn: 'root' })
export class PurchasesStore {
  private readonly api = inject(PurchasesApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly pageSignal = signal<PaginatedCustomerPurchaseInvoices | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);

  private readonly kpisSignal = signal<CustomerPurchaseInvoiceKpiResponse | null>(null);
  private readonly kpisLoadingSignal = signal(false);

  private readonly detailSignal = signal<CustomerPurchaseInvoiceResponse | null>(null);
  private readonly detailLoadingSignal = signal(false);
  private readonly detailErrorSignal = signal<ApiError | null>(null);

  private readonly employeesSignal = signal<EmployeeResponse[]>([]);
  private readonly employeesErrorSignal = signal<string | null>(null);
  private readonly employeesLoadedSignal = signal(false);

  private readonly categoriesSignal = signal<CategoryResponse[]>([]);
  private readonly categoriesErrorSignal = signal<string | null>(null);
  private readonly categoriesLoadedSignal = signal(false);

  private readonly accountsSignal = signal<FinancialAccountResponse[]>([]);
  private readonly accountsErrorSignal = signal<string | null>(null);
  private readonly accountsLoadedSignal = signal(false);

  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);

  private employeesPromise: Promise<void> | null = null;
  private categoriesPromise: Promise<void> | null = null;
  private accountsPromise: Promise<void> | null = null;

  readonly page = this.pageSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly kpis = this.kpisSignal.asReadonly();
  readonly kpisLoading = this.kpisLoadingSignal.asReadonly();
  readonly detail = this.detailSignal.asReadonly();
  readonly detailLoading = this.detailLoadingSignal.asReadonly();
  readonly detailError = this.detailErrorSignal.asReadonly();
  readonly employees = this.employeesSignal.asReadonly();
  readonly employeesError = this.employeesErrorSignal.asReadonly();
  readonly categories = this.categoriesSignal.asReadonly();
  readonly categoriesError = this.categoriesErrorSignal.asReadonly();
  readonly accounts = this.accountsSignal.asReadonly();
  readonly accountsError = this.accountsErrorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();

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

  async loadInvoices(query: CustomerPurchaseInvoiceQuery): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      this.pageSignal.set(
        await firstValueFrom(this.api.getInvoices(query, { context: PurchasesStore.NO_TOAST })),
      );
    } catch (error) {
      this.errorSignal.set(asApiError(error));
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async loadDetail(id: string): Promise<void> {
    this.detailLoadingSignal.set(true);
    this.detailErrorSignal.set(null);
    try {
      this.detailSignal.set(
        await firstValueFrom(this.api.getInvoice(id, { context: PurchasesStore.NO_TOAST })),
      );
    } catch (error) {
      this.detailErrorSignal.set(asApiError(error));
    } finally {
      this.detailLoadingSignal.set(false);
    }
  }

  clearDetail(): void {
    this.detailSignal.set(null);
    this.detailErrorSignal.set(null);
  }

  /**
   * Best-effort employee options for the invoice form. The endpoint is gated by `feature: hr`,
   * so a purchases-only user gets an empty list with an explanatory `employeesError` instead of
   * an error toast; the form degrades with an inline notice (the employee is required
   * server-side, so issuing a purchase genuinely needs the HR permission).
   */
  ensureEmployees(): Promise<void> {
    if (this.employeesLoadedSignal()) {
      return Promise.resolve();
    }
    this.employeesPromise ??= this.loadEmployees();
    return this.employeesPromise;
  }

  private async loadEmployees(): Promise<void> {
    try {
      this.employeesSignal.set(
        await firstValueFrom(this.api.getEmployees({ context: PurchasesStore.NO_TOAST })),
      );
    } catch {
      this.employeesErrorSignal.set(
        'تعذّر تحميل الموظفين (تحتاج صلاحية "الموظفون" لاختيار موظف الفاتورة).',
      );
    } finally {
      this.employeesLoadedSignal.set(true);
      this.employeesPromise = null;
    }
  }

  /**
   * Best-effort category options for invoice lines. Gated by `feature: catalog`; the field is
   * optional server-side (`categoryId` is nullable), so a purchases-only user simply gets no
   * categories and the lines are created without one.
   */
  ensureCategories(): Promise<void> {
    if (this.categoriesLoadedSignal()) {
      return Promise.resolve();
    }
    this.categoriesPromise ??= this.loadCategories();
    return this.categoriesPromise;
  }

  private async loadCategories(): Promise<void> {
    try {
      this.categoriesSignal.set(
        await firstValueFrom(this.api.getCategories({ context: PurchasesStore.NO_TOAST })),
      );
    } catch {
      this.categoriesErrorSignal.set(
        'تعذّر تحميل الأصناف (تحتاج صلاحية "الكتالوج" لاختيار الصنف لكل بند).',
      );
    } finally {
      this.categoriesLoadedSignal.set(true);
      this.categoriesPromise = null;
    }
  }

  /**
   * Best-effort account options for the payment method / legs. Gated by `feature: finance`;
   * the API requires an account even for cash, so a purchases-only user sees an inline notice
   * and cannot issue the invoice without the finance permission.
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
        await firstValueFrom(this.api.getAccounts({ context: PurchasesStore.NO_TOAST })),
      );
    } catch {
      this.accountsErrorSignal.set(
        'تعذّر تحميل الحسابات المالية (تحتاج صلاحية "المالية" لاختيار حساب الدفع أو الدفع بعملات متعددة).',
      );
    } finally {
      this.accountsLoadedSignal.set(true);
      this.accountsPromise = null;
    }
  }

  /**
   * Forces a reload of the cached dropdown options (employees/categories/accounts) so that
   * returning to the page always shows freshly created options without a hard refresh.
   */
  async refreshOptions(): Promise<void> {
    this.employeesLoadedSignal.set(false);
    this.categoriesLoadedSignal.set(false);
    this.accountsLoadedSignal.set(false);
    await Promise.allSettled([
      this.ensureEmployees(),
      this.ensureCategories(),
      this.ensureAccounts(),
    ]);
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createInvoice(input: CreateCustomerPurchaseInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.createInvoice(input, { context: PurchasesStore.NO_TOAST }));
      this.toasts.success('تم إصدار فاتورة شراء الذهب بنجاح');
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }
}
