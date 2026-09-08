import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormField, form, required, submit } from '@angular/forms/signals';

import type { ApiError } from '../../core/http/api-error';
import { Button, Dialog, EmptyState, Skeleton } from '../../shared/ui';
import { formatCurrency, formatDateTime } from '../../shared/format/formatters';
import { filterAccountsByCurrency } from '../../shared/finance/account-filters';
import type { SupplierFinancialTransactionResponse } from './suppliers-api.service';
import { SuppliersStore } from './suppliers-store';

/** First validation message from the server's RFC 9457 `errors` object, if any. */
function firstValidationMessage(error: ApiError): string | null {
  for (const messages of Object.values(error.validation ?? {})) {
    const message = messages[0];
    if (message !== undefined) {
      return message;
    }
  }
  return null;
}

/** `yyyy-mm-dd` for the date input, local time. */
function todayInputValue(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/** Converts a `yyyy-mm-dd` input value to a UTC date-time string (local midnight). */
function toIsoDateTime(dateInput: string): string {
  return new Date(`${dateInput}T00:00:00`).toISOString();
}

/**
 * P3.6 — payments of one supplier financial transaction: the existing payment legs
 * (GET /{transactionId}/payments) plus a small "record a payment" form. Closing the dialog
 * clears the payments state. Requires account options (best-effort, `feature: finance` gate).
 */
@Component({
  selector: 'app-transaction-payments-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, EmptyState, FormField, Skeleton],
  template: `
    <app-dialog
      [open]="open()"
      title="دفعات المعاملة"
      subtitle="الموردون — السلف"
      icon="wallet"
      (openChange)="onDismiss()"
    >
      @if (transaction(); as tx) {
        <p class="mb-4 rounded-lg bg-gold-container/30 px-3 py-2 text-sm text-gray-700">
          <span class="font-semibold">{{ tx.supplierName }}</span>
          — {{ tx.directionLabel }} · المتبقي
          <span class="data-mono" dir="ltr">{{ tx.outstandingBalanceDisplay }}</span>
        </p>
      }

      <div>
        <p class="mb-2 text-sm font-medium text-gray-700">الدفعات المسجلة</p>
        @if (paymentsLoading()) {
          <div class="space-y-2">
            <app-skeleton height="2rem" />
            <app-skeleton height="2rem" />
          </div>
        } @else if (paymentsError(); as error) {
          <app-empty-state
            icon="alert-circle"
            title="تعذّر تحميل الدفعات"
            [description]="error.detail ?? ''"
          >
            <app-button variant="secondary" size="sm" (clicked)="retryLoad()"
              >إعادة المحاولة</app-button
            >
          </app-empty-state>
        } @else if (payments().length === 0) {
          <p
            class="rounded-lg border border-dashed border-gray-300 px-3 py-4 text-center text-sm text-gray-500"
          >
            لا توجد دفعات بعد.
          </p>
        } @else {
          <div class="divide-y divide-gray-100 rounded-lg border border-gray-200">
            @for (payment of payments(); track payment.id) {
              <div class="flex items-center justify-between gap-3 px-3 py-2.5">
                <div class="min-w-0">
                  <p class="truncate text-sm font-medium text-gray-800">
                    {{ payment.accountName }}
                  </p>
                  <p class="text-xs text-gray-500">{{ formatDateTime(payment.date ?? '') }}</p>
                </div>
                <p class="shrink-0 text-sm font-semibold text-gray-900 data-mono" dir="ltr">
                  {{
                    formatCurrency(Number(payment.amount ?? 0), transaction()?.currency ?? 'JOD')
                  }}
                </p>
              </div>
            }
          </div>
        }
      </div>

      <form
        class="mt-5 space-y-4 border-t border-gray-100 pt-5"
        novalidate
        (submit)="onSubmit(); $event.preventDefault()"
      >
        <p class="text-sm font-medium text-gray-700">تسجيل دفعة جديدة</p>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="payment-account"
            >الحساب المالي</label
          >
          <select
            id="payment-account"
            [formField]="paymentForm.accountId"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">اختر حساباً</option>
            @for (account of activeAccounts(); track account.id) {
              <option [value]="account.id">
                {{ account.name }} ({{ account.currency ?? '' }})
              </option>
            }
          </select>
          @if (paymentForm.accountId().touched() && paymentForm.accountId().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ paymentForm.accountId().errors()[0].message }}</p>
          }
          @if (accountsError(); as message) {
            <p class="mt-1 text-xs text-gray-500" role="alert">{{ message }}</p>
          }
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="payment-amount"
              >المبلغ</label
            >
            <input
              id="payment-amount"
              type="number"
              dir="ltr"
              step="0.001"
              [formField]="paymentForm.amount"
              placeholder="0.000"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (amountError()) {
              <p class="mt-1 text-xs text-error">{{ amountError() }}</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="payment-date"
              >التاريخ</label
            >
            <input
              id="payment-date"
              type="date"
              [formField]="paymentForm.date"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="payment-notes"
            >ملاحظات</label
          >
          <input
            id="payment-notes"
            type="text"
            autocomplete="off"
            [formField]="paymentForm.notes"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
        </div>

        @if (saveError(); as error) {
          <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ errorMessage(error) }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button
            variant="secondary"
            type="button"
            [disabled]="saving()"
            (clicked)="onDismiss()"
          >
            إغلاق
          </app-button>
          <app-button type="submit" [loading]="saving()">تسجيل الدفعة</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class TransactionPaymentsDialog {
  private readonly store = inject(SuppliersStore);

  readonly open = input(false);
  /** The transaction whose payments are shown; null while no dialog is open. */
  readonly transaction = input<SupplierFinancialTransactionResponse | null>(null);

  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;
  readonly payments = this.store.payments;
  readonly paymentsLoading = this.store.paymentsLoading;
  readonly paymentsError = this.store.paymentsError;
  readonly accounts = this.store.accounts;
  readonly accountsError = this.store.accountsError;

  readonly activeAccounts = computed(() =>
    filterAccountsByCurrency(
      (this.accounts() ?? []).filter((account) => account.isActive !== false),
      this.transaction()?.currency ?? null,
    ),
  );

  readonly amountError = signal<string | null>(null);

  readonly draft = signal({ accountId: '', amount: '', date: todayInputValue(), notes: '' });
  readonly paymentForm = form(this.draft, (schema) => {
    required(schema.accountId, { message: 'اختر الحساب المالي.' });
    required(schema.date, { message: 'اختر التاريخ.' });
  });

  readonly formatCurrency = formatCurrency;
  readonly formatDateTime = formatDateTime;
  readonly Number = Number;

  constructor() {
    effect(() => {
      if (this.open()) {
        const tx = this.transaction();
        if (tx !== null) {
          void this.store.loadPayments(tx.id ?? '');
        }
        void this.store.ensureAccounts();
        this.draft.set({ accountId: '', amount: '', date: todayInputValue(), notes: '' });
        this.amountError.set(null);
        this.store.clearSaveError();
      } else {
        this.store.clearPayments();
      }
    });
  }

  retryLoad(): void {
    const tx = this.transaction();
    if (tx !== null) {
      void this.store.loadPayments(tx.id ?? '');
    }
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  onSubmit(): void {
    submit(this.paymentForm, async () => {
      const amount = Number(this.draft().amount);
      if (this.draft().amount === '' || !Number.isFinite(amount) || amount <= 0) {
        this.amountError.set('أدخل مبلغاً أكبر من صفر.');
        return;
      }
      this.amountError.set(null);
      const tx = this.transaction();
      if (tx === null) {
        return;
      }
      const draft = this.draft();
      const ok = await this.store.createPayment(tx.id ?? '', {
        accountId: draft.accountId,
        amount,
        date: toIsoDateTime(draft.date),
        notes: draft.notes.trim() || null,
      });
      if (ok) {
        this.saved.emit();
        void this.store.loadPayments(tx.id ?? '');
      }
    });
  }
}
