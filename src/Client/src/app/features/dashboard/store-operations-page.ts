import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import {
  Badge,
  Button,
  Card,
  CurrencyTotals,
  EmptyState,
  RetryButton,
  Skeleton,
  Table,
  resolveIcon,
  type TableColumn,
} from '../../shared/ui';
import { formatWeight } from '../../shared/format/formatters';
import { OperationDetailDialog } from './operation-detail-dialog';
import type { StoreOperationsQuery } from './store-operations-api.service';
import {
  toCategoryKpiRow,
  toEmployeeStatRow,
  toOperationRow,
  type CategoryKpiRow,
  type EmployeeStatRow,
  type OperationRow,
} from './store-operations.model';
import { StoreOperationsStore } from './store-operations-store';

const PAGE_SIZE = 20;
const SKELETON_TABLE_COLUMNS = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

/**
 * P3.2 — Dashboard: store operations. Auth-only (no feature gate). Aggregates sales and
 * purchases: today's KPIs, a filterable paged operations table, per-employee day stats and a
 * detail drawer. Backed by {@link StoreOperationsStore}; this component is presentation-only.
 */
@Component({
  selector: 'app-store-operations-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Badge,
    Button,
    Card,
    CurrencyTotals,
    EmptyState,
    LucideAngularModule,
    OperationDetailDialog,
    RetryButton,
    Skeleton,
    Table,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">عمليات المتجر</h1>
          <p class="mt-1 text-sm text-gray-600">سجل عمليات البيع والشراء في المتجر.</p>
        </div>
        <app-button variant="secondary" icon="refresh-cw" (clicked)="refresh()">تحديث</app-button>
      </div>

      <section class="grid grid-cols-1 gap-4 md:grid-cols-2">
        <app-card>
          <div class="flex items-center justify-between">
            <p class="text-xs font-medium text-gray-500">مبيعات اليوم</p>
            <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50">
              <lucide-icon [img]="receiptIcon" [size]="18" class="text-gray-700" />
            </span>
          </div>
          @if (kpisLoading() && kpis() === null) {
            <app-skeleton class="mt-3" width="6rem" height="1.75rem" />
          } @else {
            <p class="mt-3 text-2xl font-semibold text-gray-900" data-mono>
              {{ salesCount() }}
              <span class="ms-1 text-sm font-normal text-gray-500">فاتورة</span>
            </p>
            <app-currency-totals class="mt-2 block" [totals]="kpis()?.todaySalesTotals ?? []" />
          }
        </app-card>

        <app-card>
          <div class="flex items-center justify-between">
            <p class="text-xs font-medium text-gray-500">مشتريات اليوم</p>
            <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50">
              <lucide-icon [img]="shoppingBagIcon" [size]="18" class="text-gray-700" />
            </span>
          </div>
          @if (kpisLoading() && kpis() === null) {
            <app-skeleton class="mt-3" width="6rem" height="1.75rem" />
          } @else {
            <p class="mt-3 text-2xl font-semibold text-gray-900" data-mono>
              {{ purchasesCount() }}
              <span class="ms-1 text-sm font-normal text-gray-500">فاتورة</span>
            </p>
            <app-currency-totals class="mt-2 block" [totals]="kpis()?.todayPurchasesTotals ?? []" />
          }
        </app-card>
      </section>

      <app-card title="مبيعات اليوم حسب التصنيف">
        <app-table
          [columns]="categoryColumns"
          [rows]="salesByCategoryRows()"
          [loading]="kpisLoading()"
          emptyTitle="لا توجد مبيعات اليوم"
          emptyDescription="لم تُسجَّل مبيعات اليوم."
        />
      </app-card>

      <app-card title="مشتريات اليوم حسب التصنيف">
        <app-table
          [columns]="categoryColumns"
          [rows]="purchasesByCategoryRows()"
          [loading]="kpisLoading()"
          emptyTitle="لا توجد مشتريات اليوم"
          emptyDescription="لم تُسجَّل مشتريات اليوم."
        />
      </app-card>

      <app-card title="إحصائيات الموظفين اليوم">
        <app-table
          [columns]="statColumns"
          [rows]="statRows()"
          [loading]="statsLoading()"
          emptyTitle="لا توجد إحصائيات لليوم"
          emptyDescription="لم تُسجَّل عمليات باسم أي موظف اليوم."
        />
      </app-card>

      <app-card title="تصفية العمليات">
        <form class="space-y-4" novalidate (submit)="applyFilters(); $event.preventDefault()">
          <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">بحث</span>
              <input
                type="text"
                [value]="search()"
                (input)="search.set($any($event.target).value)"
                (keydown)="onSearchKeydown($event)"
                placeholder="رقم الفاتورة أو اسم الطرف..."
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">من تاريخ</span>
              <input
                type="date"
                [value]="fromDate()"
                (input)="fromDate.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">إلى تاريخ</span>
              <input
                type="date"
                [value]="toDate()"
                (input)="toDate.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">نوع العملية</span>
              <select
                [value]="operationType()"
                (change)="operationType.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">الكل</option>
                <option value="Sale">بيع</option>
                <option value="Buy">شراء</option>
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">الموظف</span>
              <select
                [value]="employeeId()"
                (change)="employeeId.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">الكل</option>
                @for (employee of employees(); track employee.id) {
                  <option [value]="employee.id">{{ employee.name }}</option>
                }
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">الحساب</span>
              <select
                [value]="accountId()"
                (change)="accountId.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">الكل</option>
                @for (account of accountOptions(); track account.id) {
                  <option [value]="account.id">{{ account.name }}</option>
                }
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
      </app-card>

      <app-card>
        <div class="mb-4 flex items-center justify-between">
          <h2 class="text-sm font-semibold text-gray-700">سجل العمليات</h2>
          <span class="text-xs text-gray-500" data-mono>{{ totalCount() }} عملية</span>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full border-collapse text-sm">
            <thead>
              <tr class="border-b border-gray-200">
                <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">رقم الفاتورة</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">النوع</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الطرف الآخر</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الموظف</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الحساب</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الإجمالي</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المدفوع</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المتبقي</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">الحالة</th>
                <th class="px-4 py-3 text-center font-semibold text-gray-600">إجراءات</th>
              </tr>
            </thead>
            <tbody>
              @if (tableLoading()) {
                @for (row of skeletonRows; track $index) {
                  <tr class="border-b border-gray-100">
                    @for (column of skeletonTableColumns; track column) {
                      <td class="px-4 py-3"><app-skeleton height="1rem" /></td>
                    }
                  </tr>
                }
              } @else if (tableError(); as error) {
                <tr>
                  <td colspan="11" class="px-4 py-10">
                    <app-empty-state
                      icon="alert-circle"
                      title="تعذّر تحميل العمليات"
                      [description]="error.detail ?? ''"
                    >
                      <app-retry-button (retry)="refresh()" />
                    </app-empty-state>
                  </td>
                </tr>
              } @else if (rows().length === 0) {
                <tr>
                  <td colspan="11" class="px-4 py-10">
                    <app-empty-state
                      icon="inbox"
                      title="لا توجد عمليات"
                      description="جرّب تعديل عوامل التصفية أو النطاق الزمني."
                    />
                  </td>
                </tr>
              } @else {
                @for (operation of rows(); track operation.id) {
                  <tr
                    class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15"
                  >
                    <td class="px-4 py-3">{{ operation.dateText }}</td>
                    <td class="px-4 py-3 data-mono" dir="ltr">{{ operation.invoiceNumber }}</td>
                    <td class="px-4 py-3">
                      <app-badge [variant]="operation.typeVariant">
                        {{ operation.typeLabel }}
                      </app-badge>
                    </td>
                    <td class="px-4 py-3">{{ operation.counterpartyName }}</td>
                    <td class="px-4 py-3">{{ operation.employeeName }}</td>
                    <td class="px-4 py-3">{{ operation.accountName }}</td>
                    <td class="px-4 py-3 data-mono">{{ operation.totalText }}</td>
                    <td class="px-4 py-3 data-mono">{{ operation.paidText }}</td>
                    <td class="px-4 py-3 data-mono">{{ operation.balanceText }}</td>
                    <td class="px-4 py-3">
                      @if (operation.statusLabel; as statusLabel) {
                        <app-badge [variant]="operation.statusVariant">{{ statusLabel }}</app-badge>
                      } @else {
                        —
                      }
                    </td>
                    <td class="px-4 py-3 text-center">
                      <button
                        type="button"
                        class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800"
                        title="عرض التفاصيل"
                        [attr.aria-label]="'عرض تفاصيل ' + operation.invoiceNumber"
                        (click)="openDetail(operation)"
                      >
                        <lucide-icon [img]="eyeIcon" [size]="18" />
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
            <span class="text-xs text-gray-500" data-mono>
              {{ totalCount() }} عملية — صفحة {{ currentPage() }} من {{ totalPages() }}
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

      <app-operation-detail-dialog
        [open]="detailOpen()"
        [detail]="detail()"
        [loading]="detailLoading()"
        [error]="detailError()"
        (closed)="closeDetail()"
        (retry)="retryDetail()"
      />
    </main>
  `,
})
export class StoreOperationsPage {
  private readonly store = inject(StoreOperationsStore);

  readonly kpis = this.store.kpis;
  readonly kpisLoading = this.store.kpisLoading;
  readonly employees = this.store.employees;
  readonly statsLoading = this.store.statsLoading;
  readonly tableLoading = this.store.tableLoading;
  readonly tableError = this.store.tableError;
  readonly detail = this.store.detail;
  readonly detailLoading = this.store.detailLoading;
  readonly detailError = this.store.detailError;

  readonly search = signal('');
  readonly fromDate = signal('');
  readonly toDate = signal('');
  readonly operationType = signal('');
  readonly employeeId = signal('');
  readonly accountId = signal('');

  readonly detailOpen = signal(false);
  private readonly pendingDetail = signal<{ id: string; operationType: string } | null>(null);

  private readonly query = signal<StoreOperationsQuery>({ page: 1, pageSize: PAGE_SIZE });

  readonly rows = computed(() => (this.store.page()?.items ?? []).map(toOperationRow));
  readonly statRows = computed(() => this.store.todayStats().map(toEmployeeStatRow));

  readonly totalCount = computed(() => Number(this.store.page()?.totalCount ?? 0));
  readonly currentPage = computed(() => Number(this.store.page()?.page ?? 1));
  readonly totalPages = computed(() => Number(this.store.page()?.totalPages ?? 0));

  readonly salesCount = computed(() => Number(this.store.kpis()?.todaySalesCount ?? 0));
  readonly purchasesCount = computed(() => Number(this.store.kpis()?.todayPurchasesCount ?? 0));

  readonly salesByCategoryRows = computed<CategoryKpiRow[]>(() =>
    (this.store.kpis()?.salesByCategory ?? []).map(toCategoryKpiRow),
  );

  readonly purchasesByCategoryRows = computed<CategoryKpiRow[]>(() =>
    (this.store.kpis()?.purchasesByCategory ?? []).map(toCategoryKpiRow),
  );

  readonly categoryColumns: TableColumn<CategoryKpiRow>[] = [
    { key: 'name', header: 'التصنيف', cell: (row) => row.name, sortable: true },
    {
      key: 'weight',
      header: 'الوزن (غ)',
      cell: (row) => row.weightText,
      numeric: true,
      sortable: true,
      sortValue: (row) => row.weight,
    },
    {
      key: 'count',
      header: 'عدد البنود',
      cell: (row) => row.count,
      numeric: true,
      sortable: true,
      sortValue: (row) => row.count,
    },
    { key: 'totals', header: 'الإجمالي', cell: (row) => row.totals, numeric: true },
  ];

  /**
   * The dashboard group exposes no account-list endpoint (contract gap vs the MVC page's
   * /FinancialAccounts/GetAll), so the account filter options are collected from the accounts
   * present on the currently loaded page of operations.
   */
  readonly accountOptions = computed(() => {
    const seen = new Map<string, { id: string; name: string }>();
    for (const operation of this.store.page()?.items ?? []) {
      if (operation.accountId && operation.accountName) {
        seen.set(operation.accountId, { id: operation.accountId, name: operation.accountName });
      }
    }
    return [...seen.values()].sort((a, b) => a.name.localeCompare(b.name, 'ar'));
  });

  readonly statColumns: TableColumn<EmployeeStatRow>[] = [
    { key: 'name', header: 'الموظف', cell: (row) => row.name, sortable: true },
    {
      key: 'salesCount',
      header: 'مبيعات (عدد)',
      cell: (row) => row.salesCount,
      numeric: true,
      sortable: true,
      sortValue: (row) => row.salesCount,
    },
    {
      key: 'salesWeight',
      header: 'وزن المبيعات 21 (غ)',
      cell: (row) => formatWeight(row.salesWeight),
      numeric: true,
      sortable: true,
      sortValue: (row) => row.salesWeight,
    },
    { key: 'salesTotals', header: 'قيمة المبيعات', cell: (row) => row.salesTotals, numeric: true },
    {
      key: 'purchasesCount',
      header: 'مشتريات (عدد)',
      cell: (row) => row.purchasesCount,
      numeric: true,
      sortable: true,
      sortValue: (row) => row.purchasesCount,
    },
    {
      key: 'purchasesWeight',
      header: 'وزن المشتريات 21 (غ)',
      cell: (row) => formatWeight(row.purchasesWeight),
      numeric: true,
      sortable: true,
      sortValue: (row) => row.purchasesWeight,
    },
    {
      key: 'purchasesTotals',
      header: 'قيمة المشتريات',
      cell: (row) => row.purchasesTotals,
      numeric: true,
    },
  ];

  readonly skeletonRows = [0, 1, 2, 3, 4];
  readonly skeletonTableColumns = SKELETON_TABLE_COLUMNS;

  readonly receiptIcon = resolveIcon('receipt');
  readonly shoppingBagIcon = resolveIcon('shopping-bag');
  readonly eyeIcon = resolveIcon('eye');

  constructor() {
    void this.store.ensureKpis();
    void this.store.ensureEmployees();
    void this.store.loadTodayStats();
    void this.store.loadOperations(this.query());
  }

  applyFilters(): void {
    this.query.set({
      page: 1,
      pageSize: PAGE_SIZE,
      search: this.search() || undefined,
      fromDate: this.fromDate() || undefined,
      toDate: this.toDate() || undefined,
      operationType: this.operationType() || undefined,
      employeeId: this.employeeId() || undefined,
      accountId: this.accountId() || undefined,
    });
    void this.store.loadOperations(this.query());
  }

  resetFilters(): void {
    this.search.set('');
    this.fromDate.set('');
    this.toDate.set('');
    this.operationType.set('');
    this.employeeId.set('');
    this.accountId.set('');
    this.applyFilters();
  }

  goToPage(page: number): void {
    this.query.update((current) => ({ ...current, page }));
    void this.store.loadOperations(this.query());
  }

  refresh(): void {
    void this.store.refresh();
    void this.store.loadOperations(this.query());
  }

  onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.applyFilters();
    }
  }

  openDetail(operation: OperationRow): void {
    this.pendingDetail.set({ id: operation.id, operationType: operation.operationType });
    this.detailOpen.set(true);
    void this.store.loadDetail(operation.id, operation.operationType);
  }

  retryDetail(): void {
    const pending = this.pendingDetail();
    if (pending !== null) {
      void this.store.loadDetail(pending.id, pending.operationType as 'Sale' | 'Buy');
    }
  }

  closeDetail(): void {
    this.detailOpen.set(false);
    this.store.clearDetail();
  }
}
