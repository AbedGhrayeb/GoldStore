import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type SupplierResponse = components['schemas']['SupplierResponse'];
export type SupplierDetailResponse = components['schemas']['SupplierDetailResponse'];
export type SupplierFinancialTransactionResponse =
  components['schemas']['SupplierFinancialTransactionResponse'];
export type PagedSupplierFinancialTransactionResponse =
  components['schemas']['PagedSupplierFinancialTransactionResponse'];
export type SupplierFinancialKpiResponse = components['schemas']['SupplierFinancialKpiResponse'];
export type SupplierFinancialPaymentResponse =
  components['schemas']['SupplierFinancialPaymentResponse'];
export type FinancialAccountResponse = components['schemas']['FinancialAccountResponse'];

/** Create/update supplier payload shared by POST and PUT (schema-optional fields are required here). */
export interface SupplierInput {
  name: string;
  primaryPhone: string;
  secondaryPhone: string | null;
  bankAccountNumber: string | null;
  notes: string | null;
}

/** One delivery line: karat + weight + optional category; totals are header-level. */
export interface DeliveryLineInput {
  karat: number;
  weightInGrams: number;
  categoryId: string | null;
}

export interface DeliveryPaymentLegInput {
  accountId: string;
  currency: string;
  amount: number;
  exchangeRate: number;
}

export interface DeliveryInput {
  supplierId: string;
  lines: DeliveryLineInput[];
  manufacturingFeePerGram: number;
  manufacturingFeeCurrency: string;
  amountDue: number;
  amountDueCurrency: string;
  paymentLegs: DeliveryPaymentLegInput[] | null;
  notes: string | null;
}

export interface ScrapGoldPaymentInput {
  supplierId: string;
  karat: number;
  weightInGrams: number;
  notes: string | null;
  categoryId?: string | null;
}

export interface ManufacturingPaymentLegInput {
  accountId: string;
  currency: string;
  amount: number;
  exchangeRate: number;
}

export interface ManufacturingPaymentInput {
  supplierId: string;
  accountId: string;
  amount: number;
  currency: string;
  paymentLegs: ManufacturingPaymentLegInput[] | null;
  notes: string | null;
}

export interface SupplierTransactionResponse {
  id?: string;
  description?: string;
  date?: string;
  type?: string;
  amount?: number | string;
  unit?: string;
  direction?: string;
}

/** Paginated supplier detail transactions (server: PaginatedList<SupplierTransactionResponse>). */
export interface PagedSupplierTransactionsResponse {
  items: SupplierTransactionResponse[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface SupplierDetailTransactionsQuery {
  page?: number;
  pageSize?: number;
  /** 'gold' | 'manufacturing' | 'financial' — omitted/unknown returns all types. */
  type?: string;
}

/** Per-karat / per-currency dues for one supplier (GET /suppliers/{id}/balances). */
export interface SupplierGoldDue {
  karat: number | string;
  netWeight: number | string;
}

export interface SupplierManufacturingDue {
  currency: string;
  netAmount: number | string;
}

export interface SupplierBalancesResponse {
  supplierId: string;
  goldByKarat: SupplierGoldDue[];
  manufacturingByCurrency: SupplierManufacturingDue[];
}

/** 1 = FromSupplier (له), 2 = ToSupplier (لنا). */
export type FinancialDirection = 1 | 2;

export interface FinancialTransactionInput {
  supplierId: string;
  direction: FinancialDirection;
  amount: number;
  currency: string;
  accountId: string;
  date: string;
  notes: string | null;
}

export interface SupplierFinancialPaymentInput {
  accountId: string;
  amount: number;
  date: string;
  notes: string | null;
}

export interface SupplierTransactionQuery {
  page?: number;
  pageSize?: number;
  supplierId?: string;
  direction?: FinancialDirection;
  search?: string;
}

export interface SupplierListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  activeOnly?: boolean;
}

/** Paginated supplier list (server: PaginatedList<SupplierResponse>). */
export interface PagedSuppliersResponse {
  items: SupplierResponse[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

function toQueryString(params: object): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  }
  const query = search.toString();
  return query.length > 0 ? `?${query}` : '';
}

/**
 * Typed transport for the suppliers feature groups (`/api/v1/suppliers`,
 * `/api/v1/supplier-deliveries`, `/api/v1/supplier-payments/*`,
 * `/api/v1/supplier-financial-transactions*`, all `feature: suppliers`).
 * No payload ever carries a `tenantId` — the tenant comes from the session claims.
 * Components never call this directly — they go through {@link SuppliersStore}.
 */
@Injectable({ providedIn: 'root' })
export class SuppliersApi {
  private readonly api = inject(ApiClient);

  private static readonly suppliers = '/api/v1/suppliers';
  private static readonly deliveries = '/api/v1/supplier-deliveries';
  private static readonly payments = '/api/v1/supplier-payments';
  private static readonly transactions = '/api/v1/supplier-financial-transactions';
  private static readonly accounts = '/api/v1/finance/accounts';

  getSuppliers(options?: ApiRequestOptions): Observable<SupplierResponse[]> {
    return this.api.get<SupplierResponse[]>(SuppliersApi.suppliers, options);
  }

  getSuppliersPaged(
    query: SupplierListQuery,
    options?: ApiRequestOptions,
  ): Observable<PagedSuppliersResponse> {
    const queryString = toQueryString({
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 15,
      search: query.search ?? '',
      activeOnly: query.activeOnly ?? '',
    });
    return this.api.get<PagedSuppliersResponse>(
      `${SuppliersApi.suppliers}/paged${queryString}`,
      options,
    );
  }

  getSupplier(id: string, options?: ApiRequestOptions): Observable<SupplierDetailResponse> {
    return this.api.get<SupplierDetailResponse>(
      `${SuppliersApi.suppliers}/${encodeURIComponent(id)}`,
      options,
    );
  }

  getSupplierBalances(
    id: string,
    options?: ApiRequestOptions,
  ): Observable<SupplierBalancesResponse> {
    return this.api.get<SupplierBalancesResponse>(
      `${SuppliersApi.suppliers}/${encodeURIComponent(id)}/balances`,
      options,
    );
  }

  getSupplierTransactions(
    id: string,
    query: SupplierDetailTransactionsQuery,
    options?: ApiRequestOptions,
  ): Observable<PagedSupplierTransactionsResponse> {
    const queryString = toQueryString({
      type: query.type ?? '',
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 15,
    });
    return this.api.get<PagedSupplierTransactionsResponse>(
      `${SuppliersApi.suppliers}/${encodeURIComponent(id)}/transactions${queryString}`,
      options,
    );
  }

  createSupplier(input: SupplierInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(SuppliersApi.suppliers, input, options);
  }

  updateSupplier(
    id: string,
    input: SupplierInput,
    options?: ApiRequestOptions,
  ): Observable<Record<string, never>> {
    return this.api.put<Record<string, never>>(
      `${SuppliersApi.suppliers}/${encodeURIComponent(id)}`,
      input,
      options,
    );
  }

  toggleActive(id: string): Observable<Record<string, never>> {
    return this.api.post<Record<string, never>>(
      `${SuppliersApi.suppliers}/${encodeURIComponent(id)}/toggle-active`,
    );
  }

  createDelivery(input: DeliveryInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(SuppliersApi.deliveries, input, options);
  }

  createScrapGoldPayment(
    input: ScrapGoldPaymentInput,
    options?: ApiRequestOptions,
  ): Observable<string> {
    return this.api.post<string>(`${SuppliersApi.payments}/scrap-gold`, input, options);
  }

  createManufacturingPayment(
    input: ManufacturingPaymentInput,
    options?: ApiRequestOptions,
  ): Observable<string> {
    return this.api.post<string>(`${SuppliersApi.payments}/manufacturing`, input, options);
  }

  getKpis(options?: ApiRequestOptions): Observable<SupplierFinancialKpiResponse> {
    return this.api.get<SupplierFinancialKpiResponse>(`${SuppliersApi.transactions}/kpis`, options);
  }

  getTransactions(
    query: SupplierTransactionQuery,
    options?: ApiRequestOptions,
  ): Observable<PagedSupplierFinancialTransactionResponse> {
    const queryString = toQueryString(query);
    return this.api.get<PagedSupplierFinancialTransactionResponse>(
      `${SuppliersApi.transactions}${queryString}`,
      options,
    );
  }

  createTransaction(
    input: FinancialTransactionInput,
    options?: ApiRequestOptions,
  ): Observable<string> {
    return this.api.post<string>(SuppliersApi.transactions, input, options);
  }

  getPayments(
    transactionId: string,
    options?: ApiRequestOptions,
  ): Observable<SupplierFinancialPaymentResponse[]> {
    return this.api.get<SupplierFinancialPaymentResponse[]>(
      `${SuppliersApi.transactions}/${encodeURIComponent(transactionId)}/payments`,
      options,
    );
  }

  createPayment(
    transactionId: string,
    input: SupplierFinancialPaymentInput,
    options?: ApiRequestOptions,
  ): Observable<string> {
    return this.api.post<string>(
      `${SuppliersApi.transactions}/${encodeURIComponent(transactionId)}/payments`,
      input,
      options,
    );
  }

  /**
   * Account options for payment/transaction forms. Gated by `feature: finance` server-side —
   * best-effort here (see {@link SuppliersStore.ensureAccounts}); a suppliers-only user gets
   * an empty list and the payment forms degrade with an inline notice.
   */
  getAccounts(options?: ApiRequestOptions): Observable<FinancialAccountResponse[]> {
    return this.api.get<FinancialAccountResponse[]>(SuppliersApi.accounts, options);
  }
}
