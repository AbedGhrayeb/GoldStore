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
  type PagedSuppliersResponse,
  type ScrapGoldPaymentInput,
  type SupplierBalancesResponse,
  type SupplierDetailResponse,
  type PagedSupplierTransactionsResponse,
  type SupplierDetailTransactionsQuery,
  type SupplierFinancialKpiResponse,
  type SupplierFinancialPaymentResponse,
  type SupplierInput,
  type SupplierFinancialPaymentInput,
  type SupplierListQuery,
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

  private readonly suppliersPageSignal = signal<PagedSuppliersResponse | null>(null);
  private readonly suppliersPageLoadingSignal = signal(false);
  private readonly suppliersPageErrorSignal = signal<ApiError | null>(null);
  private readonly lastSuppliersPageQuerySignal = signal<SupplierListQuery | null>(null);

  private readonly kpisSignal = signal<SupplierFinancialKpiResponse | null>(null);
  private readonly kpisLoadingSignal = signal(false);

  private readonly pageSignal = signal<PagedSupplierFinancialTransactionResponse | null>(null);
  private readonly tableLoadingSignal = signal(false);
  private readonly tableErrorSignal = signal<ApiError | null>(null);

  private readonly detailSignal = signal<SupplierDetailResponse | null>(null);
  private readonly detailLoadingSignal = signal(false);
  private readonly detailErrorSignal = signal<ApiError | null>(null);

  private readonly detailTransactionsSignal = signal<PagedSupplierTransactionsResponse | null>(
    null,
  );
  private readonly detailTransactionsLoadingSignal = signal(false);
  private readonly detailTransactionsErrorSignal = signal<ApiError | null>(null);

  private readonly paymentsSignal = signal<SupplierFinancialPaymentResponse[]>([]);
  private readonly paymentsLoadingSignal = signal(false);
  private readonly paymentsErrorSignal = signal<ApiError | null>(null);

  private readonly balancesSignal = signal<SupplierBalancesResponse | null>(null);
  private readonly balancesForSupplierSignal = signal<string | null>(null);

  private readonly accountsSignal = signal<FinancialAccountResponse[]>([]);
  private readonly accountsErrorSignal = signal<string | null>(null);

  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);
  private readonly mutatingIdSignal = signal<string | null>(null);
  private loadPromise: Promise<void> | null = null;
  private accountsPromise: Promise<void> | null = null;
  private balancesPromise: Promise<void> | null = null;
  private balancesWanted: string | null = null;

  readonly suppliers = this.suppliersSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly suppliersPage = this.suppliersPageSignal.asReadonly();
  readonly suppliersPageLoading = this.suppliersPageLoadingSignal.asReadonly();
  readonly suppliersPageError = this.suppliersPageErrorSignal.asReadonly();
  readonly kpis = this.kpisSignal.asReadonly();
  readonly kpisLoading = this.kpisLoadingSignal.asReadonly();
  readonly page = this.pageSignal.asReadonly();
  readonly tableLoading = this.tableLoadingSignal.asReadonly();
  readonly tableError = this.tableErrorSignal.asReadonly();
  readonly detail = this.detailSignal.asReadonly();
  readonly detailLoading = this.detailLoadingSignal.asReadonly();
  readonly detailError = this.detailErrorSignal.asReadonly();
  readonly detailTransactions = this.detailTransactionsSignal.asReadonly();
  readonly detailTransactionsLoading = this.detailTransactionsLoadingSignal.asReadonly();
  readonly detailTransactionsError = this.detailTransactionsErrorSignal.asReadonly();
  readonly payments = this.paymentsSignal.asReadonly();
  readonly paymentsLoading = this.paymentsLoadingSignal.asReadonly();
  readonly paymentsError = this.paymentsErrorSignal.asReadonly();
  readonly supplierBalances = this.balancesSignal.asReadonly();
  readonly supplierBalancesFor = this.balancesForSupplierSignal.asReadonly();
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

  /** Server-side paged supplier list for the suppliers tab (search + status filter). */
  async loadSuppliersPage(query: SupplierListQuery): Promise<void> {
    this.lastSuppliersPageQuerySignal.set(query);
    this.suppliersPageLoadingSignal.set(true);
    this.suppliersPageErrorSignal.set(null);
    try {
      this.suppliersPageSignal.set(
        await firstValueFrom(
          this.api.getSuppliersPaged(query, { context: SuppliersStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.suppliersPageErrorSignal.set(asApiError(error));
    } finally {
      this.suppliersPageLoadingSignal.set(false);
    }
  }

  /** Reloads the full list (selects) plus the active suppliers page, if any. */
  private async refreshSuppliers(): Promise<void> {
    await this.load();
    const query = this.lastSuppliersPageQuerySignal();
    if (query !== null) {
      await this.loadSuppliersPage(query);
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
        await firstValueFrom(this.api.getTransactions(query, { context: SuppliersStore.NO_TOAST })),
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
        await firstValueFrom(this.api.getSupplier(id, { context: SuppliersStore.NO_TOAST })),
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
    this.detailTransactionsSignal.set(null);
    this.detailTransactionsErrorSignal.set(null);
  }

  async loadDetailTransactions(id: string, query: SupplierDetailTransactionsQuery): Promise<void> {
    this.detailTransactionsLoadingSignal.set(true);
    this.detailTransactionsErrorSignal.set(null);
    try {
      this.detailTransactionsSignal.set(
        await firstValueFrom(
          this.api.getSupplierTransactions(id, query, { context: SuppliersStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.detailTransactionsErrorSignal.set(asApiError(error));
    } finally {
      this.detailTransactionsLoadingSignal.set(false);
    }
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
   * Best-effort fetch of one supplier's payment dues (gold per karat, manufacturing per
   * currency) for the payment dialogs. Cached per supplier and cleared after a payment is
   * saved. Race-safe: a response is applied only when its supplier is still wanted.
   */
  ensureSupplierBalances(supplierId: string): Promise<void> {
    if (supplierId === '') {
      return Promise.resolve();
    }
    this.balancesWanted = supplierId;
    if (this.balancesForSupplierSignal() === supplierId) {
      return Promise.resolve();
    }
    this.balancesPromise ??= this.loadSupplierBalances(supplierId);
    return this.balancesPromise;
  }

  clearSupplierBalances(): void {
    this.balancesWanted = null;
    this.balancesSignal.set(null);
    this.balancesForSupplierSignal.set(null);
  }

  private async loadSupplierBalances(supplierId: string): Promise<void> {
    try {
      const balances = await firstValueFrom(
        this.api.getSupplierBalances(supplierId, { context: SuppliersStore.NO_TOAST }),
      );
      if (this.balancesWanted === supplierId) {
        this.balancesSignal.set(balances);
        this.balancesForSupplierSignal.set(supplierId);
      }
    } catch {
      // Best-effort: the dialogs hide the due banner when balances are unavailable.
      // balancesFor stays null so a later retry is possible and stale dues are never shown.
      if (this.balancesWanted === supplierId) {
        this.balancesSignal.set(null);
        this.balancesForSupplierSignal.set(null);
      }
    } finally {
      this.balancesPromise = null;
      const wanted = this.balancesWanted;
      if (wanted !== null && wanted !== this.balancesForSupplierSignal()) {
        void this.ensureSupplierBalances(wanted);
      }
    }
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
        await firstValueFrom(this.api.getAccounts({ context: SuppliersStore.NO_TOAST })),
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
      await this.refreshSuppliers();
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
      await firstValueFrom(
        this.api.updateSupplier(id, input, { context: SuppliersStore.NO_TOAST }),
      );
      this.toasts.success('تم تحديث المورد بنجاح');
      await this.refreshSuppliers();
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
      await this.refreshSuppliers();
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
      await this.refreshSuppliers();
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
      this.toasts.success('تم تسجيل دفعة الكسر بنجاح');
      await this.refreshSuppliers();
      this.clearSupplierBalances();
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
      await this.refreshSuppliers();
      this.clearSupplierBalances();
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
      await firstValueFrom(this.api.createTransaction(input, { context: SuppliersStore.NO_TOAST }));
      this.toasts.success('تمت إضافة المعاملة المالية بنجاح');
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  async createPayment(
    transactionId: string,
    input: SupplierFinancialPaymentInput,
  ): Promise<boolean> {
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
