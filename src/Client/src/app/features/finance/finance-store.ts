import { HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom, type Observable } from 'rxjs';

import { isApiError, toApiError, type ApiError } from '../../core/http/api-error';
import { SKIP_ERROR_TOAST } from '../../core/http/error.interceptor';
import { ToastStore } from '../../core/toast/toast-store';
import {
  FinanceApi,
  type AccountWithBalanceResponse,
  type AccountsWithBalancesQuery,
  type CreateAccountInput,
  type CreateDebtInput,
  type DebtKpiResponse,
  type DebtPaymentInput,
  type DebtsQuery,
  type PaginatedDebts,
  type PaginatedTransactions,
  type SetBalanceInput,
  type TransactionsQuery,
} from './finance-api.service';

/** Guard that normalizes any thrown error (already-normalized ApiError or raw HttpErrorResponse). */
function asApiError(error: unknown): ApiError {
  return isApiError(error) ? error : toApiError(error);
}

/**
 * Signal-backed facade for the finance feature (`feature: finance`). Owns the accounts list
 * with derived balances, the debt KPIs, the paged debts table and the paged financial
 * transactions table, plus the create-account / set-balance / create-debt / pay-debt
 * mutations. Balances are ledger-derived server-side, so every successful mutation is
 * followed by a page-level reload of the affected lists. Components consume only this
 * store — never {@link FinanceApi} directly.
 */
@Injectable({ providedIn: 'root' })
export class FinanceStore {
  private readonly api = inject(FinanceApi);
  private readonly toasts = inject(ToastStore);
  private static readonly NO_TOAST = new HttpContext().set(SKIP_ERROR_TOAST, true);

  private readonly accountsSignal = signal<AccountWithBalanceResponse[] | null>(null);
  private readonly accountsLoadingSignal = signal(false);
  private readonly accountsErrorSignal = signal<ApiError | null>(null);

  private readonly debtKpisSignal = signal<DebtKpiResponse | null>(null);
  private readonly debtKpisLoadingSignal = signal(false);

  private readonly debtsPageSignal = signal<PaginatedDebts | null>(null);
  private readonly debtsLoadingSignal = signal(false);
  private readonly debtsErrorSignal = signal<ApiError | null>(null);

  private readonly txPageSignal = signal<PaginatedTransactions | null>(null);
  private readonly txLoadingSignal = signal(false);
  private readonly txErrorSignal = signal<ApiError | null>(null);

  private readonly savingSignal = signal(false);
  private readonly saveErrorSignal = signal<ApiError | null>(null);

  readonly accounts = this.accountsSignal.asReadonly();
  readonly accountsLoading = this.accountsLoadingSignal.asReadonly();
  readonly accountsError = this.accountsErrorSignal.asReadonly();
  readonly debtKpis = this.debtKpisSignal.asReadonly();
  readonly debtKpisLoading = this.debtKpisLoadingSignal.asReadonly();
  readonly debtsPage = this.debtsPageSignal.asReadonly();
  readonly debtsLoading = this.debtsLoadingSignal.asReadonly();
  readonly debtsError = this.debtsErrorSignal.asReadonly();
  readonly txPage = this.txPageSignal.asReadonly();
  readonly txLoading = this.txLoadingSignal.asReadonly();
  readonly txError = this.txErrorSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly saveError = this.saveErrorSignal.asReadonly();

  async loadAccounts(query: AccountsWithBalancesQuery = {}): Promise<void> {
    this.accountsLoadingSignal.set(true);
    this.accountsErrorSignal.set(null);
    try {
      this.accountsSignal.set(
        await firstValueFrom(
          this.api.getAccountsWithBalances(query, { context: FinanceStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.accountsErrorSignal.set(asApiError(error));
    } finally {
      this.accountsLoadingSignal.set(false);
    }
  }

  async loadDebtKpis(): Promise<void> {
    this.debtKpisLoadingSignal.set(true);
    try {
      this.debtKpisSignal.set(await firstValueFrom(this.api.getDebtKpis()));
    } catch {
      // KPIs are cosmetic; the table remains the source of truth.
    } finally {
      this.debtKpisLoadingSignal.set(false);
    }
  }

  async loadDebts(query: DebtsQuery): Promise<void> {
    this.debtsLoadingSignal.set(true);
    this.debtsErrorSignal.set(null);
    try {
      this.debtsPageSignal.set(
        await firstValueFrom(
          this.api.getDebts(query, { context: FinanceStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.debtsErrorSignal.set(asApiError(error));
    } finally {
      this.debtsLoadingSignal.set(false);
    }
  }

  async loadTransactions(query: TransactionsQuery): Promise<void> {
    this.txLoadingSignal.set(true);
    this.txErrorSignal.set(null);
    try {
      this.txPageSignal.set(
        await firstValueFrom(
          this.api.getTransactions(query, { context: FinanceStore.NO_TOAST }),
        ),
      );
    } catch (error) {
      this.txErrorSignal.set(asApiError(error));
    } finally {
      this.txLoadingSignal.set(false);
    }
  }

  clearSaveError(): void {
    this.saveErrorSignal.set(null);
  }

  async createAccount(input: CreateAccountInput): Promise<boolean> {
    return this.mutate((options) => this.api.createAccount(input, options), 'تم إنشاء الحساب بنجاح');
  }

  async setBalance(id: string, input: SetBalanceInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.setBalance(id, input, options),
      'تم تعديل رصيد الحساب بنجاح',
    );
  }

  async createDebt(input: CreateDebtInput): Promise<boolean> {
    return this.mutate((options) => this.api.createDebt(input, options), 'تم إنشاء الذمة بنجاح');
  }

  async payDebt(id: string, input: DebtPaymentInput): Promise<boolean> {
    return this.mutate(
      (options) => this.api.payDebt(id, input, options),
      'تم تسجيل الدفعة بنجاح',
    );
  }

  private async mutate(
    call: (options: { context: HttpContext }) => Observable<unknown>,
    successMessage: string,
  ): Promise<boolean> {
    this.savingSignal.set(true);
    this.saveErrorSignal.set(null);
    try {
      await firstValueFrom(call({ context: FinanceStore.NO_TOAST }));
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
