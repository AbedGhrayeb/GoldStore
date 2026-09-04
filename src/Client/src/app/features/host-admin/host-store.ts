import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom, type Observable } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  HostApi,
  type CreatePlanInput,
  type ProvisionTenantInput,
  type RenewSubscriptionInput,
  type SubscriptionPlanResponse,
  type TenantReconciliationResponse,
  type TenantSubscriptionResponse,
  type TenantSummaryResponse,
  type UpdatePlanInput,
  type UpdateTenantStatusInput,
} from './host-api.service';

function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the host-admin feature (`/host/api/v1/*`). Owns the
 * tenants list and the reconciliation snapshot plus the `PATCH /tenants/{id}/status`
 * mutation (XSRF-protected). The feature is host-only — no tenant `feature:*` gate.
 * Components consume only this store — never {@link HostApi} directly.
 */
@Injectable({ providedIn: 'root' })
export class HostStore {
  private readonly api = inject(HostApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly tenantsSignal = signal<TenantSummaryResponse[] | null>(null);
  private readonly tenantsLoadingSignal = signal(false);
  private readonly tenantsErrorSignal = signal<ApiError | null>(null);

  private readonly reconciliationSignal = signal<TenantReconciliationResponse | null>(null);
  private readonly reconciliationLoadingSignal = signal(false);
  private readonly reconciliationErrorSignal = signal<ApiError | null>(null);

  private readonly plansSignal = signal<SubscriptionPlanResponse[] | null>(null);
  private readonly plansLoadingSignal = signal(false);
  private readonly plansErrorSignal = signal<ApiError | null>(null);

  private readonly subscriptionSignal = signal<TenantSubscriptionResponse | null>(null);
  private readonly subscriptionLoadingSignal = signal(false);
  private readonly subscriptionErrorSignal = signal<ApiError | null>(null);

  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);

  readonly tenants = this.tenantsSignal.asReadonly();
  readonly tenantsLoading = this.tenantsLoadingSignal.asReadonly();
  readonly tenantsError = this.tenantsErrorSignal.asReadonly();

  readonly reconciliation = this.reconciliationSignal.asReadonly();
  readonly reconciliationLoading = this.reconciliationLoadingSignal.asReadonly();
  readonly reconciliationError = this.reconciliationErrorSignal.asReadonly();

  readonly plans = this.plansSignal.asReadonly();
  readonly plansLoading = this.plansLoadingSignal.asReadonly();
  readonly plansError = this.plansErrorSignal.asReadonly();

  readonly subscription = this.subscriptionSignal.asReadonly();
  readonly subscriptionLoading = this.subscriptionLoadingSignal.asReadonly();
  readonly subscriptionError = this.subscriptionErrorSignal.asReadonly();

  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();

  async loadTenants(): Promise<void> {
    this.tenantsLoadingSignal.set(true);
    this.tenantsErrorSignal.set(null);
    try {
      this.tenantsSignal.set(
        await firstValueFrom(this.api.getTenants({ context: HostStore.NO_TOAST })),
      );
    } catch (error) {
      this.tenantsErrorSignal.set(asApiError(error));
    } finally {
      this.tenantsLoadingSignal.set(false);
    }
  }

  async loadReconciliation(tenantId: string | null | undefined): Promise<void> {
    this.reconciliationLoadingSignal.set(true);
    this.reconciliationErrorSignal.set(null);
    try {
      this.reconciliationSignal.set(
        await firstValueFrom(
          this.api.getReconciliation(tenantId, { context: HostStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.reconciliationErrorSignal.set(asApiError(error));
    } finally {
      this.reconciliationLoadingSignal.set(false);
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  clearReconciliation(): void {
    this.reconciliationSignal.set(null);
    this.reconciliationErrorSignal.set(null);
  }

  clearSubscription(): void {
    this.subscriptionSignal.set(null);
    this.subscriptionErrorSignal.set(null);
  }

  async loadPlans(): Promise<void> {
    this.plansLoadingSignal.set(true);
    this.plansErrorSignal.set(null);
    try {
      this.plansSignal.set(await firstValueFrom(this.api.listPlans({ context: HostStore.NO_TOAST })));
    } catch (error) {
      this.plansErrorSignal.set(asApiError(error));
    } finally {
      this.plansLoadingSignal.set(false);
    }
  }

  async loadSubscription(tenantId: string): Promise<void> {
    this.subscriptionLoadingSignal.set(true);
    this.subscriptionErrorSignal.set(null);
    try {
      this.subscriptionSignal.set(
        await firstValueFrom(this.api.getSubscription(tenantId, { context: HostStore.NO_TOAST })),
      );
    } catch (error) {
      this.subscriptionErrorSignal.set(asApiError(error));
    } finally {
      this.subscriptionLoadingSignal.set(false);
    }
  }

  async updateTenantStatus(tenantId: string, input: UpdateTenantStatusInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.updateTenantStatus(tenantId, input, options),
      'تم تحديث حالة المستأجر بنجاح',
    );
  }

  async createPlan(input: CreatePlanInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.createPlan(input, options),
      'تم إنشاء خطة الاشتراك بنجاح',
    );
  }

  async updatePlan(planId: string, input: UpdatePlanInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.updatePlan(planId, input, options),
      'تم تحديث خطة الاشتراك بنجاح',
    );
  }

  async renewSubscription(tenantId: string, input: RenewSubscriptionInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.renewSubscription(tenantId, input, options),
      'تم تجديد الاشتراك بنجاح',
    );
  }

  async provisionTenant(input: ProvisionTenantInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.provisionTenant(input, options),
      'تم تأسيس المتجر بنجاح',
    );
  }

  private async mutate(
    call: (options: { context: HttpContext }) => Observable<unknown>,
    successMessage: string,
  ): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(call({ context: HostStore.NO_TOAST }));
      this.toasts.success(successMessage);
      return true;
    } catch (error) {
      this.saveErrorSignal.set(asApiError(error));
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }
}
