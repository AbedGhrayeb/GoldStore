import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type EmployeeResponse = components['schemas']['EmployeeResponse'];
export type SalaryPaymentResponse = components['schemas']['SalaryPaymentResponse'];
export type SalaryPeriodSummaryResponse = components['schemas']['SalaryPeriodSummaryResponse'];
export type EmployeeUserOptionResponse = components['schemas']['EmployeeUserOptionResponse'];
export type PaginatedSalaryPayments = components['schemas']['PaginatedListOfSalaryPaymentResponse'];
export type FinancialAccountResponse = components['schemas']['FinancialAccountResponse'];

/** Mirrors `CreateEmployeeRequest` key-for-key. */
export interface CreateEmployeeInput {
  firstName: string;
  lastName: string;
  role: number;
  salary: number;
  currency: number;
  salaryCycle: number;
  connectToUser: boolean;
  existingUserId: string | null;
  newUserEmail: string | null;
  newUserPassword: string | null;
}

/** Mirrors `UpdateEmployeeRequest` key-for-key. */
export interface UpdateEmployeeInput {
  firstName: string;
  lastName: string;
  role: number;
  salary: number;
  currency: number;
  salaryCycle: number;
  isActive: boolean;
}

/** Mirrors `PaySalaryRequest` key-for-key. */
export interface PaySalaryInput {
  accountId: string;
  amount: number;
  paymentDate: string;
  notes: string | null;
}

export interface SalaryPaymentsQuery {
  page?: number;
  pageSize?: number;
  employeeName?: string;
  fromDate?: string;
  toDate?: string;
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
 * Typed transport for the HR group (`/api/v1/employees*`, `feature: hr`) — employees
 * CRUD/toggle/by-id, pay-salary (financial OUT ledger), paged salary payments,
 * salary-period summary, and unlinked users. No payload ever carries a `tenantId`.
 * Components never call this directly — they go through {@link HrStore}.
 */
@Injectable({ providedIn: 'root' })
export class HrApi {
  private readonly api = inject(ApiClient);

  private static readonly employees = '/api/v1/employees';
  private static readonly accounts = '/api/v1/finance/accounts';

  getEmployees(options?: ApiRequestOptions): Observable<EmployeeResponse[]> {
    return this.api.get<EmployeeResponse[]>(HrApi.employees, options);
  }

  getEmployee(id: string, options?: ApiRequestOptions): Observable<EmployeeResponse> {
    return this.api.get<EmployeeResponse>(`${HrApi.employees}/${encodeURIComponent(id)}`, options);
  }

  createEmployee(input: CreateEmployeeInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(HrApi.employees, input, options);
  }

  updateEmployee(
    id: string,
    input: UpdateEmployeeInput,
    options?: ApiRequestOptions,
  ): Observable<unknown> {
    return this.api.put<unknown>(`${HrApi.employees}/${encodeURIComponent(id)}`, input, options);
  }

  toggleActive(id: string, options?: ApiRequestOptions): Observable<unknown> {
    return this.api.post<unknown>(`${HrApi.employees}/${encodeURIComponent(id)}/toggle-active`, {}, options);
  }

  paySalary(
    id: string,
    input: PaySalaryInput,
    options?: ApiRequestOptions,
  ): Observable<string> {
    return this.api.post<string>(`${HrApi.employees}/${encodeURIComponent(id)}/pay-salary`, input, options);
  }

  getSalaryPayments(
    query: SalaryPaymentsQuery,
    options?: ApiRequestOptions,
  ): Observable<PaginatedSalaryPayments> {
    const queryString = toQueryString(query);
    return this.api.get<PaginatedSalaryPayments>(`${HrApi.employees}/salary-payments${queryString}`, options);
  }

  getSalaryPeriodSummary(
    employeeId: string,
    paymentDate: string,
    options?: ApiRequestOptions,
  ): Observable<SalaryPeriodSummaryResponse> {
    return this.api.get<SalaryPeriodSummaryResponse>(
      `${HrApi.employees}/salary-period-summary${toQueryString({ employeeId, paymentDate })}`,
      options,
    );
  }

  getUnlinkedUsers(options?: ApiRequestOptions): Observable<EmployeeUserOptionResponse[]> {
    return this.api.get<EmployeeUserOptionResponse[]>(`${HrApi.employees}/unlinked-users`, options);
  }

  /** Gated `feature: finance` server-side — best-effort for the pay-salary account select. */
  getAccounts(options?: ApiRequestOptions): Observable<FinancialAccountResponse[]> {
    return this.api.get<FinancialAccountResponse[]>(HrApi.accounts, options);
  }
}
