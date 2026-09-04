import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AuthStore } from '../../core/auth/auth-store';

import {
  Badge,
  Button,
  Card,
  EmptyState,
  KpiCard,
  RetryButton,
  Skeleton,
} from '../../shared/ui';
import { formatDate, formatDateTime, formatWeight } from '../../shared/format/formatters';
import type {
  AdjustmentsQuery,
  GoldLedgerEntryResponse,
  GoldLedgerQuery,
  InventoryAdjustmentResponse,
} from './inventory-api.service';
import { AdjustmentDialog } from './adjustment-dialog';
import { InventoryStore } from './inventory-store';

const PAGE_SIZE = 15;
const SKELETON_TABLE_COLUMNS = [0, 1, 2, 3, 4, 5, 6, 7];

const REFERENCE_TYPES: ReadonlyArray<{ value: string; label: string }> = [
  { value: 'SupplierDelivery', label: 'توريد مورد' },
  { value: 'CustomerGoldPurchase', label: 'شراء ذهب عميل' },
  { value: 'Sale', label: 'بيع' },
  { value: 'SupplierScrapPayment', label: 'دفع كسر مورد' },
  { value: 'InventoryAdjustment', label: 'تسوية جردية' },
];

/** Display row for the adjustments table (formatted date + signed weight texts). */
export interface AdjustmentTableRow extends InventoryAdjustmentResponse {
  dateText: string;
  signedWeightText: string;
  weightText: string;
  equivalentText: string;
}

/** Display row for the gold-ledger table (formatted date + weight texts). */
export interface LedgerTableRow extends GoldLedgerEntryResponse {
  dateText: string;
  weightText: string;
  equivalentText: string;
}

function signedWeightText(grams: number): string {
  return `${grams >= 0 ? '+' : ''}${formatWeight(grams)}`;
}

function toAdjustmentRow(adjustment: InventoryAdjustmentResponse): AdjustmentTableRow {
  return {
    ...adjustment,
    dateText: formatDateTime(adjustment.date ?? ''),
    signedWeightText: signedWeightText(Number(adjustment.signedWeight ?? 0)),
    weightText: formatWeight(Number(adjustment.weightInGrams ?? 0)),
    equivalentText: formatWeight(Number(adjustment.equivalent21KWeightInGrams ?? 0)),
  };
}

function toLedgerRow(entry: GoldLedgerEntryResponse): LedgerTableRow {
  return {
    ...entry,
    dateText: formatDateTime(entry.date ?? ''),
    weightText: formatWeight(Number(entry.weightInGrams ?? 0)),
    equivalentText: formatWeight(Number(entry.equivalent21KWeightInGrams ?? 0)),
  };
}

/**
 * P3.7 — Inventory (`feature: inventory`). One page for the whole feature: inventory KPIs
 * (21K-equivalent total + estimated value + per-karat breakdowns), today's adjustment KPIs,
 * a 7/14/30-day gold-flow trend chart (pure CSS bars — no chart library), the paged
 * adjustments table (type/date filters + create via {@link AdjustmentDialog}), and the
 * paged gold ledger (karat/date/reference-type filters). Balances are never trusted from
 * the client — the server derives everything from ledger entries.
 */
@Component({
  selector: 'app-inventory-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AdjustmentDialog,
    Badge,
    Button,
    Card,
    EmptyState,
    KpiCard,
    RetryButton,
    Skeleton,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">المخزون</h1>
          <p class="mt-1 text-sm text-gray-600">تسويات المخزون، سجل حركة الذهب، ورصيد المعادل 21ك.</p>
        </div>
        <app-button
          icon="plus"
          [disabled]="!canManageInventory()"
          [title]="!canManageInventory() ? 'ليس لديك صلاحية إنشاء تسوية' : ''"
          (clicked)="canManageInventory() && dialogOpen.set(true)">تسوية جردية</app-button>
      </div>

      @if (inventoryKpisLoading() && inventoryKpis() === null) {
        <section class="grid grid-cols-1 gap-4 lg:grid-cols-3">
          @for (column of skeletonTableColumns; track column) {
            <app-skeleton height="11rem" />
          }
        </section>
      } @else if (inventoryKpis(); as kpis) {
        <section class="grid grid-cols-1 gap-4 lg:grid-cols-3">
          <app-card>
            <p class="text-xs font-medium text-gray-500">إجمالي المخزون (معادل 21ك)</p>
            <div class="mt-3 flex items-baseline gap-1">
              <p class="text-4xl font-bold text-gray-900 data-mono">
                {{ kpis.totalEquivalent21KDisplay ?? '0.000' }}
              </p>
              <span class="text-sm text-gray-500">{{ kpis.totalEquivalent21KUnit ?? 'جم' }}</span>
            </div>
            <div class="my-4 h-px bg-gray-100"></div>
            <div class="flex items-center justify-between text-sm">
              <span class="text-gray-500">القيمة التقديرية</span>
              <span class="data-mono font-medium">{{ kpis.estimatedValueDisplay ?? '—' }} د.أ</span>
            </div>
          </app-card>

          @for (breakdown of kpis.karatBreakdowns ?? []; track breakdown.karat) {
            <app-card>
              <div class="flex items-center justify-between">
                <p class="text-xs font-medium text-gray-500">{{ breakdown.karatLabel }}</p>
                @if (breakdown.isPrimary) {
                  <app-badge variant="gold">الرئيسي</app-badge>
                }
              </div>
              <p class="mt-3 text-2xl font-semibold text-gray-900 data-mono">
                {{ breakdown.totalWeightDisplay ?? '0.000' }}
                <span class="ms-1 text-sm font-normal text-gray-500">جم</span>
              </p>
              <p class="mt-2 text-xs text-gray-500">{{ breakdown.description }}</p>
            </app-card>
          }
        </section>
      }

      <section class="grid grid-cols-1 gap-4 md:grid-cols-2">
        @if (adjustmentKpisLoading() && adjustmentKpis() === null) {
          <app-skeleton height="7rem" />
          <app-skeleton height="7rem" />
        } @else {
          <app-kpi-card
            title="تسويات اليوم"
            [value]="adjustmentKpis()?.todayCount ?? '—'"
            icon="boxes"
          />
          <app-kpi-card
            title="صافي التغير اليوم (غ)"
            [value]="adjustmentKpis()?.netWeightDisplay ?? '—'"
            [icon]="adjustmentKpis()?.isNegative ? 'trending-down' : 'trending-up'"
          />
        }
      </section>

      <app-card title="سجل حركة الذهب — آخر {{ trendDays() }} أيام">
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div class="flex items-center gap-4 text-xs text-gray-500">
            <span class="flex items-center gap-1.5">
              <span class="h-2.5 w-2.5 rounded-sm bg-gold"></span>
              دخول
            </span>
            <span class="flex items-center gap-1.5">
              <span class="h-2.5 w-2.5 rounded-sm bg-red-500"></span>
              خروج
            </span>
            <span>معادل 21ك (غ)</span>
          </div>
          <select
            [value]="trendDays()"
            (change)="setTrendDays($any($event.target).value)"
            class="rounded-input border border-gray-300 bg-white px-3 py-1.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            aria-label="فترة الرسم البياني"
          >
            <option value="7">7 أيام</option>
            <option value="14">14 يوماً</option>
            <option value="30">30 يوماً</option>
          </select>
        </div>

        @if (trendLoading() && trend() === null) {
          <app-skeleton height="13rem" />
        } @else if (trendError(); as error) {
          <app-empty-state
            icon="alert-circle"
            title="تعذّر تحميل الرسم البياني"
            [description]="error.detail ?? ''"
          >
            <app-retry-button (retry)="reloadTrend()" />
          </app-empty-state>
        } @else if (chartPoints().length === 0) {
          <app-empty-state
            icon="trending-up"
            title="لا توجد بيانات للرسم البياني"
            description="ستظهر حركة الذهب هنا عند تسجيل أول عملية."
          />
        } @else {
          <div class="flex h-52 items-end gap-1.5">
            @for (point of chartPoints(); track point.date) {
              <div class="flex h-full flex-1 flex-col items-center justify-end gap-1">
                <div class="flex w-full flex-1 items-end justify-center gap-1">
                  <div
                    class="w-2.5 rounded-t-sm bg-gold transition-all"
                    [style.height.%]="point.inPercent"
                    [title]="'دخول ' + point.inText + ' غ'"
                  ></div>
                  <div
                    class="w-2.5 rounded-t-sm bg-red-500 transition-all"
                    [style.height.%]="point.outPercent"
                    [title]="'خروج ' + point.outText + ' غ'"
                  ></div>
                </div>
                <span class="text-[10px] text-gray-500">{{ point.label }}</span>
              </div>
            }
          </div>
          <p class="mt-3 text-xs text-gray-500">
            الصافي: <span class="data-mono font-medium">{{ trendNetText() }} غ</span> خلال الفترة
          </p>
        }
      </app-card>

      <app-card title="التسويات الجردية">
        <form class="mb-4 space-y-4" novalidate (submit)="applyAdjustmentFilters(); $event.preventDefault()">
          <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">النوع</span>
              <select
                [value]="adjustmentTypeFilter()"
                (change)="adjustmentTypeFilter.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">الكل</option>
                @for (option of adjustmentTypeOptions; track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">من تاريخ</span>
              <input
                type="date"
                [value]="adjustmentFromDate()"
                (change)="adjustmentFromDate.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">إلى تاريخ</span>
              <input
                type="date"
                [value]="adjustmentToDate()"
                (change)="adjustmentToDate.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>
          </div>

          <div class="flex flex-wrap gap-3">
            <app-button type="submit" icon="filter">تصفية</app-button>
            <app-button variant="secondary" type="button" (clicked)="resetAdjustmentFilters()">
              إعادة تعيين
            </app-button>
          </div>
        </form>

        <div class="overflow-x-auto">
          <table class="w-full border-collapse text-sm">
            <thead>
              <tr class="border-b border-gray-200">
                <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">النوع</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">العيار</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الوزن (غ)</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">معادل 21ك</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">السبب</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">ملاحظات</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المستخدم</th>
              </tr>
            </thead>
            <tbody>
              @if (adjustmentsLoading()) {
                @for (row of skeletonRows; track $index) {
                  <tr class="border-b border-gray-100">
                    @for (column of skeletonTableColumns; track column) {
                      <td class="px-4 py-3"><app-skeleton height="1rem" /></td>
                    }
                  </tr>
                }
              } @else if (adjustmentsError(); as error) {
                <tr>
                  <td colspan="8" class="px-4 py-10">
                    <app-empty-state
                      icon="alert-circle"
                      title="تعذّر تحميل التسويات"
                      [description]="error.detail ?? ''"
                    >
                      <app-retry-button (retry)="reloadAdjustments()" />
                    </app-empty-state>
                  </td>
                </tr>
              } @else if (adjustmentRows().length === 0) {
                <tr>
                  <td colspan="8" class="px-4 py-10">
                    <app-empty-state
                      icon="inbox"
                      title="لا توجد تسويات جردية"
                      description="سجّل أول تسوية جردية عبر زر «تسوية جردية»."
                    />
                  </td>
                </tr>
              } @else {
                @for (adjustment of adjustmentRows(); track adjustment.id) {
                  <tr class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15">
                    <td class="px-4 py-3 data-mono">{{ adjustment.dateText }}</td>
                    <td class="px-4 py-3">
                      @switch (adjustment.type) {
                        @case ('Increase') {
                          <app-badge variant="success">{{ adjustment.typeLabel }}</app-badge>
                        }
                        @case ('Correction') {
                          <app-badge variant="neutral">{{ adjustment.typeLabel }}</app-badge>
                        }
                        @default {
                          <app-badge variant="error">{{ adjustment.typeLabel }}</app-badge>
                        }
                      }
                    </td>
                    <td class="px-4 py-3">{{ adjustment.karat }}</td>
                    <td class="px-4 py-3">
                      <span class="data-mono font-medium" [class.text-emerald-600]="Number(adjustment.signedWeight ?? 0) >= 0" [class.text-red-600]="Number(adjustment.signedWeight ?? 0) < 0">
                        {{ adjustment.signedWeightText }}
                      </span>
                    </td>
                    <td class="px-4 py-3 data-mono">{{ adjustment.equivalentText }}</td>
                    <td class="max-w-48 truncate px-4 py-3" title="{{ adjustment.reason }}">{{ adjustment.reason }}</td>
                    <td class="max-w-48 truncate px-4 py-3 text-gray-600" title="{{ adjustment.notes ?? '' }}">
                      {{ adjustment.notes ?? '—' }}
                    </td>
                    <td class="px-4 py-3 text-gray-600">{{ adjustment.userName }}</td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        @if (adjustmentsTotalPages() > 1) {
          <div class="mt-4 flex items-center justify-between border-t border-gray-100 pt-4">
            <span class="text-xs text-gray-500 data-mono">
              {{ adjustmentsTotalCount() }} تسوية — صفحة {{ adjustmentsCurrentPage() }} من {{ adjustmentsTotalPages() }}
            </span>
            <div class="flex gap-2">
              <app-button
                variant="secondary"
                size="sm"
                icon="chevron-right"
                [disabled]="adjustmentsCurrentPage() <= 1"
                (clicked)="goToAdjustmentsPage(adjustmentsCurrentPage() - 1)"
              >
                السابق
              </app-button>
              <app-button
                variant="secondary"
                size="sm"
                icon="chevron-left"
                [disabled]="adjustmentsCurrentPage() >= adjustmentsTotalPages()"
                (clicked)="goToAdjustmentsPage(adjustmentsCurrentPage() + 1)"
              >
                التالي
              </app-button>
            </div>
          </div>
        }
      </app-card>

      <app-card title="سجل حركة الذهب">
        <form class="mb-4 space-y-4" novalidate (submit)="applyLedgerFilters(); $event.preventDefault()">
          <div class="grid grid-cols-1 gap-4 md:grid-cols-4">
            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">العيار</span>
              <select
                [value]="ledgerKaratFilter()"
                (change)="ledgerKaratFilter.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">كل العيارات</option>
                @for (karat of karatOptions; track karat.value) {
                  <option [value]="karat.value">{{ karat.label }}</option>
                }
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">نوع المرجع</span>
              <select
                [value]="ledgerReferenceTypeFilter()"
                (change)="ledgerReferenceTypeFilter.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">كل الأنواع</option>
                @for (reference of referenceTypes; track reference.value) {
                  <option [value]="reference.value">{{ reference.label }}</option>
                }
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">من تاريخ</span>
              <input
                type="date"
                [value]="ledgerFromDate()"
                (change)="ledgerFromDate.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">إلى تاريخ</span>
              <input
                type="date"
                [value]="ledgerToDate()"
                (change)="ledgerToDate.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>
          </div>

          <div class="flex flex-wrap gap-3">
            <app-button type="submit" icon="filter">تصفية</app-button>
            <app-button variant="secondary" type="button" (clicked)="resetLedgerFilters()">
              إعادة تعيين
            </app-button>
          </div>
        </form>

        <div class="overflow-x-auto">
          <table class="w-full border-collapse text-sm">
            <thead>
              <tr class="border-b border-gray-200">
                <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ والوقت</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الحركة</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">العيار</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الوزن (غ)</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">معادل 21ك</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المرجع</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">ملاحظات</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المستخدم</th>
              </tr>
            </thead>
            <tbody>
              @if (ledgerLoading()) {
                @for (row of skeletonRows; track $index) {
                  <tr class="border-b border-gray-100">
                    @for (column of skeletonTableColumns; track column) {
                      <td class="px-4 py-3"><app-skeleton height="1rem" /></td>
                    }
                  </tr>
                }
              } @else if (ledgerError(); as error) {
                <tr>
                  <td colspan="8" class="px-4 py-10">
                    <app-empty-state
                      icon="alert-circle"
                      title="تعذّر تحميل سجل الذهب"
                      [description]="error.detail ?? ''"
                    >
                      <app-retry-button (retry)="reloadLedger()" />
                    </app-empty-state>
                  </td>
                </tr>
              } @else if (ledgerRows().length === 0) {
                <tr>
                  <td colspan="8" class="px-4 py-10">
                    <app-empty-state
                      icon="inbox"
                      title="لا توجد حركات في سجل الذهب"
                      description="جرّب تعديل عوامل التصفية."
                    />
                  </td>
                </tr>
              } @else {
                @for (entry of ledgerRows(); track entry.id) {
                  <tr class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15">
                    <td class="px-4 py-3 data-mono">{{ entry.dateText }}</td>
                    <td class="px-4 py-3">
                      @if (entry.movementType === 'Increase') {
                        <app-badge variant="success">{{ entry.movementLabel }}</app-badge>
                      } @else {
                        <app-badge variant="error">{{ entry.movementLabel }}</app-badge>
                      }
                    </td>
                    <td class="px-4 py-3">{{ entry.karatLabel }}</td>
                    <td class="px-4 py-3 data-mono">{{ entry.weightText }}</td>
                    <td class="px-4 py-3 data-mono">{{ entry.equivalentText }}</td>
                    <td class="px-4 py-3">{{ entry.referenceLabel }}</td>
                    <td class="max-w-48 truncate px-4 py-3 text-gray-600" title="{{ entry.notes ?? '' }}">
                      {{ entry.notes ?? '—' }}
                    </td>
                    <td class="px-4 py-3 text-gray-600">{{ entry.userName }}</td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        @if (ledgerTotalPages() > 1) {
          <div class="mt-4 flex items-center justify-between border-t border-gray-100 pt-4">
            <span class="text-xs text-gray-500 data-mono">
              {{ ledgerTotalCount() }} حركة — صفحة {{ ledgerCurrentPage() }} من {{ ledgerTotalPages() }}
            </span>
            <div class="flex gap-2">
              <app-button
                variant="secondary"
                size="sm"
                icon="chevron-right"
                [disabled]="ledgerCurrentPage() <= 1"
                (clicked)="goToLedgerPage(ledgerCurrentPage() - 1)"
              >
                السابق
              </app-button>
              <app-button
                variant="secondary"
                size="sm"
                icon="chevron-left"
                [disabled]="ledgerCurrentPage() >= ledgerTotalPages()"
                (clicked)="goToLedgerPage(ledgerCurrentPage() + 1)"
              >
                التالي
              </app-button>
            </div>
          </div>
        }
      </app-card>

      <app-adjustment-dialog
        [open]="dialogOpen()"
        (openChange)="dialogOpen.set(false)"
        (saved)="onAdjustmentSaved()"
      />
    </main>
  `,
})
export class InventoryPage {
  private readonly auth = inject(AuthStore);
  readonly canManageInventory = computed(() => this.auth.hasPermission('inventory.manage') || this.auth.hasRole('store_admin'));
  readonly store = inject(InventoryStore);

  readonly inventoryKpis = this.store.inventoryKpis;
  readonly inventoryKpisLoading = this.store.inventoryKpisLoading;
  readonly adjustmentKpis = this.store.adjustmentKpis;
  readonly adjustmentKpisLoading = this.store.adjustmentKpisLoading;
  readonly adjustmentsLoading = this.store.adjustmentsLoading;
  readonly adjustmentsError = this.store.adjustmentsError;
  readonly ledgerLoading = this.store.ledgerLoading;
  readonly ledgerError = this.store.ledgerError;
  readonly trend = this.store.trend;
  readonly trendLoading = this.store.trendLoading;
  readonly trendError = this.store.trendError;

  readonly dialogOpen = signal(false);

  readonly trendDays = signal(7);

  readonly adjustmentTypeFilter = signal('');
  readonly adjustmentFromDate = signal('');
  readonly adjustmentToDate = signal('');

  readonly ledgerKaratFilter = signal('');
  readonly ledgerReferenceTypeFilter = signal('');
  readonly ledgerFromDate = signal('');
  readonly ledgerToDate = signal('');

  private readonly adjustmentsQuery = signal<AdjustmentsQuery>({ page: 1, pageSize: PAGE_SIZE });
  private readonly ledgerQuery = signal<GoldLedgerQuery>({ page: 1, pageSize: PAGE_SIZE });

  readonly adjustmentTypeOptions = [
    { value: '1', label: 'زيادة' },
    { value: '2', label: 'نقصان' },
    { value: '3', label: 'تلف' },
    { value: '4', label: 'خسارة' },
    { value: '5', label: 'يدوي تصحيح' },
  ];

  readonly karatOptions = [
    { value: '24', label: 'عيار 24' },
    { value: '21', label: 'عيار 21' },
    { value: '18', label: 'عيار 18' },
  ];

  readonly referenceTypes = REFERENCE_TYPES;

  readonly adjustmentRows = computed(() =>
    (this.store.adjustments()?.items ?? []).map(toAdjustmentRow),
  );
  readonly adjustmentsTotalCount = computed(() => Number(this.store.adjustments()?.totalCount ?? 0));
  readonly adjustmentsCurrentPage = computed(() => Number(this.store.adjustments()?.pageNumber ?? 1));
  readonly adjustmentsTotalPages = computed(() => Number(this.store.adjustments()?.totalPages ?? 0));

  readonly ledgerRows = computed(() => (this.store.ledger()?.items ?? []).map(toLedgerRow));
  readonly ledgerTotalCount = computed(() => Number(this.store.ledger()?.totalCount ?? 0));
  readonly ledgerCurrentPage = computed(() => Number(this.store.ledger()?.pageNumber ?? 1));
  readonly ledgerTotalPages = computed(() => Number(this.store.ledger()?.totalPages ?? 0));

  readonly chartPoints = computed(() => {
    const points = this.trend() ?? [];
    const max = points.reduce(
      (highest, point) => Math.max(highest, Number(point.in21K ?? 0), Number(point.out21K ?? 0)),
      0,
    );
    const scale = max > 0 ? max : 1;
    return points.map((point) => {
      const inGrams = Number(point.in21K ?? 0);
      const outGrams = Number(point.out21K ?? 0);
      return {
        date: point.date ?? '',
        label:
          this.trendDays() <= 7
            ? (point.label ?? '')
            : formatDate(point.date ?? '', 'date'),
        inText: formatWeight(inGrams),
        outText: formatWeight(outGrams),
        inPercent: (inGrams / scale) * 100,
        outPercent: (outGrams / scale) * 100,
      };
    });
  });

  readonly trendNetText = computed(() => {
    const net = (this.trend() ?? []).reduce((sum, point) => sum + Number(point.net21K ?? 0), 0);
    return formatWeight(net);
  });

  readonly skeletonRows = [0, 1, 2, 3, 4];
  readonly skeletonTableColumns = SKELETON_TABLE_COLUMNS;

  readonly Number = Number;

  constructor() {
    void this.store.loadInventoryKpis();
    void this.store.loadAdjustmentKpis();
    void this.store.loadAdjustments(this.adjustmentsQuery());
    void this.store.loadLedger(this.ledgerQuery());
    void this.store.loadTrend(this.trendDays());
  }

  setTrendDays(value: string): void {
    const days = Number(value);
    this.trendDays.set(days);
    void this.store.loadTrend(days);
  }

  reloadTrend(): void {
    void this.store.loadTrend(this.trendDays());
  }

  applyAdjustmentFilters(): void {
    this.adjustmentsQuery.set({
      page: 1,
      pageSize: PAGE_SIZE,
      adjustmentType: this.adjustmentTypeFilter() || undefined,
      fromDate: this.adjustmentFromDate() || undefined,
      toDate: this.adjustmentToDate() || undefined,
    });
    void this.store.loadAdjustments(this.adjustmentsQuery());
  }

  resetAdjustmentFilters(): void {
    this.adjustmentTypeFilter.set('');
    this.adjustmentFromDate.set('');
    this.adjustmentToDate.set('');
    this.applyAdjustmentFilters();
  }

  goToAdjustmentsPage(page: number): void {
    this.adjustmentsQuery.update((current) => ({ ...current, page }));
    void this.store.loadAdjustments(this.adjustmentsQuery());
  }

  reloadAdjustments(): void {
    void this.store.loadAdjustments(this.adjustmentsQuery());
  }

  applyLedgerFilters(): void {
    this.ledgerQuery.set({
      page: 1,
      pageSize: PAGE_SIZE,
      karat: this.ledgerKaratFilter() === '' ? undefined : Number(this.ledgerKaratFilter()),
      referenceType: this.ledgerReferenceTypeFilter() || undefined,
      fromDate: this.ledgerFromDate() || undefined,
      toDate: this.ledgerToDate() || undefined,
    });
    void this.store.loadLedger(this.ledgerQuery());
  }

  resetLedgerFilters(): void {
    this.ledgerKaratFilter.set('');
    this.ledgerReferenceTypeFilter.set('');
    this.ledgerFromDate.set('');
    this.ledgerToDate.set('');
    this.applyLedgerFilters();
  }

  goToLedgerPage(page: number): void {
    this.ledgerQuery.update((current) => ({ ...current, page }));
    void this.store.loadLedger(this.ledgerQuery());
  }

  reloadLedger(): void {
    void this.store.loadLedger(this.ledgerQuery());
  }

  onAdjustmentSaved(): void {
    this.dialogOpen.set(false);
    void this.store.loadAdjustments(this.adjustmentsQuery());
    void this.store.loadAdjustmentKpis();
    void this.store.loadInventoryKpis();
    void this.store.loadTrend(this.trendDays());
  }
}