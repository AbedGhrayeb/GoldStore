import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type CustomerPurchaseInvoiceResponse =
  components['schemas']['CustomerPurchaseInvoiceResponse'];
export type CustomerPurchaseInvoiceItemResponse =
  components['schemas']['CustomerPurchaseInvoiceItemResponse'];
export type CustomerPurchaseInvoiceKpiResponse =
  components['schemas']['CustomerPurchaseInvoiceKpiResponse'];
export type PaginatedCustomerPurchaseInvoices =
  components['schemas']['PaginatedListOfCustomerPurchaseInvoiceResponse'];
export type EmployeeResponse = components['schemas']['EmployeeResponse'];
export type CategoryResponse = components['schemas']['CategoryResponse'];
export type FinancialAccountResponse = components['schemas']['FinancialAccountResponse'];

/** One purchase line — mirrors `CustomerPurchaseInvoiceItemRequest` (the server computes the 21K-equivalent). */
export interface PurchaseItemInput {
  categoryId: string | null;
  karat: number;
  weightInGrams: number;
  pricePerGram: number;
}

/** One multi-currency payment leg — mirrors `InvoicePaymentLegRequest`. */
export interface PaymentLegInput {
  accountId: string;
  currency: string;
  amount: number;
  exchangeRate: number;
}

/**
 * Create-invoice payload — mirrors `CreateCustomerPurchaseInvoiceRequest` key-for-key. The
 * client never sends a `tenantId`; the 21K-equivalent is computed server-side. `accountId`
 * is required by the API even for the cash method — the page resolves it to the first
 * matching account (currency + Cash/Bank type), mirroring the MVC auto-selection.
 */
export interface CreateCustomerPurchaseInput {
  sellerName: string;
  sellerPhone: string | null;
  sellerIdNumber: string | null;
  sellerYearOfBirth: number | null;
  sellerAddress: string | null;
  employeeId: string;
  currency: string;
  date: string;
  totalAmount: number;
  amountPaid: number;
  paymentMethod: number;
  accountId: string;
  sellerAccountNumber: string | null;
  notes: string | null;
  items: PurchaseItemInput[];
  paymentLegs: PaymentLegInput[] | null;
}

export interface CustomerPurchaseInvoiceQuery {
  page?: number;
  pageSize?: number;
  fromDate?: string;
  toDate?: string;
  /** Matches invoice number or seller name. */
  search?: string;
  /** Single-select category filter — matches invoices having at least one line in the category. */
  categoryId?: string;
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
 * Typed transport for the customer-purchases group (`/api/v1/customer-purchases/invoices`,
 * `feature: purchases`) — paged list, KPIs, detail, create, plus the
 * best-effort reference lists the dialog needs (`/employees`, `/categories`,
 * `/finance/accounts` — gated by other features server-side, degraded gracefully). No payload
 * ever carries a `tenantId`; the 21K-equivalent is computed server-side. Components never
 * call this directly — they go through {@link PurchasesStore}.
 */
@Injectable({ providedIn: 'root' })
export class PurchasesApi {
  private readonly api = inject(ApiClient);

  private static readonly invoices = '/api/v1/customer-purchases/invoices';
  private static readonly employees = '/api/v1/employees';
  private static readonly categories = '/api/v1/categories';
  private static readonly accounts = '/api/v1/finance/accounts';

  getInvoices(
    query: CustomerPurchaseInvoiceQuery,
    options?: ApiRequestOptions,
  ): Observable<PaginatedCustomerPurchaseInvoices> {
    const queryString = toQueryString(query);
    return this.api.get<PaginatedCustomerPurchaseInvoices>(
      `${PurchasesApi.invoices}${queryString}`,
      options,
    );
  }

  getInvoice(id: string, options?: ApiRequestOptions): Observable<CustomerPurchaseInvoiceResponse> {
    return this.api.get<CustomerPurchaseInvoiceResponse>(
      `${PurchasesApi.invoices}/${encodeURIComponent(id)}`,
      options,
    );
  }

  getKpis(options?: ApiRequestOptions): Observable<CustomerPurchaseInvoiceKpiResponse> {
    return this.api.get<CustomerPurchaseInvoiceKpiResponse>(
      `${PurchasesApi.invoices}/kpis`,
      options,
    );
  }

  createInvoice(
    input: CreateCustomerPurchaseInput,
    options?: ApiRequestOptions,
  ): Observable<string> {
    return this.api.post<string>(PurchasesApi.invoices, input, options);
  }

  /** Gated `feature: hr` server-side — best-effort (see {@link PurchasesStore.ensureEmployees}). */
  getEmployees(options?: ApiRequestOptions): Observable<EmployeeResponse[]> {
    return this.api.get<EmployeeResponse[]>(PurchasesApi.employees, options);
  }

  /** Gated `feature: catalog` server-side — best-effort (see {@link PurchasesStore.ensureCategories}). */
  getCategories(options?: ApiRequestOptions): Observable<CategoryResponse[]> {
    return this.api.get<CategoryResponse[]>(PurchasesApi.categories, options);
  }

  /** Gated `feature: finance` server-side — best-effort (see {@link PurchasesStore.ensureAccounts}). */
  getAccounts(options?: ApiRequestOptions): Observable<FinancialAccountResponse[]> {
    return this.api.get<FinancialAccountResponse[]>(PurchasesApi.accounts, options);
  }
}
