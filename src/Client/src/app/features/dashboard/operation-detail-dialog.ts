import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { formatCurrency, formatDateTime, formatWeight } from '../../shared/format/formatters';
import { Badge, Dialog, Skeleton } from '../../shared/ui';
import type { ApiError } from '../../core/http/api-error';
import type { StoreOperationDetailResponse } from './store-operations-api.service';
import {
  counterpartyLabel,
  employeeLabel,
  num,
  operationTypeVariant,
  statusBadgeVariant,
} from './store-operations.model';

/**
 * Drawer-style detail view for one store operation (sale or purchase). Fed by
 * `StoreOperationsStore.detail`; renders loading, error+retry and populated states.
 */
@Component({
  selector: 'app-operation-detail-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Badge, Dialog, Skeleton],
  template: `
    <app-dialog [open]="open()" [title]="title()" (openChange)="closed.emit()">
      @if (loading()) {
        <div class="space-y-4">
          <app-skeleton height="1rem" width="60%" />
          <app-skeleton height="0.875rem" width="100%" />
          <app-skeleton height="0.875rem" width="90%" />
          <app-skeleton height="0.875rem" width="70%" />
        </div>
      } @else if (error(); as error) {
        <div class="py-8 text-center">
          <p class="text-sm text-error">تعذّر تحميل تفاصيل العملية.</p>
          <p class="mt-1 text-xs text-gray-500" dir="ltr">{{ error.detail }}</p>
          <button
            type="button"
            class="mt-4 inline-flex items-center gap-2 rounded-input bg-gold px-4 py-2 text-sm font-medium text-gray-900 hover:bg-gold/90"
            (click)="retry.emit()"
          >
            إعادة المحاولة
          </button>
        </div>
      } @else if (detail(); as d) {
        <div class="mb-4 flex flex-wrap items-center gap-2">
          <app-badge [variant]="operationTypeVariant(d.operationType)">
            {{ d.operationTypeLabel }}
          </app-badge>
          @if (d.statusLabel) {
            <app-badge [variant]="statusBadgeVariant(d.status)">
              {{ d.statusLabel }}
            </app-badge>
          }
        </div>

        <dl class="grid grid-cols-2 gap-x-4 gap-y-3">
          <div>
            <dt class="text-xs text-gray-500">التاريخ</dt>
            <dd class="mt-0.5 text-sm text-gray-900">
              {{ d.date ? formatDateTime(d.date) : '—' }}
            </dd>
          </div>
          <div>
            <dt class="text-xs text-gray-500">{{ counterpartyLabel(d.operationType) }}</dt>
            <dd class="mt-0.5 text-sm text-gray-900">{{ d.counterpartyName }}</dd>
          </div>
          <div>
            <dt class="text-xs text-gray-500">الهاتف</dt>
            <dd class="mt-0.5 text-sm text-gray-900" dir="ltr">{{ d.counterpartyPhone ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-gray-500">{{ employeeLabel(d.operationType) }}</dt>
            <dd class="mt-0.5 text-sm text-gray-900">{{ d.employeeName ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-gray-500">الحساب</dt>
            <dd class="mt-0.5 text-sm text-gray-900">{{ d.accountName ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-gray-500">طريقة الدفع</dt>
            <dd class="mt-0.5 text-sm text-gray-900">{{ d.paymentMethodLabel ?? '—' }}</dd>
          </div>
          @if (d.counterpartyIdNumber) {
            <div>
              <dt class="text-xs text-gray-500">رقم الهوية</dt>
              <dd class="mt-0.5 text-sm text-gray-900" dir="ltr">{{ d.counterpartyIdNumber }}</dd>
            </div>
          }
          @if (d.counterpartyYearOfBirth) {
            <div>
              <dt class="text-xs text-gray-500">سنة الميلاد</dt>
              <dd class="mt-0.5 text-sm text-gray-900">{{ num(d.counterpartyYearOfBirth) }}</dd>
            </div>
          }
          @if (d.counterpartyAddress) {
            <div class="col-span-2">
              <dt class="text-xs text-gray-500">العنوان</dt>
              <dd class="mt-0.5 text-sm text-gray-900">{{ d.counterpartyAddress }}</dd>
            </div>
          }
          @if (d.accountNumber) {
            <div>
              <dt class="text-xs text-gray-500">رقم الحساب</dt>
              <dd class="mt-0.5 text-sm text-gray-900" dir="ltr">{{ d.accountNumber }}</dd>
            </div>
          }
        </dl>

        <div class="mt-5 grid grid-cols-3 gap-3 rounded-lg bg-surface p-3">
          <div>
            <dt class="text-xs text-gray-500">الإجمالي</dt>
            <dd class="mt-0.5 text-sm font-semibold text-gray-900" data-mono>
              {{ formatCurrency(num(d.totalAmount), d.currency) }}
            </dd>
          </div>
          <div>
            <dt class="text-xs text-gray-500">المدفوع</dt>
            <dd class="mt-0.5 text-sm font-semibold text-gray-900" data-mono>
              {{ formatCurrency(num(d.amountPaid), d.currency) }}
            </dd>
          </div>
          <div>
            <dt class="text-xs text-gray-500">المتبقي</dt>
            <dd class="mt-0.5 text-sm font-semibold text-gray-900" data-mono>
              {{ formatCurrency(num(d.remainingBalance), d.currency) }}
            </dd>
          </div>
        </div>

        <div class="mt-5">
          <h3 class="mb-2 text-sm font-semibold text-gray-700">الأصناف ({{ itemCount(d) }})</h3>
          <div class="overflow-x-auto rounded-lg border border-gray-200">
            <table class="w-full border-collapse text-sm">
              <thead>
                <tr class="bg-surface">
                  <th class="px-3 py-2 text-start font-semibold text-gray-600">العيار</th>
                  <th class="px-3 py-2 text-start font-semibold text-gray-600">الوزن (غ)</th>
                  <th class="px-3 py-2 text-start font-semibold text-gray-600">معادل 21</th>
                  <th class="px-3 py-2 text-start font-semibold text-gray-600">سعر الغرام</th>
                  <th class="px-3 py-2 text-start font-semibold text-gray-600">قيمة الذهب</th>
                  <th class="px-3 py-2 text-start font-semibold text-gray-600">التصنيف</th>
                </tr>
              </thead>
              <tbody>
                @for (item of d.items ?? []; track item.id) {
                  <tr class="border-t border-gray-100">
                    <td class="px-3 py-2 data-mono" dir="ltr">{{ num(item.karat) }}K</td>
                    <td class="px-3 py-2 data-mono">{{ formatWeight(num(item.weightInGrams)) }}</td>
                    <td class="px-3 py-2 data-mono">
                      {{ formatWeight(num(item.equivalent21KWeightInGrams)) }}
                    </td>
                    <td class="px-3 py-2 data-mono">
                      {{ formatCurrency(num(item.pricePerGram), d.currency) }}
                    </td>
                    <td class="px-3 py-2 data-mono">
                      {{ formatCurrency(num(item.goldAmount), d.currency) }}
                    </td>
                    <td class="px-3 py-2">{{ item.categoryName ?? '—' }}</td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="6" class="px-3 py-6 text-center text-gray-500">لا توجد أصناف</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>

        @if (d.notes) {
          <div class="mt-4 rounded-lg bg-surface p-3">
            <p class="text-xs text-gray-500">ملاحظات</p>
            <p class="mt-1 text-sm text-gray-900">{{ d.notes }}</p>
          </div>
        }
      }
    </app-dialog>
  `,
})
export class OperationDetailDialog {
  readonly open = input(false);
  readonly detail = input<StoreOperationDetailResponse | null>(null);
  readonly loading = input(false);
  readonly error = input<ApiError | null>(null);

  readonly closed = output<void>();
  readonly retry = output<void>();

  readonly title = computed(() => this.detail()?.invoiceNumber ?? 'تفاصيل العملية');

  readonly num = num;
  readonly formatCurrency = formatCurrency;
  readonly formatWeight = formatWeight;
  readonly formatDateTime = formatDateTime;
  readonly counterpartyLabel = counterpartyLabel;
  readonly employeeLabel = employeeLabel;
  readonly statusBadgeVariant = statusBadgeVariant;
  readonly operationTypeVariant = operationTypeVariant;

  itemCount(detail: StoreOperationDetailResponse): number {
    return detail.items?.length ?? 0;
  }
}
