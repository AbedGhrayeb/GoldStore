import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth-store';

import {
  Button,
  Card,
  EmptyState,
  KpiCard,
  RetryButton,
  resolveIcon,
  Skeleton,
} from '../../shared/ui';
import { formatDateTime } from '../../shared/format/formatters';
import type {
  CustomerPurchaseInvoiceQuery,
  CustomerPurchaseInvoiceResponse,
} from './purchases-api.service';
import { PurchaseDetailDialog } from './purchase-detail-dialog';
import { PurchaseDialog } from './purchase-dialog';
import { PurchasesStore } from './purchases-store';

const PAGE_SIZE = 15;

/** Display row for the purchases table (formatted date + currency texts). */
export interface PurchaseTableRow extends CustomerPurchaseInvoiceResponse {
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

function toPurchaseRow(invoice: CustomerPurchaseInvoiceResponse): PurchaseTableRow {
  return {
    ...invoice,
    dateText: formatDateTime(invoice.date ?? ''),
    totalText: amountText(invoice.totalAmount, invoice.currency),
    paidText: amountText(invoice.amountPaid, invoice.currency),
    remainingText: amountText(invoice.remainingBalance, invoice.currency),
  };
}

/**
 * P3.9 — Customer gold purchases (`feature: purchases`). Today's KPIs + purchase totals
 * (server-computed Display strings), a paged purchases table (search by invoice number /
 * seller name, date filters, pagination), a create-purchase builder ({@link PurchaseDialog})
 * and a per-invoice detail view ({@link PurchaseDetailDialog}). The client never computes or
 * sends the 21K-equivalent — the server owns that formula.
 */
@Component({
  selector: 'app-purchases-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Button,
    Card,
    EmptyState,
    KpiCard,
    LucideAngularModule,
    PurchaseDetailDialog,
    PurchaseDialog,
    RetryButton,
    Skeleton,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">شراء ذهب من العملاء</h1>
          <p class="mt-1 text-sm text-gray-600">مشتريات الذهب — فواتير شراء من البائعين.</p>
        </div>
        <app-button
          icon="shopping-bag"
          [disabled]="!canManagePurchases()"
          [title]="!canManagePurchases() ? 'ليس لديك صلاحية إنشاء فاتورة شراء' : ''"
          (clicked)="canManagePurchases() && purchaseDialogOpen.set(true)"
        >
          فاتورة شراء جديدة
        </app-button>
      </div>

      @if (kpisLoading() && kpis() === null) {
        <section class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
          @for (column of skeletonCards; track column) {
            <app-skeleton height="7rem" />
          }
        </section>
      } @else if (kpis(); as kpis) {
        <section class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
          <app-kpi-card
            title="مشتريات اليوم"
            [value]="kpis.todayCount ?? '0'"
            icon="shopping-bag"
          />
          <app-kpi-card
            title="إجمالي المشتريات"
            [value]="kpis.totalPurchasesDisplay ?? '0.000'"
            icon="trending-up"
          />
          <app-kpi-card
            title="الإجمالي المدفوع"
            [value]="kpis.totalPaidDisplay ?? '0.000'"
            icon="wallet"
          />
          <app-kpi-card
            title="الرصيد المتبقي (دين على المتجر)"
            [value]="kpis.totalRemainingDisplay ?? '0.000'"
            icon="coins"
          />
        </section>
      }

      <app-card title="فواتير الشراء">
        <form class="mb-4 space-y-4" novalidate (submit)="applyFilters(); $event.preventDefault()">
          <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-5">
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
                  placeholder="رقم الفاتورة أو اسم البائع"
                  [value]="searchFilter()"
                  (input)="onSearchInput($any($event.target).value)"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 pe-10 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                />
              </div>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">الصنف</span>
              <select
                [value]="categoryFilter()"
                (change)="onCategoryChange($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">كل الأصناف</option>
                @for (category of activeCategories(); track category.id) {
                  <option [value]="category.id">{{ category.name }}</option>
                }
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">من تاريخ</span>
              <input
                type="date"
                [value]="fromDateFilter()"
                (change)="onDateChange('from', $any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">إلى تاريخ</span>
              <input
                type="date"
                [value]="toDateFilter()"
                (change)="onDateChange('to', $any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">عدد الصفوف</span>
              <select
                [value]="pageSizeFilter()"
                (change)="setPageSize($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option [value]="15">15</option>
                <option [value]="25">25</option>
                <option [value]="50">50</option>
              </select>
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
                <th class="px-4 py-3 text-start font-semibold text-gray-600">البائع</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الموظف</th>
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
                      title="لا توجد فواتير شراء"
                      description="أصدر أول فاتورة عبر زر «فاتورة شراء جديدة»."
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
                    <td class="px-4 py-3 text-gray-900">{{ invoice.sellerName ?? '—' }}</td>
                    <td class="px-4 py-3 data-mono">{{ invoice.dateText }}</td>
                    <td class="px-4 py-3 text-gray-900">{{ invoice.employeeName ?? '—' }}</td>
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

      <app-purchase-dialog
        [open]="purchaseDialogOpen()"
        (openChange)="purchaseDialogOpen.set(false)"
        (saved)="onPurchaseSaved()"
      />

      <app-purchase-detail-dialog
        [open]="detailOpen()"
        [invoice]="detailInvoice()"
        (openChange)="closeDetail()"
      />
    </main>
  `,
})
export class PurchasesPage {
  private readonly auth = inject(AuthStore);
  readonly canManagePurchases = computed(
    () => this.auth.hasPermission('purchases.manage') || this.auth.hasRole('store_admin'),
  );
  readonly store = inject(PurchasesStore);

  readonly kpis = this.store.kpis;
  readonly kpisLoading = this.store.kpisLoading;
  readonly loading = this.store.loading;
  readonly error = this.store.error;

  readonly purchaseDialogOpen = signal(false);
  readonly detailOpen = signal(false);
  readonly detailInvoice = signal<CustomerPurchaseInvoiceResponse | null>(null);

  readonly searchFilter = signal('');
  readonly categoryFilter = signal('');
  readonly fromDateFilter = signal('');
  readonly toDateFilter = signal('');
  readonly pageSizeFilter = signal(PAGE_SIZE);

  private readonly query = signal<CustomerPurchaseInvoiceQuery>({ page: 1, pageSize: PAGE_SIZE });

  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  readonly activeCategories = computed(() =>
    (this.store.categories() ?? []).filter((category) => category.isActive !== false),
  );

  readonly rows = computed(() => (this.store.page()?.items ?? []).map(toPurchaseRow));
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

  applyFilters(): void {
    this.query.set({
      page: 1,
      pageSize: this.pageSizeFilter(),
      search: this.searchFilter().trim() || undefined,
      categoryId: this.categoryFilter() || undefined,
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

  onCategoryChange(value: string): void {
    this.categoryFilter.set(value);
    this.applyFilters();
  }

  onDateChange(field: 'from' | 'to', value: string): void {
    if (field === 'from') {
      this.fromDateFilter.set(value);
    } else {
      this.toDateFilter.set(value);
    }
    this.applyFilters();
  }

  resetFilters(): void {
    this.searchFilter.set('');
    this.categoryFilter.set('');
    this.fromDateFilter.set('');
    this.toDateFilter.set('');
    this.pageSizeFilter.set(PAGE_SIZE);
    this.applyFilters();
  }

  setPageSize(value: string): void {
    this.pageSizeFilter.set(Number(value) || PAGE_SIZE);
    this.applyFilters();
  }

  goToPage(page: number): void {
    this.query.update((current) => ({ ...current, page }));
    void this.store.loadInvoices(this.query());
  }

  reload(): void {
    void this.store.loadInvoices(this.query());
  }

  openDetail(invoice: CustomerPurchaseInvoiceResponse): void {
    this.detailInvoice.set(invoice);
    this.detailOpen.set(true);
  }

  closeDetail(): void {
    this.detailOpen.set(false);
    this.store.clearDetail();
  }

  onPurchaseSaved(): void {
    this.purchaseDialogOpen.set(false);
    void this.store.loadInvoices(this.query());
    void this.store.loadKpis();
  }
}
