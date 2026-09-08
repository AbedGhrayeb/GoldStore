import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type SalesInvoiceResponse = components['schemas']['SalesInvoiceResponse'];
export type SalesInvoiceItemResponse = components['schemas']['SalesInvoiceItemResponse'];
export type SalesInvoiceKpiResponse = components['schemas']['SalesInvoiceKpiResponse'];
export type PaginatedSalesInvoices = components['schemas']['PaginatedListOfSalesInvoiceResponse'];
export type FinancialAccountResponse = components['schemas']['FinancialAccountResponse'];
export type EmployeeResponse = components['schemas']['EmployeeResponse'];
export type CategoryResponse = components['schemas']['CategoryResponse'];

/** One invoice line — mirrors `SalesInvoiceItemRequest` (the server computes the 21K-equivalent). */
export interface SalesInvoiceItemInput {
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
 * Create-invoice payload — mirrors `CreateSalesInvoiceRequest` key-for-key. The client never
 * sends a `tenantId`; the 21K-equivalent is computed server-side.
 */
export interface CreateSalesInvoiceInput {
  customerName: string;
  customerPhone: string | null;
  date: string;
  currency: string;
  items: SalesInvoiceItemInput[];
  totalAmount: number;
  amountPaid: number;
  paymentMethod: number | null;
  accountId: string | null;
  buyerAccountNumber: string | null;
  employeeId: string;
  paymentLegs: PaymentLegInput[] | null;
  notes: string | null;
}

export interface SalesInvoiceQuery {
  page?: number;
  pageSize?: number;
  fromDate?: string;
  toDate?: string;
  search?: string;
  /** `SalesInvoiceStatus` enum name (Draft | Completed | PartiallyPaid | Cancelled). */
  status?: string;
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
 * Typed transport for the sales-invoice group (`/api/v1/sales-invoices*`, `feature: sales`)
 * plus the best-effort reference lists the invoice form needs (`/employees`, `/categories`,
 * `/finance/accounts` — gated by other features server-side, degraded gracefully in the
 * dialogs). No payload ever carries a `tenantId`. Components never call this directly — they
 * go through {@link SalesStore}.
 */
@Injectable({ providedIn: 'root' })
export class SalesApi {
  private readonly api = inject(ApiClient);

  private static readonly invoices = '/api/v1/sales-invoices';
  private static readonly employees = '/api/v1/employees';
  private static readonly categories = '/api/v1/categories';
  private static readonly accounts = '/api/v1/finance/accounts';

  getInvoices(
    query: SalesInvoiceQuery,
    options?: ApiRequestOptions,
  ): Observable<PaginatedSalesInvoices> {
    const queryString = toQueryString(query);
    return this.api.get<PaginatedSalesInvoices>(`${SalesApi.invoices}${queryString}`, options);
  }

  getInvoice(id: string, options?: ApiRequestOptions): Observable<SalesInvoiceResponse> {
    return this.api.get<SalesInvoiceResponse>(
      `${SalesApi.invoices}/${encodeURIComponent(id)}`,
      options,
    );
  }

  getKpis(options?: ApiRequestOptions): Observable<SalesInvoiceKpiResponse> {
    return this.api.get<SalesInvoiceKpiResponse>(`${SalesApi.invoices}/kpis`, options);
  }

  createInvoice(input: CreateSalesInvoiceInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(SalesApi.invoices, input, options);
  }

  /** Gated `feature: hr` server-side — best-effort (see {@link SalesStore.ensureEmployees}). */
  getEmployees(options?: ApiRequestOptions): Observable<EmployeeResponse[]> {
    return this.api.get<EmployeeResponse[]>(SalesApi.employees, options);
  }

  /** Gated `feature: catalog` server-side — best-effort (see {@link SalesStore.ensureCategories}). */
  getCategories(options?: ApiRequestOptions): Observable<CategoryResponse[]> {
    return this.api.get<CategoryResponse[]>(SalesApi.categories, options);
  }

  /** Gated `feature: finance` server-side — best-effort (see {@link SalesStore.ensureAccounts}). */
  getAccounts(options?: ApiRequestOptions): Observable<FinancialAccountResponse[]> {
    return this.api.get<FinancialAccountResponse[]>(SalesApi.accounts, options);
  }
}
