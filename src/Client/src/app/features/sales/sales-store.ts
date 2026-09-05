import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  SalesApi,
  type CategoryResponse,
  type CreateSalesInvoiceInput,
  type EmployeeResponse,
  type FinancialAccountResponse,
  type PaginatedSalesInvoices,
  type SalesInvoiceKpiResponse,
  type SalesInvoiceQuery,
  type SalesInvoiceResponse,
} from './sales-api.service';

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the sales feature (`feature: sales`). Owns the paged invoice list,
 * the invoice KPIs, the next-number prefill, the invoice detail, the create-invoice mutation,
 * and the best-effort option lists (employees/categories/accounts) the invoice dialog needs.
 * Components consume only this store — never {@link SalesApi} directly.
 */
@Injectable({ providedIn: 'root' })
export class SalesStore {
  private readonly api = inject(SalesApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly pageSignal = signal<PaginatedSalesInvoices | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);

  private readonly kpisSignal = signal<SalesInvoiceKpiResponse | null>(null);
  private readonly kpisLoadingSignal = signal(false);

  private readonly detailSignal = signal<SalesInvoiceResponse | null>(null);
  private readonly detailLoadingSignal = signal(false);
  private readonly detailErrorSignal = signal<ApiError | null>(null);

  private readonly nextNumberSignal = signal('');
  private readonly nextNumberErrorSignal = signal(false);

  private readonly employeesSignal = signal<EmployeeResponse[]>([]);
  private readonly employeesErrorSignal = signal<string | null>(null);

  private readonly categoriesSignal = signal<CategoryResponse[]>([]);
  private readonly categoriesErrorSignal = signal<string | null>(null);

  private readonly accountsSignal = signal<FinancialAccountResponse[]>([]);
  private readonly accountsErrorSignal = signal<string | null>(null);

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
  readonly nextNumber = this.nextNumberSignal.asReadonly();
  readonly nextNumberError = this.nextNumberErrorSignal.asReadonly();
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

  async loadInvoices(query: SalesInvoiceQuery): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      this.pageSignal.set(
        await firstValueFrom(this.api.getInvoices(query, { context: SalesStore.NO_TOAST })),
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
        await firstValueFrom(this.api.getInvoice(id, { context: SalesStore.NO_TOAST })),
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

  async loadNextNumber(): Promise<void> {
    this.nextNumberErrorSignal.set(false);
    try {
      this.nextNumberSignal.set(await firstValueFrom(this.api.getNextNumber()));
    } catch {
      this.nextNumberSignal.set('');
      this.nextNumberErrorSignal.set(true);
    }
  }

  /**
   * Best-effort employee options for the invoice form. The endpoint is gated by `feature: hr`,
   * so a sales-only user gets an empty list with an explanatory `employeesError` instead of an
   * error toast; the dialog degrades with an inline notice (the seller is required server-side,
   * so creating an invoice genuinely needs the HR permission).
   */
  ensureEmployees(): Promise<void> {
    if (this.employeesSignal().length > 0) return Promise.resolve();
    if (this.employeesPromise) return this.employeesPromise;
    this.employeesErrorSignal.set(null);
    this.employeesPromise = this.loadEmployees();
    return this.employeesPromise;
  }

  private async loadEmployees(): Promise<void> {
    try {
      this.employeesSignal.set(
        await firstValueFrom(this.api.getEmployees({ context: SalesStore.NO_TOAST })),
      );
    } catch {
      this.employeesErrorSignal.set(
        'تعذّر تحميل الموظفين (تحتاج صلاحية "الموظفون" لاختيار بائع الفاتورة).',
      );
    } finally {
      this.employeesPromise = null;
    }
  }

  /**
   * Best-effort category options for invoice lines. Gated by `feature: catalog`; the field is
   * optional server-side (`categoryId` is nullable), so a sales-only user simply gets no
   * categories and the lines are created without one.
   */
  ensureCategories(): Promise<void> {
    if (this.categoriesSignal().length > 0) return Promise.resolve();
    if (this.categoriesPromise) return this.categoriesPromise;
    this.categoriesErrorSignal.set(null);
    this.categoriesPromise = this.loadCategories();
    return this.categoriesPromise;
  }

  private async loadCategories(): Promise<void> {
    try {
      this.categoriesSignal.set(
        await firstValueFrom(this.api.getCategories({ context: SalesStore.NO_TOAST })),
      );
    } catch {
      this.categoriesErrorSignal.set(
        'تعذّر تحميل الأصناف (تحتاج صلاحية "الكتالوج" لاختيار الصنف لكل بند).',
      );
    } finally {
      this.categoriesPromise = null;
    }
  }

  /**
   * Best-effort account options for the payment method / legs. Gated by `feature: finance`;
   * a sales-only user sees an inline notice and can still use the cash method with no account.
   */
  ensureAccounts(): Promise<void> {
    if (this.accountsSignal().length > 0) return Promise.resolve();
    if (this.accountsPromise) return this.accountsPromise;
    this.accountsErrorSignal.set(null);
    this.accountsPromise = this.loadAccounts();
    return this.accountsPromise;
  }

  private async loadAccounts(): Promise<void> {
    try {
      this.accountsSignal.set(
        await firstValueFrom(this.api.getAccounts({ context: SalesStore.NO_TOAST })),
      );
    } catch {
      this.accountsErrorSignal.set(
        'تعذّر تحميل الحسابات المالية (تحتاج صلاحية "المالية" للتحويل البنكي أو الدفع بعملات متعددة).',
      );
    } finally {
      this.accountsPromise = null;
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createInvoice(input: CreateSalesInvoiceInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.createInvoice(input, { context: SalesStore.NO_TOAST }));
      this.toasts.success('تم إصدار الفاتورة بنجاح');
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }
}