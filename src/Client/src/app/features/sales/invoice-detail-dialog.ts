import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
} from '@angular/core';

import { Badge, Button, Dialog } from '../../shared/ui';
import { formatDate } from '../../shared/format/formatters';
import type { SalesInvoiceResponse } from './sales-api.service';
import { SalesStore } from './sales-store';

const STATUS_VARIANT: Readonly<Record<string, 'success' | 'warning' | 'neutral' | 'error'>> = {
  Completed: 'success',
  PartiallyPaid: 'warning',
  Draft: 'neutral',
  Cancelled: 'error',
};

function paymentMethodLabel(method: string | null | undefined): string {
  if (method === 'Cash') {
    return 'نقدي';
  }
  if (method === 'Bank') {
    return 'مصرفي';
  }
  return '—';
}

/**
 * P3.8 — sales invoice detail. Shows the header (number, customer, employee, status, payment
 * method, totals) and the invoice lines (karat / weight / 21K-equivalent / price / gold value).
 * Receives the row from the list for instant render, then refreshes it from `GET /{id}` while
 * open so the data is always current.
 */
@Component({
  selector: 'app-invoice-detail-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Badge, Button, Dialog],
  template: `
    <app-dialog
      [open]="open()"
      [title]="displayInvoice()?.invoiceNumber ?? 'فاتورة مبيعات'"
      subtitle="المبيعات"
      icon="receipt"
      maxWidth="max-w-2xl"
      (openChange)="onDismiss()"
    >
      @if (detailLoading() && detail() === null) {
        <div class="space-y-3 py-4">
          <div class="h-4 w-40 rounded bg-gray-100"></div>
          <div class="h-3 w-full rounded bg-gray-100"></div>
          <div class="h-3 w-2/3 rounded bg-gray-100"></div>
        </div>
      } @else if (detailError(); as error) {
        <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700">
          {{ error.detail ?? 'تعذّر تحميل الفاتورة.' }}
        </p>
      } @else if (displayInvoice(); as invoice) {
        <div class="space-y-4">
          <div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
            <div>
              <p class="text-xs text-gray-500">العميل</p>
              <p class="mt-0.5 text-sm font-semibold text-gray-900">
                {{ invoice.customerName ?? '—' }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">الهاتف</p>
              <p class="mt-0.5 text-sm text-gray-900 data-mono" dir="ltr">
                {{ invoice.customerPhone ?? '—' }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">التاريخ</p>
              <p class="mt-0.5 text-sm text-gray-900 data-mono">
                {{ formatDate(invoice.date ?? '') }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">البائع</p>
              <p class="mt-0.5 text-sm text-gray-900">{{ invoice.userName ?? '—' }}</p>
            </div>
            <div>
              <p class="text-xs text-gray-500">طريقة الدفع</p>
              <p class="mt-0.5 text-sm text-gray-900">
                {{ paymentMethodLabel(invoice.paymentMethod) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">الحالة</p>
              <p class="mt-0.5">
                <app-badge [variant]="statusVariant(invoice.status)">{{
                  invoice.statusLabel ?? invoice.status ?? '—'
                }}</app-badge>
              </p>
            </div>
          </div>

          <div class="overflow-hidden rounded-lg border border-gray-200">
            <div class="overflow-x-auto">
              <table class="w-full min-w-[480px] border-collapse text-sm">
                <thead>
                  <tr class="border-b border-gray-200 bg-gray-50 text-gray-600">
                    <th class="px-3 py-2 text-start font-semibold">العيار</th>
                    <th class="px-3 py-2 text-start font-semibold">الوزن (غ)</th>
                    <th class="px-3 py-2 text-start font-semibold">المكافئ 21ك (غ)</th>
                    <th class="px-3 py-2 text-start font-semibold">السعر/جم</th>
                    <th class="px-3 py-2 text-end font-semibold">قيمة الذهب</th>
                  </tr>
                </thead>
                <tbody>
                  @for (item of invoice.items ?? []; track item.id) {
                    <tr class="border-b border-gray-100 last:border-0">
                      <td class="px-3 py-2 text-gray-900">عيار {{ item.karat ?? '—' }}</td>
                      <td class="px-3 py-2 data-mono" dir="ltr">
                        {{ Number(item.weightInGrams ?? 0).toFixed(3) }}
                      </td>
                      <td class="px-3 py-2 data-mono" dir="ltr">
                        {{ Number(item.equivalent21KWeightInGrams ?? 0).toFixed(3) }}
                      </td>
                      <td class="px-3 py-2 data-mono" dir="ltr">
                        {{ Number(item.pricePerGram ?? 0).toFixed(3) }}
                      </td>
                      <td class="px-3 py-2 text-end font-semibold text-gold data-mono" dir="ltr">
                        {{ Number(item.goldAmount ?? 0).toFixed(3) }}
                      </td>
                    </tr>
                  } @empty {
                    <tr>
                      <td colspan="5" class="px-3 py-6 text-center text-sm text-gray-500">
                        لا توجد عناصر في هذه الفاتورة.
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div>
              <p class="text-xs text-gray-500">الإجمالي</p>
              <p class="mt-0.5 font-semibold text-gray-900 data-mono" dir="ltr">
                {{ Number(invoice.totalAmount ?? 0).toFixed(3) }} {{ invoice.currency ?? '' }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">المدفوع</p>
              <p class="mt-0.5 font-semibold text-emerald-700 data-mono" dir="ltr">
                {{ Number(invoice.amountPaid ?? 0).toFixed(3) }} {{ invoice.currency ?? '' }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">المتبقي</p>
              <p class="mt-0.5 font-semibold text-red-700 data-mono" dir="ltr">
                {{ Number(invoice.remainingBalance ?? 0).toFixed(3) }} {{ invoice.currency ?? '' }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">ملاحظات</p>
              <p class="mt-0.5 text-sm text-gray-900">{{ invoice.notes ?? '—' }}</p>
            </div>
          </div>
        </div>
      }

      <div class="flex justify-end gap-3 pt-4">
        <app-button variant="secondary" type="button" (clicked)="onDismiss()">إغلاق</app-button>
      </div>
    </app-dialog>
  `,
})
export class InvoiceDetailDialog {
  private readonly store = inject(SalesStore);

  readonly open = input(false);
  /** Row from the list — rendered instantly while the fresh detail loads. */
  readonly invoice = input<SalesInvoiceResponse | null>(null);

  readonly openChange = output<boolean>();

  readonly detail = this.store.detail;
  readonly detailLoading = this.store.detailLoading;
  readonly detailError = this.store.detailError;

  readonly displayInvoice = computed(
    () => this.store.detail() ?? this.invoice(),
  );

  readonly formatDate = formatDate;
  readonly Number = Number;
  readonly paymentMethodLabel = paymentMethodLabel;

  constructor() {
    effect(() => {
      if (this.open() && this.invoice()?.id) {
        void this.store.loadDetail(this.invoice()!.id!);
      }
    });
  }

  statusVariant(status: string | undefined): 'success' | 'warning' | 'neutral' | 'error' {
    return STATUS_VARIANT[status ?? ''] ?? 'neutral';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }
}