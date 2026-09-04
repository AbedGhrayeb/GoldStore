import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import { Button, Dialog, EmptyState, Skeleton } from '../../shared/ui';
import { formatCurrency, formatDateTime, formatWeight } from '../../shared/format/formatters';
import { SuppliersStore } from './suppliers-store';

/**
 * P3.6 — supplier detail dialog: balances (gold 21K, manufacturing, per-currency financial)
 * and recent transactions, fed by GET /suppliers/{id}. Loaded by the page when a row's
 * view action is clicked.
 */
@Component({
  selector: 'app-supplier-detail-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, EmptyState, LucideAngularModule, Skeleton],
  template: `
    <app-dialog
      [open]="open()"
      title="تفاصيل المورد"
      subtitle="الموردون"
      icon="truck"
      (openChange)="onDismiss()"
    >
      @if (loading() && detail() === null) {
        <div class="space-y-3">
          <app-skeleton height="2.5rem" />
          <app-skeleton height="2.5rem" />
          <app-skeleton height="8rem" />
        </div>
      } @else if (error(); as error) {
        <app-empty-state
          icon="alert-circle"
          title="تعذّر تحميل التفاصيل"
          [description]="error.detail ?? ''"
        >
          <app-button variant="secondary" size="sm" (clicked)="retry()">إعادة المحاولة</app-button>
        </app-empty-state>
      } @else if (detail(); as detail) {
        <div class="space-y-4">
          <div class="flex items-center justify-between gap-3">
            <div class="min-w-0">
              <h3 class="truncate text-base font-semibold text-gray-900">{{ detail.name }}</h3>
              @if (detail.primaryPhone; as phone) {
                <p class="mt-0.5 text-sm text-gray-600" dir="ltr">{{ phone }}</p>
              }
            </div>
            @if (detail.isActive) {
              <span class="inline-flex items-center rounded-full bg-success/15 px-2.5 py-1 text-xs font-medium text-emerald-700"
                >نشط</span
              >
            } @else {
              <span class="inline-flex items-center rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-600"
                >متوقف</span
              >
            }
          </div>

          <dl class="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div class="rounded-lg border border-gray-200 p-3">
              <dt class="text-xs font-medium text-gray-500">رصيد الذهب (21ك)</dt>
              <dd class="mt-1 text-lg font-semibold text-gray-900 data-mono">
                {{ formatWeight(Number(detail.goldBalance ?? 0)) }}
                <span class="text-xs font-normal text-gray-500">غ</span>
              </dd>
            </div>
            <div class="rounded-lg border border-gray-200 p-3">
              <dt class="text-xs font-medium text-gray-500">أجور التصنيع</dt>
              <dd class="mt-1 text-lg font-semibold text-gray-900 data-mono">
                {{ formatWeight(Number(detail.manufacturingBalance ?? 0)) }}
              </dd>
            </div>
            <div class="rounded-lg border border-gray-200 p-3">
              <dt class="text-xs font-medium text-gray-500">الأرصدة المالية</dt>
              <dd class="mt-1 space-y-0.5">
                @for (balance of detail.financialBalancesByCurrency ?? []; track balance.currency) {
                  <p class="text-sm font-semibold text-gray-900 data-mono">
                    {{ formatCurrency(Number(balance.balance ?? 0), balance.currency ?? 'JOD') }}
                  </p>
                } @empty {
                  <p class="text-sm font-semibold text-gray-400 data-mono">0.000</p>
                }
              </dd>
            </div>
          </dl>

          <div>
            <p class="mb-2 text-sm font-medium text-gray-700">آخر العمليات</p>
            @if ((detail.recentTransactions ?? []).length === 0) {
              <p class="rounded-lg border border-dashed border-gray-300 px-3 py-4 text-center text-sm text-gray-500">
                لا توجد عمليات بعد.
              </p>
            } @else {
              <div class="max-h-64 divide-y divide-gray-100 overflow-y-auto rounded-lg border border-gray-200">
                @for (transaction of detail.recentTransactions ?? []; track transaction.id) {
                  <div class="flex items-center justify-between gap-3 px-3 py-2.5">
                    <div class="min-w-0">
                      <p class="truncate text-sm font-medium text-gray-800">
                        {{ transaction.description }}
                      </p>
                      <p class="text-xs text-gray-500">{{ formatDateTime(transaction.date ?? '') }}</p>
                    </div>
                    <div class="shrink-0 text-end">
                      <p class="text-sm font-semibold text-gray-900 data-mono">
                        {{ formatWeight(Number(transaction.amount ?? 0)) }}
                        @if (transaction.unit; as unit) {
                          <span class="text-xs font-normal text-gray-500">{{ unit }}</span>
                        }
                      </p>
                      <p class="text-xs text-gray-500">{{ transaction.type }}</p>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        </div>
      }

      <div class="flex justify-end gap-3 pt-4">
        <app-button variant="secondary" type="button" (clicked)="onDismiss()">إغلاق</app-button>
      </div>
    </app-dialog>
  `,
})
export class SupplierDetailDialog {
  private readonly store = inject(SuppliersStore);

  readonly open = input(false);
  /** Supplier row whose detail is loaded on open. */
  readonly supplierId = input<string | null>(null);

  readonly detail = this.store.detail;
  readonly loading = this.store.detailLoading;
  readonly error = this.store.detailError;

  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly formatWeight = formatWeight;
  readonly formatCurrency = formatCurrency;
  readonly formatDateTime = formatDateTime;
  readonly Number = Number;

  retry(): void {
    const id = this.supplierId();
    if (id !== null) {
      void this.store.loadDetail(id);
    }
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }
}