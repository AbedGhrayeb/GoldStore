import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  InventoryApi,
  type AdjustmentInput,
  type AdjustmentsQuery,
  type GoldLedgerQuery,
  type GoldTrendPoint,
  type InventoryAdjustmentKpiResponse,
  type InventoryKpiResponse,
  type PaginatedGoldLedger,
  type PaginatedInventoryAdjustments,
} from './inventory-api.service';

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the inventory feature (`feature: inventory`). Owns the inventory
 * KPIs (21K total + karat breakdowns), the today-adjustments KPIs, the paged adjustments
 * table, the paged gold ledger, and the 7/14/30-day trend. Components consume only this
 * store — never {@link InventoryApi} directly.
 */
@Injectable({ providedIn: 'root' })
export class InventoryStore {
  private readonly api = inject(InventoryApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly inventoryKpisSignal = signal<InventoryKpiResponse | null>(null);
  private readonly inventoryKpisLoadingSignal = signal(false);

  private readonly adjustmentKpisSignal = signal<InventoryAdjustmentKpiResponse | null>(null);
  private readonly adjustmentKpisLoadingSignal = signal(false);

  private readonly adjustmentsSignal = signal<PaginatedInventoryAdjustments | null>(null);
  private readonly adjustmentsLoadingSignal = signal(false);
  private readonly adjustmentsErrorSignal = signal<ApiError | null>(null);

  private readonly ledgerSignal = signal<PaginatedGoldLedger | null>(null);
  private readonly ledgerLoadingSignal = signal(false);
  private readonly ledgerErrorSignal = signal<ApiError | null>(null);

  private readonly trendSignal = signal<GoldTrendPoint[] | null>(null);
  private readonly trendLoadingSignal = signal(false);
  private readonly trendErrorSignal = signal<ApiError | null>(null);

  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);

  readonly inventoryKpis = this.inventoryKpisSignal.asReadonly();
  readonly inventoryKpisLoading = this.inventoryKpisLoadingSignal.asReadonly();
  readonly adjustmentKpis = this.adjustmentKpisSignal.asReadonly();
  readonly adjustmentKpisLoading = this.adjustmentKpisLoadingSignal.asReadonly();
  readonly adjustments = this.adjustmentsSignal.asReadonly();
  readonly adjustmentsLoading = this.adjustmentsLoadingSignal.asReadonly();
  readonly adjustmentsError = this.adjustmentsErrorSignal.asReadonly();
  readonly ledger = this.ledgerSignal.asReadonly();
  readonly ledgerLoading = this.ledgerLoadingSignal.asReadonly();
  readonly ledgerError = this.ledgerErrorSignal.asReadonly();
  readonly trend = this.trendSignal.asReadonly();
  readonly trendLoading = this.trendLoadingSignal.asReadonly();
  readonly trendError = this.trendErrorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();

  async loadInventoryKpis(): Promise<void> {
    this.inventoryKpisLoadingSignal.set(true);
    try {
      this.inventoryKpisSignal.set(await firstValueFrom(this.api.getInventoryKpis()));
    } catch {
      // KPIs are cosmetic; the tables remain the source of truth.
    } finally {
      this.inventoryKpisLoadingSignal.set(false);
    }
  }

  async loadAdjustmentKpis(): Promise<void> {
    this.adjustmentKpisLoadingSignal.set(true);
    try {
      this.adjustmentKpisSignal.set(await firstValueFrom(this.api.getAdjustmentKpis()));
    } catch {
      // KPIs are cosmetic; the tables remain the source of truth.
    } finally {
      this.adjustmentKpisLoadingSignal.set(false);
    }
  }

  async loadAdjustments(query: AdjustmentsQuery): Promise<void> {
    this.adjustmentsLoadingSignal.set(true);
    this.adjustmentsErrorSignal.set(null);
    try {
      this.adjustmentsSignal.set(
        await firstValueFrom(
          this.api.getAdjustments(query, { context: InventoryStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.adjustmentsErrorSignal.set(asApiError(error));
    } finally {
      this.adjustmentsLoadingSignal.set(false);
    }
  }

  async loadLedger(query: GoldLedgerQuery): Promise<void> {
    this.ledgerLoadingSignal.set(true);
    this.ledgerErrorSignal.set(null);
    try {
      this.ledgerSignal.set(
        await firstValueFrom(this.api.getGoldLedger(query, { context: InventoryStore.NO_TOAST })),
      );
    } catch (error) {
      this.ledgerErrorSignal.set(asApiError(error));
    } finally {
      this.ledgerLoadingSignal.set(false);
    }
  }

  async loadTrend(days: number): Promise<void> {
    this.trendLoadingSignal.set(true);
    this.trendErrorSignal.set(null);
    try {
      this.trendSignal.set(
        await firstValueFrom(this.api.getTrend(days, { context: InventoryStore.NO_TOAST })),
      );
    } catch (error) {
      this.trendErrorSignal.set(asApiError(error));
    } finally {
      this.trendLoadingSignal.set(false);
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createAdjustment(input: AdjustmentInput): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(
        this.api.createAdjustment(input, { context: InventoryStore.NO_TOAST }),
      );
      this.toasts.success('تم تسجيل التسوية الجردية بنجاح');
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }
}