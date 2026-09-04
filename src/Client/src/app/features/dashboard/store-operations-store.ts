import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import {
  StoreOperationsApi,
  type DashboardEmployeeOption,
  type EmployeeDayStatsResponse,
  type OperationType,
  type PagedStoreOperationsResponse,
  type StoreOperationDetailResponse,
  type StoreOperationResponse,
  type StoreOperationsKpiResponse,
  type StoreOperationsQuery,
} from './store-operations-api.service';

export { type StoreOperationsKpiResponse, type StoreOperationResponse };

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the dashboard store-operations group. Owns KPI cards, the paged
 * operations table (with filters), employee day stats and the operation detail dialog.
 * Components consume only these signals — never HttpClient directly.
 */
@Injectable({ providedIn: 'root' })
export class StoreOperationsStore {
  private readonly api = inject(StoreOperationsApi);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly kpisSignal = signal<StoreOperationsKpiResponse | null>(null);
  private readonly pageSignal = signal<PagedStoreOperationsResponse | null>(null);
  private readonly employeesSignal = signal<DashboardEmployeeOption[]>([]);
  private readonly todayStatsSignal = signal<EmployeeDayStatsResponse[]>([]);
  private readonly detailSignal = signal<StoreOperationDetailResponse | null>(null);

  private readonly kpisLoadingSignal = signal(false);
  private readonly tableLoadingSignal = signal(false);
  private readonly statsLoadingSignal = signal(false);
  private readonly detailLoadingSignal = signal(false);

  private readonly tableErrorSignal = signal<ApiError | null>(null);
  private readonly detailErrorSignal = signal<ApiError | null>(null);

  readonly kpis = this.kpisSignal.asReadonly();
  readonly page = this.pageSignal.asReadonly();
  readonly employees = this.employeesSignal.asReadonly();
  readonly todayStats = this.todayStatsSignal.asReadonly();
  readonly detail = this.detailSignal.asReadonly();

  readonly kpisLoading = this.kpisLoadingSignal.asReadonly();
  readonly tableLoading = this.tableLoadingSignal.asReadonly();
  readonly statsLoading = this.statsLoadingSignal.asReadonly();
  readonly detailLoading = this.detailLoadingSignal.asReadonly();

  readonly tableError = this.tableErrorSignal.asReadonly();
  readonly detailError = this.detailErrorSignal.asReadonly();

  private kpisPromise: Promise<void> | null = null;
  private employeesPromise: Promise<void> | null = null;

  /** Loads today's KPIs once per session (single-flight). */
  ensureKpis(): Promise<void> {
    if (this.kpisSignal() !== null) {
      return Promise.resolve();
    }
    this.kpisPromise ??= this.loadKpis();
    return this.kpisPromise;
  }

  /** Loads the employee filter options once per session (single-flight). */
  ensureEmployees(): Promise<void> {
    if (this.employeesSignal().length > 0) {
      return Promise.resolve();
    }
    this.employeesPromise ??= this.loadEmployees();
    return this.employeesPromise;
  }

  async loadKpis(): Promise<void> {
    this.kpisLoadingSignal.set(true);
    try {
      const kpis = await firstValueFrom(this.api.getKpis());
      this.kpisSignal.set(kpis);
    } finally {
      this.kpisLoadingSignal.set(false);
      this.kpisPromise = null;
    }
  }

  async loadEmployees(): Promise<void> {
    try {
      const employees = await firstValueFrom(this.api.getEmployees());
      this.employeesSignal.set(employees);
    } finally {
      this.employeesPromise = null;
    }
  }

  async loadTodayStats(): Promise<void> {
    this.statsLoadingSignal.set(true);
    try {
      const stats = await firstValueFrom(this.api.getTodayEmployeeStats());
      this.todayStatsSignal.set(stats);
    } finally {
      this.statsLoadingSignal.set(false);
    }
  }

  async loadOperations(query: StoreOperationsQuery): Promise<void> {
    this.tableLoadingSignal.set(true);
    this.tableErrorSignal.set(null);
    try {
      const page = await firstValueFrom(
        this.api.getOperations(query, { context: StoreOperationsStore.NO_TOAST }),
      );
      this.pageSignal.set(page);
    } catch (error) {
      this.tableErrorSignal.set(asApiError(error));
    } finally {
      this.tableLoadingSignal.set(false);
    }
  }

  async loadDetail(id: string, operationType: OperationType): Promise<void> {
    this.detailLoadingSignal.set(true);
    this.detailErrorSignal.set(null);
    try {
      const detail = await firstValueFrom(
        this.api.getDetail(id, operationType, { context: StoreOperationsStore.NO_TOAST }),
      );
      this.detailSignal.set(detail);
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

  /** Re-pulls today's KPIs and employee stats (the dashboard refresh button). */
  async refresh(): Promise<void> {
    await Promise.all([this.loadKpis(), this.loadTodayStats()]);
  }
}
