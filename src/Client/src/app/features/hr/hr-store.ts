import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  HrApi,
  type CreateEmployeeInput,
  type EmployeeResponse,
  type EmployeeUserOptionResponse,
  type FinancialAccountResponse,
  type PaginatedSalaryPayments,
  type PaySalaryInput,
  type SalaryPeriodSummaryResponse,
  type SalaryPaymentsQuery,
  type UpdateEmployeeInput,
} from './hr-api.service';

function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the HR feature (`feature: hr`). Owns the employees list
 * (+ by-id), the paged salary payments, the salary-period summary, the unlinked users,
 * the best-effort accounts for paying, and the create/update/toggle/pay mutations.
 * Paying creates a financial OUT entry + salary payment; every mutation reloads the
 * affected lists. Components consume only this store — never {@link HrApi} directly.
 */
@Injectable({ providedIn: 'root' })
export class HrStore {
  private readonly api = inject(HrApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly employeesSignal = signal<EmployeeResponse[] | null>(null);
  private readonly employeesLoadingSignal = signal(false);
  private readonly employeesErrorSignal = signal<ApiError | null>(null);

  private readonly employeeSignal = signal<EmployeeResponse | null>(null);
  private readonly employeeLoadingSignal = signal(false);
  private readonly employeeErrorSignal = signal<ApiError | null>(null);

  private readonly salaryPaymentsSignal = signal<PaginatedSalaryPayments | null>(null);
  private readonly salaryPaymentsLoadingSignal = signal(false);
  private readonly salaryPaymentsErrorSignal = signal<ApiError | null>(null);

  private readonly periodSummarySignal = signal<SalaryPeriodSummaryResponse | null>(null);
  private readonly periodSummaryLoadingSignal = signal(false);
  private readonly periodSummaryErrorSignal = signal<ApiError | null>(null);

  private readonly unlinkedUsersSignal = signal<EmployeeUserOptionResponse[]>([]);
  private readonly unlinkedUsersErrorSignal = signal<string | null>(null);
  private readonly unlinkedUsersLoadedSignal = signal(false);

  private readonly accountsSignal = signal<FinancialAccountResponse[]>([]);
  private readonly accountsErrorSignal = signal<string | null>(null);
  private readonly accountsLoadedSignal = signal(false);

  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);

  private unlinkedPromise: Promise<void> | null = null;
  private accountsPromise: Promise<void> | null = null;

  readonly employees = this.employeesSignal.asReadonly();
  readonly employeesLoading = this.employeesLoadingSignal.asReadonly();
  readonly employeesError = this.employeesErrorSignal.asReadonly();
  readonly employee = this.employeeSignal.asReadonly();
  readonly employeeLoading = this.employeeLoadingSignal.asReadonly();
  readonly employeeError = this.employeeErrorSignal.asReadonly();
  readonly salaryPayments = this.salaryPaymentsSignal.asReadonly();
  readonly salaryPaymentsLoading = this.salaryPaymentsLoadingSignal.asReadonly();
  readonly salaryPaymentsError = this.salaryPaymentsErrorSignal.asReadonly();
  readonly periodSummary = this.periodSummarySignal.asReadonly();
  readonly periodSummaryLoading = this.periodSummaryLoadingSignal.asReadonly();
  readonly periodSummaryError = this.periodSummaryErrorSignal.asReadonly();
  readonly unlinkedUsers = this.unlinkedUsersSignal.asReadonly();
  readonly unlinkedUsersError = this.unlinkedUsersErrorSignal.asReadonly();
  readonly accounts = this.accountsSignal.asReadonly();
  readonly accountsError = this.accountsErrorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();

  async loadEmployees(): Promise<void> {
    this.employeesLoadingSignal.set(true);
    this.employeesErrorSignal.set(null);
    try {
      this.employeesSignal.set(await firstValueFrom(this.api.getEmployees({ context: HrStore.NO_TOAST })));
    } catch (error) {
      this.employeesErrorSignal.set(asApiError(error));
    } finally {
      this.employeesLoadingSignal.set(false);
    }
  }

  async loadEmployee(id: string): Promise<void> {
    this.employeeLoadingSignal.set(true);
    this.employeeErrorSignal.set(null);
    try {
      this.employeeSignal.set(await firstValueFrom(this.api.getEmployee(id, { context: HrStore.NO_TOAST })));
    } catch (error) {
      this.employeeErrorSignal.set(asApiError(error));
    } finally {
      this.employeeLoadingSignal.set(false);
    }
  }

  clearEmployee(): void {
    this.employeeSignal.set(null);
    this.employeeErrorSignal.set(null);
  }

  async loadSalaryPayments(query: SalaryPaymentsQuery): Promise<void> {
    this.salaryPaymentsLoadingSignal.set(true);
    this.salaryPaymentsErrorSignal.set(null);
    try {
      this.salaryPaymentsSignal.set(
        await firstValueFrom(this.api.getSalaryPayments(query, { context: HrStore.NO_TOAST })),
      );
    } catch (error) {
      this.salaryPaymentsErrorSignal.set(asApiError(error));
    } finally {
      this.salaryPaymentsLoadingSignal.set(false);
    }
  }

  async loadPeriodSummary(employeeId: string, paymentDate: string): Promise<void> {
    this.periodSummaryLoadingSignal.set(true);
    this.periodSummaryErrorSignal.set(null);
    try {
      this.periodSummarySignal.set(
        await firstValueFrom(this.api.getSalaryPeriodSummary(employeeId, paymentDate, { context: HrStore.NO_TOAST })),
      );
    } catch (error) {
      this.periodSummaryErrorSignal.set(asApiError(error));
    } finally {
      this.periodSummaryLoadingSignal.set(false);
    }
  }

  clearPeriodSummary(): void {
    this.periodSummarySignal.set(null);
    this.periodSummaryErrorSignal.set(null);
  }

  ensureUnlinkedUsers(): Promise<void> {
    if (this.unlinkedUsersLoadedSignal()) {
      return Promise.resolve();
    }
    this.unlinkedPromise ??= this.loadUnlinkedUsers();
    return this.unlinkedPromise;
  }

  private async loadUnlinkedUsers(): Promise<void> {
    try {
      this.unlinkedUsersSignal.set(await firstValueFrom(this.api.getUnlinkedUsers({ context: HrStore.NO_TOAST })));
    } catch {
      this.unlinkedUsersErrorSignal.set('تعذّر تحميل المستخدمين غير المرتبطين.');
    } finally {
      this.unlinkedUsersLoadedSignal.set(true);
      this.unlinkedPromise = null;
    }
  }

  ensureAccounts(): Promise<void> {
    if (this.accountsLoadedSignal()) {
      return Promise.resolve();
    }
    this.accountsPromise ??= this.loadAccounts();
    return this.accountsPromise;
  }

  private async loadAccounts(): Promise<void> {
    try {
      this.accountsSignal.set(await firstValueFrom(this.api.getAccounts({ context: HrStore.NO_TOAST })));
    } catch {
      this.accountsErrorSignal.set('تعذّر تحميل الحسابات المالية (تحتاج صلاحية "المالية" لاختيار حساب دفع الراتب).');
    } finally {
      this.accountsLoadedSignal.set(true);
      this.accountsPromise = null;
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createEmployee(input: CreateEmployeeInput): Promise<boolean> {
    return this.mutate((o) => this.api.createEmployee(input, o), 'تم إنشاء الموظف بنجاح');
  }

  async updateEmployee(id: string, input: UpdateEmployeeInput): Promise<boolean> {
    return this.mutate((o) => this.api.updateEmployee(id, input, o), 'تم تعديل بيانات الموظف بنجاح');
  }

  async toggleActive(id: string): Promise<boolean> {
    return this.mutate((o) => this.api.toggleActive(id, o), 'تم تغيير حالة الموظف بنجاح');
  }

  async paySalary(id: string, input: PaySalaryInput): Promise<boolean> {
    return this.mutate((o) => this.api.paySalary(id, input, o), 'تم تسجيل دفع الراتب بنجاح');
  }

  private async mutate(
    call: (options: { context: HttpContext }) => import('rxjs').Observable<unknown>,
    successMessage: string,
  ): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(call({ context: HrStore.NO_TOAST }));
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
