import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  SuppliersApi,
  type DeliveryInput,
  type FinancialAccountResponse,
  type FinancialTransactionInput,
  type ManufacturingPaymentInput,
  type PagedSupplierFinancialTransactionResponse,
  type ScrapGoldPaymentInput,
  type SupplierDetailResponse,
  type SupplierFinancialKpiResponse,
  type SupplierFinancialPaymentResponse,
  type SupplierInput,
  type SupplierFinancialPaymentInput,
  type SupplierResponse,
  type SupplierTransactionQuery,
} from './suppliers-api.service';

export type { SupplierResponse };

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the suppliers feature (`feature: suppliers`). Owns the supplier
 * list, the supplier financial KPIs, the paged financial-transactions table, the supplier
 * detail (balances + recent transactions), the payments of one transaction, and the
 * best-effort account options for the payment forms. Components consume only this store —
 * never {@link SuppliersApi} directly.
 */
@Injectable({ providedIn: 'root' })
export class SuppliersStore {
  private readonly api = inject(SuppliersApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly suppliersSignal = signal<SupplierResponse[] | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);

  private readonly kpisSignal = signal<SupplierFinancialKpiResponse | null>(null);
  private readonly kpisLoadingSignal = signal(false);

  private readonly pageSignal = signal<PagedSupplierFinancialTransactionResponse | null>(null);
  private readonly tableLoadingSignal = signal(false);
  private readonly tableErrorSignal = signal<ApiError | null>(null);

  private readonly detailSignal = signal<SupplierDetailResponse | null>(null);
  private readonly detailLoadingSignal = signal(false);
  private readonly detailErrorSignal = signal<ApiError | null>(null);

  private readonly paymentsSignal = signal<SupplierFinancialPaymentResponse[]>([]);
  private readonly paymentsLoadingSignal = signal(false);
  private readonly paymentsErrorSignal = signal<ApiError | null>(null);

  private readonly accountsSignal = signal<FinancialAccountResponse[]>([]);
  private readonly accountsErrorSignal = signal<string | null>(null);

  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);
  private readonly mutatingIdSignal = signal<string | null>(null);
  private loadPromise: Promise<void> | null = null;
  private accountsPromise: Promise<void> | null = null;

  readonly suppliers = this.suppliersSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly kpis = this.kpisSignal.asReadonly();
  readonly kpisLoading = this.kpisLoadingSignal.asReadonly();
  readonly page = this.pageSignal.asReadonly();
  readonly tableLoading = this.tableLoadingSignal.asReadonly();
  readonly tableError = this.tableErrorSignal.asReadonly();
  readonly detail = this.detailSignal.asReadonly();
  readonly detailLoading = this.detailLoadingSignal.asReadonly();
  readonly detailError = this.detailErrorSignal.asReadonly();
  readonly payments = this.paymentsSignal.asReadonly();
  readonly paymentsLoading = this.paymentsLoadingSignal.asReadonly();
  readonly paymentsError = this.paymentsErrorSignal.asReadonly();
  readonly accounts = this.accountsSignal.asReadonly();
  readonly accountsError = this.accountsErrorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();
  readonly mutatingId = this.mutatingIdSignal.asReadonly();

  /** Loads the supplier list once per session (single-flight). */
  ensureLoaded(): Promise<void> {
    if (this.suppliersSignal() !== null) {
      return Promise.resolve();
    }
    this.loadPromise ??= this.load();
    return this.loadPromise;
  }

  async load(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const suppliers = await firstValueFrom(
        this.api.getSuppliers({ context: SuppliersStore.NO_TOAST }),
      );
      this.suppliersSignal.set(suppliers);
    } catch (error) {
      this.errorSignal.set(asApiError(error));
    } finally {
      this.loadingSignal.set(false);
      this.loadPromise = null;
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

  async loadTransactions(query: SupplierTransactionQuery): Promise<void> {
    this.tableLoadingSignal.set(true);
    this.tableErrorSignal.set(null);
    try {
      this.pageSignal.set(
        await firstValueFrom(
          this.api.getTransactions(query, { context: SuppliersStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.tableErrorSignal.set(asApiError(error));
    } finally {
      this.tableLoadingSignal.set(false);
    }
  }

  async loadDetail(id: string): Promise<void> {
    this.detailLoadingSignal.set(true);
    this.detailErrorSignal.set(null);
    try {
      this.detailSignal.set(
        await firstValueFrom(
          this.api.getSupplier(id, { context: SuppliersStore.NO_TOAST }),
        ),
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

  async loadPayments(transactionId: string): Promise<void> {
    this.paymentsLoadingSignal.set(true);
    this.paymentsErrorSignal.set(null);
    try {
      this.paymentsSignal.set(
        await firstValueFrom(
          this.api.getPayments(transactionId, { context: SuppliersStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.paymentsErrorSignal.set(asApiError(error));
    } finally {
      this.paymentsLoadingSignal.set(false);
    }
  }

  clearPayments(): void {
    this.paymentsSignal.set([]);
    this.paymentsErrorSignal.set(null);
  }

  /**
   * Best-effort fetch of financial accounts for the payment forms. The endpoint is gated by
   * `feature: finance`, so a suppliers-only user gets an empty list with an explanatory
   * `accountsError` instead of an error toast; the dialogs degrade with an inline notice.
   */
  ensureAccounts(): Promise<void> {
    if (this.accountsSignal().length > 0 || this.accountsErrorSignal() !== null) {
      return Promise.resolve();
    }
    this.accountsPromise ??= this.loadAccounts();
    return this.accountsPromise;
  }

  private async loadAccounts(): Promise<void> {
    try {
      this.accountsSignal.set(
        await firstValueFrom(
          this.api.getAccounts({ context: SuppliersStore.NO_TOAST }),
        ),
      );
    } catch {
      this.accountsErrorSignal.set(
        'تعذّر تحميل الحسابات المالية (تحتاج صلاحية "المالية" لدفع التصنيع والمعاملات المالية).',
      );
    } finally {
      this.accountsPromise = null;
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createSupplier(input: SupplierInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.createSupplier(input, { context: SuppliersStore.NO_TOAST }));
      this.toasts.success('تمت إضافة المورد بنجاح');
      await this.load();
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async updateSupplier(id: string, input: SupplierInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.updateSupplier(id, input, { context: SuppliersStore.NO_TOAST }));
      this.toasts.success('تم تحديث المورد بنجاح');
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
      this.toasts.success('تم تحديث حالة المورد');
      await this.load();
    } catch {
      // The global error interceptor already toasted the failure.
    } finally {
      this.mutatingIdSignal.set(null);
    }
  }

  async createDelivery(input: DeliveryInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(this.api.createDelivery(input, { context: SuppliersStore.NO_TOAST }));
      this.toasts.success('تم تسجيل التسليم بنجاح');
      await this.load();
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async createScrapGoldPayment(input: ScrapGoldPaymentInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(
        this.api.createScrapGoldPayment(input, { context: SuppliersStore.NO_TOAST }),
      );
      this.toasts.success('تم تسجيل دفعة الخردة بنجاح');
      await this.load();
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async createManufacturingPayment(input: ManufacturingPaymentInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(
        this.api.createManufacturingPayment(input, { context: SuppliersStore.NO_TOAST }),
      );
      this.toasts.success('تم تسجيل دفعة التصنيع بنجاح');
      await this.load();
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async createTransaction(input: FinancialTransactionInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(
        this.api.createTransaction(input, { context: SuppliersStore.NO_TOAST }),
      );
      this.toasts.success('تمت إضافة المعاملة المالية بنجاح');
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async createPayment(transactionId: string, input: SupplierFinancialPaymentInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(
        this.api.createPayment(transactionId, input, { context: SuppliersStore.NO_TOAST }),
      );
      this.toasts.success('تم تسجيل الدفعة بنجاح');
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }
}