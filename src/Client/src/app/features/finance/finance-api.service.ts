import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type FinancialAccountResponse = components['schemas']['FinancialAccountResponse'];
export type AccountWithBalanceResponse = components['schemas']['AccountWithBalanceResponse'];
export type DebtResponse = components['schemas']['DebtResponse'];
export type DebtKpiResponse = components['schemas']['DebtKpiResponse'];
export type RecentTransactionResponse = components['schemas']['RecentTransactionResponse'];
export type PaginatedDebts = components['schemas']['PaginatedListOfDebtResponse'];
export type PaginatedTransactions =
  components['schemas']['PaginatedListOfRecentTransactionResponse'];

/** Create-account payload — mirrors `CreateFinancialAccountRequest` key-for-key. */
export interface CreateAccountInput {
  name: string;
  currency: string;
  accountNumber: string | null;
  notes: string | null;
  openingBalance: number;
}

/** Set-balance payload — mirrors `SetAccountBalanceRequest` (a ledger adjustment). */
export interface SetBalanceInput {
  targetBalance: number;
  notes: string | null;
}

/** Create-debt payload — mirrors `CreateDebtRequest` key-for-key. */
export interface CreateDebtInput {
  name: string;
  phone: string | null;
  /** `DebtDirection` enum value: 1 = Receivable (لنا), 2 = Payable (علينا). */
  direction: number;
  currency: string;
  accountId: string;
  amount: number;
  notes: string | null;
  date: string;
}

/** Debt-payment payload — mirrors `CreateDebtPaymentRequest`. */
export interface DebtPaymentInput {
  accountId: string;
  amount: number;
  date: string;
  notes: string | null;
}

export interface AccountsWithBalancesQuery {
  /** `FinancialAccountType` enum name (Cash | Bank). */
  accountType?: string;
  activeOnly?: boolean;
  /** `Currency` enum name (JOD | USD | ILS). */
  currency?: string;
}

export interface DebtsQuery {
  page?: number;
  pageSize?: number;
  /** `DebtDirection` enum name (Receivable | Payable). */
  direction?: string;
  /** Matches the debt holder name or phone. */
  search?: string;
}

export interface TransactionsQuery {
  page?: number;
  pageSize?: number;
  /** Partial account-name match. */
  accountName?: string;
  fromDate?: string;
  toDate?: string;
  /** `Currency` enum name (JOD | USD | ILS). */
  currency?: string;
  /** `FinancialAccountType` enum name (Cash | Bank). */
  accountType?: string;
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
 * Typed transport for the finance group (`/api/v1/finance/*`, `feature: finance`) —
 * accounts (+ derived balances), debts (+ KPIs + payments) and financial transactions.
 * Balances are never stored server-side; they are derived from ledger sums, so every
 * mutation here is followed by a list reload. No payload ever carries a `tenantId`.
 * Components never call this directly — they go through {@link FinanceStore}.
 */
@Injectable({ providedIn: 'root' })
export class FinanceApi {
  private readonly api = inject(ApiClient);

  private static readonly accounts = '/api/v1/finance/accounts';
  private static readonly debts = '/api/v1/finance/debts';
  private static readonly transactions = '/api/v1/finance/transactions';

  getAccountsWithBalances(
    query: AccountsWithBalancesQuery,
    options?: ApiRequestOptions,
  ): Observable<AccountWithBalanceResponse[]> {
    const queryString = toQueryString({ activeOnly: false, ...query });
    return this.api.get<AccountWithBalanceResponse[]>(
      `${FinanceApi.accounts}/with-balances${queryString}`,
      options,
    );
  }

  createAccount(input: CreateAccountInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(FinanceApi.accounts, input, options);
  }

  setBalance(
    id: string,
    input: SetBalanceInput,
    options?: ApiRequestOptions,
  ): Observable<unknown> {
    return this.api.put<unknown>(
      `${FinanceApi.accounts}/${encodeURIComponent(id)}/balance`,
      input,
      options,
    );
  }

  getDebtKpis(options?: ApiRequestOptions): Observable<DebtKpiResponse> {
    return this.api.get<DebtKpiResponse>(`${FinanceApi.debts}/kpis`, options);
  }

  getDebts(query: DebtsQuery, options?: ApiRequestOptions): Observable<PaginatedDebts> {
    const queryString = toQueryString(query);
    return this.api.get<PaginatedDebts>(`${FinanceApi.debts}${queryString}`, options);
  }

  createDebt(input: CreateDebtInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(FinanceApi.debts, input, options);
  }

  payDebt(
    id: string,
    input: DebtPaymentInput,
    options?: ApiRequestOptions,
  ): Observable<unknown> {
    return this.api.post<unknown>(
      `${FinanceApi.debts}/${encodeURIComponent(id)}/payments`,
      input,
      options,
    );
  }

  getTransactions(
    query: TransactionsQuery,
    options?: ApiRequestOptions,
  ): Observable<PaginatedTransactions> {
    const queryString = toQueryString(query);
    return this.api.get<PaginatedTransactions>(
      `${FinanceApi.transactions}${queryString}`,
      options,
    );
  }

  getRecentTransactions(
    count: number,
    options?: ApiRequestOptions,
  ): Observable<RecentTransactionResponse[]> {
    return this.api.get<RecentTransactionResponse[]>(
      `${FinanceApi.transactions}/recent${toQueryString({ count })}`,
      options,
    );
  }
}
