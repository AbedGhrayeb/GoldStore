import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';

// ---------------------------------------------------------------------------
// DTOs — mirror WebUI Endpoints + Application layer exactly
// ---------------------------------------------------------------------------

export interface TenantSummaryResponse {
  id: string;
  key: string;
  name: string;
  status: string;
}

/** Mirrors `UpdateTenantStatusRequest` key-for-key. */
export interface UpdateTenantStatusInput {
  newStatus: number;
  transitionAtUtc: string | null;
}

export interface SubscriptionPlanResponse {
  id: string;
  name: string;
  key: string;
  maximumActiveUsers: number | null;
  maximumPostedInvoicesPerPeriod: number | null;
  maximumActiveBranches: number | null;
  maximumStorageBytes: number | null;
  isActive: boolean;
  isTrial: boolean;
  durationInMonths: number;
  price: number;
  discountPercent: number | null;
  effectivePrice: number;
}

export interface CreatePlanInput {
  name: string;
  key?: string | null;
  maximumActiveUsers: number | null;
  maximumPostedInvoicesPerPeriod: number | null;
  maximumActiveBranches: number | null;
  maximumStorageBytes: number | null;
  isTrial: boolean;
  durationInMonths: number;
  price: number;
  discountPercent: number | null;
}

export interface UpdatePlanInput {
  name: string;
  key?: string | null;
  maximumActiveUsers: number | null;
  maximumPostedInvoicesPerPeriod: number | null;
  maximumActiveBranches: number | null;
  maximumStorageBytes: number | null;
  isTrial: boolean;
  durationInMonths: number;
  price: number;
  discountPercent: number | null;
  isActive: boolean;
}

export interface SubscriptionPlanSnapshot {
  id: string;
  name: string;
  key: string;
  maximumActiveUsers: number | null;
  maximumPostedInvoicesPerPeriod: number | null;
  maximumActiveBranches: number | null;
  maximumStorageBytes: number | null;
  isActive: boolean;
  isTrial: boolean;
  durationInMonths: number;
  price: number;
  discountPercent: number | null;
  effectivePrice: number;
}

export interface TenantSubscriptionResponse {
  id: string;
  tenantId: string;
  status: string;
  billingCycle: string;
  startsAtUtc: string;
  endsAtUtc: string;
  billingProvider: string | null;
  billingProviderReference: string | null;
  plan: SubscriptionPlanSnapshot;
}

export interface RenewSubscriptionInput {
  newPlanId: string | null;
  billingCycle: number;
  startsAtUtc: string;
  endsAtUtc: string;
}

export interface ProvisionTenantInput {
  name: string;
  key: string;
  timeZoneId: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminPassword: string;
  adminPhoneNumber?: string | null;
  adminWhatsappNumber?: string | null;
  subscriptionPlanId: string;
  billingCycle: number;
  startsAtUtc: string;
  endsAtUtc: string;
}

export interface ReconciliationAnomaly {
  table: string;
  tenantId: string | null;
  count: number;
  message: string;
}

export interface EntityRowCount {
  entity: string;
  count: number;
}

export interface GoldStockBalance {
  karat: number;
  weightGrams: number;
  equivalent21K: number;
}

export interface FinancialBalance {
  accountId: string;
  accountName: string;
  currency: string;
  balance: number;
}

export interface CurrencyTotals {
  currency: string;
  totalInflow: number;
  totalOutflow: number;
  net: number;
}

export interface DebtTotals {
  currency: string;
  totalReceivable: number;
  totalPayable: number;
  net: number;
}

export interface SupplierGoldBalance {
  supplierId: string;
  supplierName: string;
  karat: number;
  netWeight: number;
}

export interface SupplierManufacturingBalance {
  supplierId: string;
  supplierName: string;
  currency: string;
  netAmount: number;
}

export interface TenantReconciliationBlock {
  tenantId: string;
  key: string;
  name: string;
  status: string;
  rowCounts: EntityRowCount[];
  goldStock: GoldStockBalance[];
  totalEquivalent21K: number;
  financialBalances: FinancialBalance[];
  financialTotals: CurrencyTotals[];
  debtTotals: DebtTotals[];
  supplierGoldBalances: SupplierGoldBalance[];
  supplierManufacturingBalances: SupplierManufacturingBalance[];
}

export interface TenantReconciliationResponse {
  generatedAtUtc: string;
  anomalies: ReconciliationAnomaly[];
  tenants: TenantReconciliationBlock[];
}

function toQueryString(params: Record<string, string | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, value);
    }
  }
  const query = search.toString();
  return query.length > 0 ? `?${query}` : '';
}

/**
 * Typed transport for the host-admin group (`/host/api/v1/*`, cookie
 * `GoldStore.HostAccessToken` + XSRF). Never mixes into tenant services.
 * Components never call this directly — they go through {@link HostStore}.
 */
@Injectable({ providedIn: 'root' })
export class HostApi {
  private readonly api = inject(ApiClient);

  private static readonly base = '/host/api/v1';

  getTenants(options?: ApiRequestOptions): Observable<TenantSummaryResponse[]> {
    return this.api.get<TenantSummaryResponse[]>(`${HostApi.base}/tenants`, options);
  }

  updateTenantStatus(
    tenantId: string,
    input: UpdateTenantStatusInput,
    options?: ApiRequestOptions,
  ): Observable<unknown> {
    return this.api.patch<unknown>(
      `${HostApi.base}/tenants/${encodeURIComponent(tenantId)}/status`,
      input,
      options,
    );
  }

  getReconciliation(
    tenantId: string | null | undefined,
    options?: ApiRequestOptions,
  ): Observable<TenantReconciliationResponse> {
    const query = toQueryString({ tenantId: tenantId ?? undefined });
    return this.api.get<TenantReconciliationResponse>(
      `${HostApi.base}/reconciliation${query}`,
      options,
    );
  }

  listPlans(options?: ApiRequestOptions): Observable<SubscriptionPlanResponse[]> {
    return this.api.get<SubscriptionPlanResponse[]>(`${HostApi.base}/subscription-plans`, options);
  }

  createPlan(input: CreatePlanInput, options?: ApiRequestOptions): Observable<{ planId: string }> {
    return this.api.post<{ planId: string }>(`${HostApi.base}/subscription-plans`, input, options);
  }

  updatePlan(planId: string, input: UpdatePlanInput, options?: ApiRequestOptions): Observable<{ planId: string }> {
    return this.api.put<{ planId: string }>(`${HostApi.base}/subscription-plans/${encodeURIComponent(planId)}`, input, options);
  }

  getSubscription(
    tenantId: string,
    options?: ApiRequestOptions,
  ): Observable<TenantSubscriptionResponse> {
    return this.api.get<TenantSubscriptionResponse>(
      `${HostApi.base}/tenants/${encodeURIComponent(tenantId)}/subscription`,
      options,
    );
  }

  renewSubscription(
    tenantId: string,
    input: RenewSubscriptionInput,
    options?: ApiRequestOptions,
  ): Observable<{ subscriptionId: string }> {
    return this.api.post<{ subscriptionId: string }>(
      `${HostApi.base}/tenants/${encodeURIComponent(tenantId)}/subscription/renew`,
      input,
      options,
    );
  }

  provisionTenant(input: ProvisionTenantInput, options?: ApiRequestOptions): Observable<{ tenantId: string }> {
    return this.api.post<{ tenantId: string }>(`${HostApi.base}/tenants`, input, options);
  }
}
