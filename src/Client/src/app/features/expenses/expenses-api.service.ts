import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type ExpenseResponse = components['schemas']['ExpenseResponse'];
export type ExpenseCategoryResponse = components['schemas']['ExpenseCategoryResponse'];
export type ExpenseKpiResponse = components['schemas']['ExpenseKpiResponse'];
export type PaginatedExpenses = components['schemas']['PaginatedListOfExpenseResponse'];
export type FinancialAccountResponse = components['schemas']['FinancialAccountResponse'];

/** Create-expense payload — mirrors `CreateExpenseRequest` key-for-key. */
export interface CreateExpenseInput {
  expenseDate: string;
  categoryId: string | null;
  description: string | null;
  amount: number;
  accountId: string;
}

/** Update-expense payload — mirrors `UpdateExpenseRequest` key-for-key. */
export interface UpdateExpenseInput {
  expenseDate: string;
  categoryId: string | null;
  description: string | null;
  amount: number;
  accountId: string;
}

/** Create-category payload — mirrors `CreateExpenseCategoryRequest`. */
export interface CreateExpenseCategoryInput {
  name: string;
}

/** Update-category payload — mirrors `UpdateExpenseCategoryRequest`. */
export interface UpdateExpenseCategoryInput {
  name: string;
}

export interface ExpensesQuery {
  page?: number;
  pageSize?: number;
  /** Partial account-name match (server filters accounts containing the term). */
  accountName?: string;
  /** ISO date `YYYY-MM-DD`; maps to server `fromDate` (DateTime). */
  fromDate?: string;
  /** ISO date `YYYY-MM-DD`; maps to server `toDate`. */
  toDate?: string;
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
 * Typed transport for the expenses group (`/api/v1/expenses*`, `feature: expenses`) —
 * categories CRUD + expenses CRUD + paged list + KPIs. Every expense creates/updates a
 * financial OUT entry; deleting reverses it (removes the transaction) so the account balance
 * is ledger-derived. No payload ever carries a `tenantId`. Components never call this
 * directly — they go through {@link ExpensesStore}.
 */
@Injectable({ providedIn: 'root' })
export class ExpensesApi {
  private readonly api = inject(ApiClient);

  private static readonly expenses = '/api/v1/expenses';
  private static readonly categories = '/api/v1/expenses/categories';
  private static readonly accounts = '/api/v1/finance/accounts';

  getExpenses(query: ExpensesQuery, options?: ApiRequestOptions): Observable<PaginatedExpenses> {
    const queryString = toQueryString(query);
    return this.api.get<PaginatedExpenses>(`${ExpensesApi.expenses}${queryString}`, options);
  }

  getKpis(options?: ApiRequestOptions): Observable<ExpenseKpiResponse> {
    return this.api.get<ExpenseKpiResponse>(`${ExpensesApi.expenses}/kpis`, options);
  }

  createExpense(input: CreateExpenseInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(ExpensesApi.expenses, input, options);
  }

  updateExpense(
    id: string,
    input: UpdateExpenseInput,
    options?: ApiRequestOptions,
  ): Observable<unknown> {
    return this.api.put<unknown>(
      `${ExpensesApi.expenses}/${encodeURIComponent(id)}`,
      input,
      options,
    );
  }

  deleteExpense(id: string, options?: ApiRequestOptions): Observable<unknown> {
    return this.api.delete<unknown>(
      `${ExpensesApi.expenses}/${encodeURIComponent(id)}`,
      options,
    );
  }

  getCategories(
    activeOnly = false,
    options?: ApiRequestOptions,
  ): Observable<ExpenseCategoryResponse[]> {
    return this.api.get<ExpenseCategoryResponse[]>(
      `${ExpensesApi.categories}${toQueryString({ activeOnly })}`,
      options,
    );
  }

  createCategory(
    input: CreateExpenseCategoryInput,
    options?: ApiRequestOptions,
  ): Observable<string> {
    return this.api.post<string>(ExpensesApi.categories, input, options);
  }

  updateCategory(
    id: string,
    input: UpdateExpenseCategoryInput,
    options?: ApiRequestOptions,
  ): Observable<unknown> {
    return this.api.put<unknown>(
      `${ExpensesApi.categories}/${encodeURIComponent(id)}`,
      input,
      options,
    );
  }

  deleteCategory(id: string, options?: ApiRequestOptions): Observable<unknown> {
    return this.api.delete<unknown>(
      `${ExpensesApi.categories}/${encodeURIComponent(id)}`,
      options,
    );
  }

  /** Gated `feature: finance` server-side — best-effort (see {@link ExpensesStore.ensureAccounts}). */
  getAccounts(options?: ApiRequestOptions): Observable<FinancialAccountResponse[]> {
    return this.api.get<FinancialAccountResponse[]>(ExpensesApi.accounts, options);
  }
}
