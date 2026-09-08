import { ChangeDetectionStrategy, Component, effect, inject, input, output } from '@angular/core';

import { Button, Dialog, EmptyState, Skeleton } from '../../shared/ui';
import { formatCurrency, formatDateTime } from '../../shared/format/formatters';
import type { SupplierFinancialTransactionResponse } from './suppliers-api.service';
import { SuppliersStore } from './suppliers-store';

/**
 * Read-only details of one supplier financial transaction: header fields (supplier,
 * direction, amount, outstanding balance, currency, date, notes) plus the recorded
 * payment legs (GET /{transactionId}/payments). Opened from the eye button in the
 * financial-transactions table. Closing the dialog clears the payments state.
 */
@Component({
  selector: 'app-transaction-detail-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, EmptyState, Skeleton],
  template: `
    <app-dialog
      [open]="open()"
      title="تفاصيل المعاملة"
      subtitle="الموردون — السلف"
      icon="eye"
      (openChange)="onDismiss()"
    >
      @if (transaction(); as tx) {
        <dl class="mb-4 grid grid-cols-2 gap-3">
          <div class="rounded-lg border border-gray-200 p-3">
            <dt class="text-xs font-medium text-gray-500">المورد</dt>
            <dd class="mt-1 truncate text-sm font-semibold text-gray-900">
              {{ tx.supplierName }}
            </dd>
          </div>
          <div class="rounded-lg border border-gray-200 p-3">
            <dt class="text-xs font-medium text-gray-500">النوع</dt>
            <dd class="mt-1 text-sm font-semibold text-gray-900">{{ tx.directionLabel }}</dd>
          </div>
          <div class="rounded-lg border border-gray-200 p-3">
            <dt class="text-xs font-medium text-gray-500">المبلغ</dt>
            <dd class="mt-1 text-sm font-semibold text-gray-900 data-mono" dir="ltr">
              {{ formatCurrency(Number(tx.amount ?? 0), tx.currency ?? 'JOD') }}
            </dd>
          </div>
          <div class="rounded-lg border border-gray-200 p-3">
            <dt class="text-xs font-medium text-gray-500">المتبقي</dt>
            <dd class="mt-1 text-sm font-semibold text-gray-900 data-mono" dir="ltr">
              {{ tx.outstandingBalanceDisplay }}
            </dd>
          </div>
          <div class="rounded-lg border border-gray-200 p-3">
            <dt class="text-xs font-medium text-gray-500">العملة</dt>
            <dd class="mt-1 text-sm font-semibold text-gray-900">{{ tx.currency }}</dd>
          </div>
          <div class="rounded-lg border border-gray-200 p-3">
            <dt class="text-xs font-medium text-gray-500">التاريخ</dt>
            <dd class="mt-1 text-sm font-semibold text-gray-900">
              {{ formatDateTime(tx.createdAt ?? '') }}
            </dd>
          </div>
        </dl>
        @if (tx.notes) {
          <p class="mb-4 rounded-lg bg-gold-container/30 px-3 py-2 text-sm text-gray-700">
            {{ tx.notes }}
          </p>
        }

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
      }

      <div class="mt-5 flex justify-end border-t border-gray-100 pt-4">
        <app-button variant="secondary" type="button" (clicked)="onDismiss()">إغلاق</app-button>
      </div>
    </app-dialog>
  `,
})
export class TransactionDetailDialog {
  private readonly store = inject(SuppliersStore);

  readonly open = input(false);
  /** The transaction to display; null while no dialog is open. */
  readonly transaction = input<SupplierFinancialTransactionResponse | null>(null);

  readonly openChange = output<boolean>();

  readonly payments = this.store.payments;
  readonly paymentsLoading = this.store.paymentsLoading;
  readonly paymentsError = this.store.paymentsError;

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

  onDismiss(): void {
    this.openChange.emit(false);
  }
}
