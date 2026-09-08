import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type InventoryAdjustmentResponse = components['schemas']['InventoryAdjustmentResponse'];
export type PaginatedInventoryAdjustments =
  components['schemas']['PaginatedListOfInventoryAdjustmentResponse'];
export type InventoryAdjustmentKpiResponse =
  components['schemas']['InventoryAdjustmentKpiResponse'];
export type GoldLedgerEntryResponse = components['schemas']['GoldLedgerEntryResponse'];
export type PaginatedGoldLedger = components['schemas']['PaginatedListOfGoldLedgerEntryResponse'];
export type GoldTrendPoint = components['schemas']['GoldTrendPoint'];
export type InventoryKpiResponse = components['schemas']['InventoryKpiResponse'];

/**
 * Payload for creating an inventory adjustment — mirrors `CreateInventoryAdjustmentRequest`
 * exactly (`adjustmentType` 1..5, `karat` 18/21/24, `date` ISO). The server writes the gold
 * ledger entry and its 21K-equivalent; the client never sends one.
 */
export interface AdjustmentInput {
  adjustmentType: number;
  karat: number;
  weightInGrams: number;
  reason: string;
  notes: string | null;
  date: string;
}

export interface AdjustmentsQuery {
  page?: number;
  pageSize?: number;
  fromDate?: string;
  toDate?: string;
  /** `InventoryAdjustmentType` enum name or number string (1..5). */
  adjustmentType?: string;
  /** Free-text search across reason and notes. */
  search?: string;
  /** Karat filter (18/21/24). */
  karat?: number;
}

export interface GoldLedgerQuery {
  page?: number;
  pageSize?: number;
  karat?: number;
  fromDate?: string;
  toDate?: string;
  /** `GoldReferenceType` enum name (SupplierDelivery, Sale, InventoryAdjustment, ...). */
  referenceType?: string;
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
 * Typed transport for the inventory feature group (`/api/v1/inventory/*`, gated
 * `feature: inventory`): paged adjustments + create, adjustment KPIs, paged gold ledger,
 * 7/14/30-day trend, and inventory KPIs by karat. No payload ever carries a `tenantId`.
 * Components never call this directly — they go through {@link InventoryStore}.
 */
@Injectable({ providedIn: 'root' })
export class InventoryApi {
  private readonly api = inject(ApiClient);

  private static readonly adjustments = '/api/v1/inventory/adjustments';
  private static readonly ledger = '/api/v1/inventory/gold-ledger';

  getAdjustments(
    query: AdjustmentsQuery,
    options?: ApiRequestOptions,
  ): Observable<PaginatedInventoryAdjustments> {
    const queryString = toQueryString(query);
    return this.api.get<PaginatedInventoryAdjustments>(
      `${InventoryApi.adjustments}${queryString}`,
      options,
    );
  }

  createAdjustment(input: AdjustmentInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(InventoryApi.adjustments, input, options);
  }

  getAdjustmentKpis(options?: ApiRequestOptions): Observable<InventoryAdjustmentKpiResponse> {
    return this.api.get<InventoryAdjustmentKpiResponse>(
      `${InventoryApi.adjustments}/kpis`,
      options,
    );
  }

  getGoldLedger(
    query: GoldLedgerQuery,
    options?: ApiRequestOptions,
  ): Observable<PaginatedGoldLedger> {
    const queryString = toQueryString(query);
    return this.api.get<PaginatedGoldLedger>(`${InventoryApi.ledger}${queryString}`, options);
  }

  getTrend(days: number, options?: ApiRequestOptions): Observable<GoldTrendPoint[]> {
    return this.api.get<GoldTrendPoint[]>(`${InventoryApi.ledger}/trend?days=${days}`, options);
  }

  getInventoryKpis(options?: ApiRequestOptions): Observable<InventoryKpiResponse> {
    return this.api.get<InventoryKpiResponse>(`${InventoryApi.ledger}/kpis`, options);
  }
}
