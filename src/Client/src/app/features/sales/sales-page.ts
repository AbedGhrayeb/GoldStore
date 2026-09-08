import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth-store';

import {
  Badge,
  Button,
  Card,
  EmptyState,
  KpiCard,
  RetryButton,
  resolveIcon,
  Skeleton,
} from '../../shared/ui';
import { formatDateTime } from '../../shared/format/formatters';
import type { SalesInvoiceQuery, SalesInvoiceResponse } from './sales-api.service';
import { InvoiceDetailDialog } from './invoice-detail-dialog';
import { InvoiceDialog } from './invoice-dialog';
import { SalesStore } from './sales-store';

const PAGE_SIZE = 15;

export const SALES_STATUS_OPTIONS: readonly { value: string; label: string }[] = [
  { value: 'Draft', label: 'مسودة' },
  { value: 'Completed', label: 'مكتملة' },
  { value: 'PartiallyPaid', label: 'مدفوعة جزئياً' },
  { value: 'Cancelled', label: 'ملغاة' },
];

const STATUS_VARIANT: Readonly<Record<string, 'success' | 'warning' | 'neutral' | 'error'>> = {
  Completed: 'success',
  PartiallyPaid: 'warning',
  Draft: 'neutral',
  Cancelled: 'error',
};

/** Display row for the invoices table (formatted date + currency texts). */
export interface SalesTableRow extends SalesInvoiceResponse {
  dateText: string;
  totalText: string;
  paidText: string;
  remainingText: string;
}

function amountText(
  value: number | string | null | undefined,
  currency: string | null | undefined,
): string {
  const amount = Number(value ?? 0);
  const text = Number.isFinite(amount) ? amount.toFixed(3) : '0.000';
  return currency ? `${text} ${currency}` : text;
}

function toSalesRow(invoice: SalesInvoiceResponse): SalesTableRow {
  return {
    ...invoice,
    dateText: formatDateTime(invoice.date ?? ''),
    totalText: amountText(invoice.totalAmount, invoice.currency),
    paidText: amountText(invoice.amountPaid, invoice.currency),
    remainingText: amountText(invoice.remainingBalance, invoice.currency),
  };
}

/**
 * P3.8 — Sales (`feature: sales`). Today's KPIs + invoice totals (server-computed Display
 * strings, no unit — the totals are raw sums across mixed currencies), a paged invoices
 * table (search by number/customer, status + date filters, pagination), a create-invoice
 * builder ({@link InvoiceDialog}) and a per-invoice detail view ({@link InvoiceDetailDialog}).
 * The client never computes or sends the 21K-equivalent — the server owns that formula.
 */
@Component({
  selector: 'app-sales-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Badge,
    Button,
    Card,
    EmptyState,
    InvoiceDetailDialog,
    InvoiceDialog,
    KpiCard,
    LucideAngularModule,
    RetryButton,
    Skeleton,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">المبيعات</h1>
          <p class="mt-1 text-sm text-gray-600">إصدار فواتير المبيعات ومتابعة التحصيل والدين.</p>
        </div>
        <app-button
          icon="receipt"
          [disabled]="!canCreateSales()"
          [title]="!canCreateSales() ? 'ليس لديك صلاحية إنشاء فاتورة مبيعات' : ''"
          (clicked)="canCreateSales() && invoiceDialogOpen.set(true)"
          >فاتورة جديدة</app-button
        >
      </div>

      @if (kpisLoading() && kpis() === null) {
        <section class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
          @for (column of skeletonCards; track column) {
            <app-skeleton height="7rem" />
          }
        </section>
      } @else if (kpis(); as kpis) {
        <section class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
          <app-kpi-card title="مبيعات اليوم" [value]="kpis.todayCount ?? '0'" icon="receipt" />
          <app-kpi-card
            title="إجمالي المبيعات"
            [value]="kpis.totalSalesDisplay ?? '0.000'"
            icon="trending-up"
          />
          <app-kpi-card
            title="الإجمالي المدفوع"
            [value]="kpis.totalPaidDisplay ?? '0.000'"
            icon="wallet"
          />
          <app-kpi-card
            title="الرصيد المتبقي (دين)"
            [value]="kpis.totalRemainingDisplay ?? '0.000'"
            icon="coins"
          />
        </section>
      }

      <app-card title="فواتير المبيعات">
        <form class="mb-4 space-y-4" novalidate (submit)="applyFilters(); $event.preventDefault()">
          <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">بحث</span>
              <div class="relative">
                <span
                  class="pointer-events-none absolute inset-y-0 end-3 flex items-center text-gray-400"
                >
                  <lucide-icon [img]="searchIcon" [size]="16" />
                </span>
                <input
                  type="text"
                  autocomplete="off"
                  placeholder="رقم الفاتورة أو اسم العميل"
                  [value]="searchFilter()"
                  (input)="onSearchInput($any($event.target).value)"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 pe-10 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                />
              </div>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">الحالة</span>
              <select
                [value]="statusFilter()"
                (change)="onSelectChange('status', $any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">الكل</option>
                @for (option of statusOptions; track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">من تاريخ</span>
              <input
                type="date"
                [value]="fromDateFilter()"
                (change)="onSelectChange('from', $any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">إلى تاريخ</span>
              <input
                type="date"
                [value]="toDateFilter()"
                (change)="onSelectChange('to', $any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>
          </div>

          <div class="flex flex-wrap gap-3">
            <app-button type="submit" icon="filter">تصفية</app-button>
            <app-button variant="secondary" type="button" (clicked)="resetFilters()">
              إعادة تعيين
            </app-button>
          </div>
        </form>

        <div class="overflow-x-auto">
          <table class="w-full border-collapse text-sm">
            <thead>
              <tr class="border-b border-gray-200">
                <th class="px-4 py-3 text-start font-semibold text-gray-600">رقم الفاتورة</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">العميل</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الحالة</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الإجمالي</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المدفوع</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المتبقي</th>
                <th class="px-4 py-3 text-center font-semibold text-gray-600"></th>
              </tr>
            </thead>
            <tbody>
              @if (loading()) {
                @for (row of skeletonRows; track $index) {
                  <tr class="border-b border-gray-100">
                    @for (column of skeletonTableColumns; track column) {
                      <td class="px-4 py-3"><app-skeleton height="1rem" /></td>
                    }
                  </tr>
                }
              } @else if (error(); as error) {
                <tr>
                  <td colspan="8" class="px-4 py-10">
                    <app-empty-state
                      icon="alert-circle"
                      title="تعذّر تحميل الفواتير"
                      [description]="error.detail ?? ''"
                    >
                      <app-retry-button (retry)="reload()" />
                    </app-empty-state>
                  </td>
                </tr>
              } @else if (rows().length === 0) {
                <tr>
                  <td colspan="8" class="px-4 py-10">
                    <app-empty-state
                      icon="inbox"
                      title="لا توجد فواتير مبيعات"
                      description="أصدر أول فاتورة عبر زر «فاتورة جديدة»."
                    />
                  </td>
                </tr>
              } @else {
                @for (invoice of rows(); track invoice.id) {
                  <tr
                    class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15"
                  >
                    <td class="px-4 py-3">
                      <button
                        type="button"
                        class="font-medium text-gold hover:underline"
                        (click)="openDetail(invoice)"
                      >
                        {{ invoice.invoiceNumber ?? '—' }}
                      </button>
                    </td>
                    <td class="px-4 py-3 text-gray-900">{{ invoice.customerName ?? '—' }}</td>
                    <td class="px-4 py-3 data-mono">{{ invoice.dateText }}</td>
                    <td class="px-4 py-3">
                      <app-badge [variant]="statusVariant(invoice.status)">
                        {{ invoice.statusLabel ?? invoice.status ?? '—' }}
                      </app-badge>
                    </td>
                    <td class="px-4 py-3 font-semibold text-gray-900 data-mono" dir="ltr">
                      {{ invoice.totalText }}
                    </td>
                    <td class="px-4 py-3 data-mono" dir="ltr">{{ invoice.paidText }}</td>
                    <td class="px-4 py-3 data-mono" dir="ltr">{{ invoice.remainingText }}</td>
                    <td class="px-4 py-3 text-center">
                      <button
                        type="button"
                        class="rounded-md p-1.5 text-gray-400 transition-colors hover:bg-gold-container/40 hover:text-gold"
                        aria-label="عرض التفاصيل"
                        (click)="openDetail(invoice)"
                      >
                        <lucide-icon [img]="eyeIcon" [size]="16" />
                      </button>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        @if (totalPages() > 1) {
          <div class="mt-4 flex items-center justify-between border-t border-gray-100 pt-4">
            <span class="text-xs text-gray-500 data-mono">
              {{ totalCount() }} فاتورة — صفحة {{ currentPage() }} من {{ totalPages() }}
            </span>
            <div class="flex gap-2">
              <app-button
                variant="secondary"
                size="sm"
                icon="chevron-right"
                [disabled]="currentPage() <= 1"
                (clicked)="goToPage(currentPage() - 1)"
              >
                السابق
              </app-button>
              <app-button
                variant="secondary"
                size="sm"
                icon="chevron-left"
                [disabled]="currentPage() >= totalPages()"
                (clicked)="goToPage(currentPage() + 1)"
              >
                التالي
              </app-button>
            </div>
          </div>
        }
      </app-card>

      <app-invoice-dialog
        [open]="invoiceDialogOpen()"
        (openChange)="invoiceDialogOpen.set(false)"
        (saved)="onInvoiceSaved()"
      />

      <app-invoice-detail-dialog
        [open]="detailOpen()"
        [invoice]="detailInvoice()"
        (openChange)="closeDetail()"
      />
    </main>
  `,
})
export class SalesPage {
  private readonly auth = inject(AuthStore);
  readonly canCreateSales = computed(
    () => this.auth.hasPermission('sales.manage') || this.auth.hasRole('store_admin'),
  );
  readonly canViewSales = computed(
    () => this.auth.hasPermission('sales.view') || this.auth.hasRole('store_admin'),
  );
  readonly store = inject(SalesStore);

  readonly kpis = this.store.kpis;
  readonly kpisLoading = this.store.kpisLoading;
  readonly loading = this.store.loading;
  readonly error = this.store.error;

  readonly invoiceDialogOpen = signal(false);
  readonly detailOpen = signal(false);
  readonly detailInvoice = signal<SalesInvoiceResponse | null>(null);

  readonly searchFilter = signal('');
  readonly statusFilter = signal('');
  readonly fromDateFilter = signal('');
  readonly toDateFilter = signal('');

  private readonly query = signal<SalesInvoiceQuery>({ page: 1, pageSize: PAGE_SIZE });

  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  readonly statusOptions = SALES_STATUS_OPTIONS;

  readonly rows = computed(() => (this.store.page()?.items ?? []).map(toSalesRow));
  readonly totalCount = computed(() => Number(this.store.page()?.totalCount ?? 0));
  readonly currentPage = computed(() => Number(this.store.page()?.pageNumber ?? 1));
  readonly totalPages = computed(() => Number(this.store.page()?.totalPages ?? 0));

  readonly skeletonRows = [0, 1, 2, 3, 4];
  readonly skeletonCards = [0, 1, 2, 3];
  readonly skeletonTableColumns = [0, 1, 2, 3, 4, 5, 6, 7];

  readonly eyeIcon = resolveIcon('eye');
  readonly searchIcon = resolveIcon('search');

  constructor() {
    void this.store.loadKpis();
    void this.store.loadInvoices(this.query());
    void this.store.refreshOptions();
  }

  statusVariant(status: string | undefined): 'success' | 'warning' | 'neutral' | 'error' {
    return STATUS_VARIANT[status ?? ''] ?? 'neutral';
  }

  applyFilters(): void {
    this.query.set({
      page: 1,
      pageSize: PAGE_SIZE,
      search: this.searchFilter().trim() || undefined,
      status: this.statusFilter() || undefined,
      fromDate: this.fromDateFilter() || undefined,
      toDate: this.toDateFilter() || undefined,
    });
    void this.store.loadInvoices(this.query());
  }

  onSearchInput(value: string): void {
    this.searchFilter.set(value);
    if (this.searchDebounce !== null) {
      clearTimeout(this.searchDebounce);
    }
    this.searchDebounce = setTimeout(() => this.applyFilters(), 400);
  }

  onSelectChange(field: 'status' | 'from' | 'to', value: string): void {
    if (field === 'status') {
      this.statusFilter.set(value);
    } else if (field === 'from') {
      this.fromDateFilter.set(value);
    } else {
      this.toDateFilter.set(value);
    }
    this.applyFilters();
  }

  resetFilters(): void {
    this.searchFilter.set('');
    this.statusFilter.set('');
    this.fromDateFilter.set('');
    this.toDateFilter.set('');
    this.applyFilters();
  }

  goToPage(page: number): void {
    this.query.update((current) => ({ ...current, page }));
    void this.store.loadInvoices(this.query());
  }

  reload(): void {
    void this.store.loadInvoices(this.query());
  }

  openDetail(invoice: SalesInvoiceResponse): void {
    this.detailInvoice.set(invoice);
    this.detailOpen.set(true);
  }

  closeDetail(): void {
    this.detailOpen.set(false);
    this.store.clearDetail();
  }

  onInvoiceSaved(): void {
    this.invoiceDialogOpen.set(false);
    void this.store.loadInvoices(this.query());
    void this.store.loadKpis();
  }
}
