import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { components } from '../../shared/api/schema';
import { ApiClient, type ApiRequestOptions } from '../../core/http/api-client.service';

export type StoreOperationsKpiResponse = components['schemas']['StoreOperationsKpiResponse'];
export type PagedStoreOperationsResponse = components['schemas']['PagedStoreOperationsResponse'];
export type StoreOperationResponse = components['schemas']['StoreOperationResponse'];
export type StoreOperationDetailResponse = components['schemas']['StoreOperationDetailResponse'];
export type StoreOperationItemResponse = components['schemas']['StoreOperationItemResponse'];
export type EmployeeDayStatsResponse = components['schemas']['EmployeeDayStatsResponse'];
export type CurrencyTotal = components['schemas']['CurrencyTotal'];
export type CategoryKpi = components['schemas']['CategoryKpi'];

export type OperationType = 'Sale' | 'Buy';

/**
 * Employee filter option returned by GET /api/v1/dashboard/store-operations/employees.
 *
 * The endpoint returns `{ id, name }` (StoreOperations.GetEmployees.EmployeeResponse), but the
 * OpenAPI document pins the HR-shaped `EmployeeResponse` schema (firstName/lastName/...). This is a
 * Phase 7a contract-drift bug; the real runtime shape is pinned here and must not read the HR fields.
 */
export interface DashboardEmployeeOption {
  id: string;
  name: string;
}

export interface StoreOperationsQuery {
  page?: number;
  pageSize?: number;
  fromDate?: string;
  toDate?: string;
  operationType?: string;
  employeeId?: string;
  accountId?: string;
  search?: string;
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
 * Typed transport for the dashboard store-operations group
 * (`/api/v1/dashboard/store-operations`, auth-only, no feature gate). Components never call this
 * directly — they go through {@link StoreOperationsStore}.
 */
@Injectable({ providedIn: 'root' })
export class StoreOperationsApi {
  private readonly api = inject(ApiClient);
  private static readonly base = '/api/v1/dashboard/store-operations';

  getKpis(): Observable<StoreOperationsKpiResponse> {
    return this.api.get<StoreOperationsKpiResponse>(`${StoreOperationsApi.base}/kpis`);
  }

  getEmployees(): Observable<DashboardEmployeeOption[]> {
    return this.api.get<DashboardEmployeeOption[]>(`${StoreOperationsApi.base}/employees`);
  }

  getTodayEmployeeStats(): Observable<EmployeeDayStatsResponse[]> {
    return this.api.get<EmployeeDayStatsResponse[]>(
      `${StoreOperationsApi.base}/today-employee-stats`,
    );
  }

  getOperations(
    query: StoreOperationsQuery,
    options?: ApiRequestOptions,
  ): Observable<PagedStoreOperationsResponse> {
    const queryString = toQueryString(query);
    return this.api.get<PagedStoreOperationsResponse>(
      `${StoreOperationsApi.base}${queryString}`,
      options,
    );
  }

  getDetail(
    id: string,
    operationType: OperationType,
    options?: ApiRequestOptions,
  ): Observable<StoreOperationDetailResponse> {
    return this.api.get<StoreOperationDetailResponse>(
      `${StoreOperationsApi.base}/${encodeURIComponent(id)}/detail?operationType=${operationType}`,
      options,
    );
  }
}
