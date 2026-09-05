import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
  type TemplateRef,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth-store';

import {
  Badge,
  Button,
  Card,
  EmptyState,
  RetryButton,
  Skeleton,
  Table,
  resolveIcon,
  type TableColumn,
} from '../../shared/ui';
import { formatDate, formatWeight } from '../../shared/format/formatters';
import type {
  SupplierFinancialTransactionResponse,
  SupplierResponse,
  SupplierTransactionQuery,
} from './suppliers-api.service';
import { SuppliersStore } from './suppliers-store';
import { DeliveryDialog } from './delivery-dialog';
import { FinancialTransactionDialog } from './financial-transaction-dialog';
import { ManufacturingDialog } from './manufacturing-dialog';
import { ScrapGoldDialog } from './scrap-gold-dialog';
import { SupplierDetailDialog } from './supplier-detail-dialog';
import { SupplierFormDialog, type SupplierFormMode } from './supplier-form-dialog';
import { TransactionPaymentsDialog } from './transaction-payments-dialog';

const PAGE_SIZE = 15;
const SKELETON_TABLE_COLUMNS = [0, 1, 2, 3, 4, 5, 6, 7];

/** Display row for a supplier (21K gold balance + manufacturing balance as plain text). */
export interface SupplierTableRow extends SupplierResponse {
  goldBalanceText: string;
  manufacturingBalanceText: string;
}

function toRow(supplier: SupplierResponse): SupplierTableRow {
  return {
    ...supplier,
    goldBalanceText: formatWeight(Number(supplier.goldBalance ?? 0)),
    manufacturingBalanceText: formatWeight(Number(supplier.manufacturingBalance ?? 0)),
  };
}

/**
 * P3.6 — Suppliers (`feature: suppliers`). One page for the whole feature: the supplier
 * list (create/edit/toggle + detail), the supplier financial KPIs (by currency), and the
 * paged supplier financial transactions (filters + pagination + create + payments).
 * Delivery / scrap-gold / manufacturing-payment actions live in the header actions.
 */
@Component({
  selector: 'app-suppliers-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Badge,
    Button,
    Card,
    DeliveryDialog,
    EmptyState,
    FinancialTransactionDialog,
    LucideAngularModule,
    ManufacturingDialog,
    RetryButton,
    ScrapGoldDialog,
    Skeleton,
    SupplierDetailDialog,
    SupplierFormDialog,
    Table,
    TransactionPaymentsDialog,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">الموردون</h1>
          <p class="mt-1 text-sm text-gray-600">
            الموردون، التسليمات، دفعات الكسر والتصنيع، والسلف.
          </p>
        </div>
        <div class="flex flex-wrap gap-2">
          <app-button
            variant="secondary"
            icon="truck"
            [disabled]="!canManageSuppliers()"
            [title]="!canManageSuppliers() ? 'ليس لديك صلاحية' : ''"
            (clicked)="canManageSuppliers() && deliveryOpen.set(true)"
          >
            تسليم ذهب
          </app-button>
          <app-button
            variant="secondary"
            icon="coins"
            [disabled]="!canManageSuppliers()"
            [title]="!canManageSuppliers() ? 'ليس لديك صلاحية' : ''"
            (clicked)="canManageSuppliers() && scrapOpen.set(true)"
          >
            دفع كسر
          </app-button>
          <app-button
            variant="secondary"
            icon="receipt"
            [disabled]="!canManageSuppliers()"
            [title]="!canManageSuppliers() ? 'ليس لديك صلاحية' : ''"
            (clicked)="canManageSuppliers() && manufacturingOpen.set(true)"
          >
            دفع تصنيع
          </app-button>
          <app-button
            icon="user-plus"
            [disabled]="!canManageSuppliers()"
            [title]="!canManageSuppliers() ? 'ليس لديك صلاحية إضافة مورد' : ''"
            (clicked)="canManageSuppliers() && openCreate()"
            >إضافة مورد</app-button
          >
        </div>
      </div>

      @if (kpis()?.byCurrency?.length) {
        <section class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          @for (item of kpis()?.byCurrency ?? []; track item.currency) {
            <app-card>
              <div class="flex items-center justify-between">
                <p class="text-xs font-medium text-gray-500">سلف الموردين — {{ item.currency }}</p>
                <span
                  class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50"
                >
                  <lucide-icon [img]="walletIcon" [size]="18" class="text-gray-700" />
                </span>
              </div>
              @if (kpisLoading() && kpis() === null) {
                <app-skeleton class="mt-3" width="6rem" height="1.75rem" />
              } @else {
                <p class="mt-3 text-2xl font-semibold text-gray-900 data-mono">
                  {{ item.netBalanceDisplay }}
                  <span class="ms-1 text-sm font-normal text-gray-500">{{ item.symbol }}</span>
                </p>
                <p class="mt-2 text-xs text-gray-500" data-mono>
                  له {{ item.totalFromSupplierDisplay }} · لنا {{ item.totalToSupplierDisplay }} ·
                  {{ item.transactionCount }} معاملة
                </p>
              }
            </app-card>
          }
        </section>
      }

      <app-card title="قائمة الموردين">
        @if (error(); as error) {
          <app-empty-state
            icon="alert-circle"
            title="تعذّر تحميل الموردين"
            [description]="error.detail ?? ''"
          >
            <app-retry-button (retry)="reload()" />
          </app-empty-state>
        } @else {
          <app-table
            [columns]="supplierColumns()"
            [rows]="supplierRows()"
            [loading]="loading()"
            emptyIcon="inbox"
            emptyTitle="لا يوجد موردون"
            emptyDescription="أضف أول مورد ليتمكن من تسجيل التسليمات والدفعات."
          >
            <ng-template #nameCell let-row>
              <span class="flex items-center gap-2">
                <span class="font-semibold">{{ row.name }}</span>
              </span>
            </ng-template>
            <ng-template #statusCell let-row>
              @if (row.isActive) {
                <app-badge variant="success">نشط</app-badge>
              } @else {
                <app-badge variant="neutral">متوقف</app-badge>
              }
            </ng-template>
            <ng-template #actionsCell let-row>
              <div class="flex justify-center gap-1">
                <button
                  type="button"
                  class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800"
                  title="عرض التفاصيل"
                  [attr.aria-label]="'عرض تفاصيل ' + (row.name ?? '')"
                  (click)="openDetail(row)"
                >
                  <lucide-icon [img]="eyeIcon" [size]="16" />
                </button>
                <button
                  type="button"
                  class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
                  title="تعديل"
                  [attr.aria-label]="'تعديل ' + (row.name ?? '')"
                  [disabled]="!canManageSuppliers() || mutatingId() === row.id"
                  [attr.title]="!canManageSuppliers() ? 'ليس لديك صلاحية التعديل' : 'تعديل'"
                  (click)="canManageSuppliers() && openEdit(row)"
                >
                  <lucide-icon [img]="pencilIcon" [size]="16" />
                </button>
                <button
                  type="button"
                  class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
                  [title]="row.isActive ? 'إيقاف المورد' : 'تفعيل المورد'"
                  [attr.aria-label]="
                    row.isActive ? 'إيقاف ' + (row.name ?? '') : 'تفعيل ' + (row.name ?? '')
                  "
                  [disabled]="!canManageSuppliers() || mutatingId() === row.id"
                  (click)="canManageSuppliers() && toggleActive(row)"
                >
                  <lucide-icon [img]="powerIcon" [size]="16" />
                </button>
              </div>
            </ng-template>
          </app-table>
        }
      </app-card>

      <app-card>
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 class="text-sm font-semibold text-gray-700">المعاملات المالية (السلف)</h2>
          <app-button
            size="sm"
            icon="wallet"
            [disabled]="!canManageSuppliers()"
            [title]="!canManageSuppliers() ? 'ليس لديك صلاحية' : ''"
            (clicked)="canManageSuppliers() && transactionOpen.set(true)"
          >
            معاملة مالية جديدة
          </app-button>
        </div>

        <form class="mb-4 space-y-4" novalidate (submit)="applyFilters(); $event.preventDefault()">
          <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">المورد</span>
              <select
                [value]="filterSupplierId()"
                (change)="filterSupplierId.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">الكل</option>
                @for (supplier of suppliers() ?? []; track supplier.id) {
                  <option [value]="supplier.id">{{ supplier.name }}</option>
                }
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">الاتجاه</span>
              <select
                [value]="filterDirection()"
                (change)="filterDirection.set($any($event.target).value)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">الكل</option>
                <option value="1">له</option>
                <option value="2">لنا</option>
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-gray-700">بحث</span>
              <input
                type="text"
                [value]="filterSearch()"
                (input)="filterSearch.set($any($event.target).value)"
                (keydown)="onSearchKeydown($event)"
                placeholder="اسم المورد..."
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
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المورد</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">النوع</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المبلغ</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">المتبقي</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">العملة</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ</th>
                <th class="px-4 py-3 text-start font-semibold text-gray-600">ملاحظات</th>
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
                  <td colspan="8" class="px-4 py-10">
                    <app-empty-state
                      icon="alert-circle"
                      title="تعذّر تحميل المعاملات"
                      [description]="error.detail ?? ''"
                    >
                      <app-retry-button (retry)="refreshTransactions()" />
                    </app-empty-state>
                  </td>
                </tr>
              } @else if (transactionRows().length === 0) {
                <tr>
                  <td colspan="8" class="px-4 py-10">
                    <app-empty-state
                      icon="inbox"
                      title="لا توجد معاملات مالية"
                      description="جرّب تعديل عوامل التصفية أو أضف معاملة جديدة."
                    />
                  </td>
                </tr>
              } @else {
                @for (transaction of transactionRows(); track transaction.id) {
                  <tr
                    class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15"
                  >
                    <td class="px-4 py-3 font-medium">{{ transaction.supplierName }}</td>
                    <td class="px-4 py-3">
                      @if (transaction.directionLabel === 'له') {
                        <app-badge variant="gold">{{ transaction.directionLabel }}</app-badge>
                      } @else {
                        <app-badge variant="success">{{ transaction.directionLabel }}</app-badge>
                      }
                    </td>
                    <td class="px-4 py-3 data-mono" dir="ltr">{{ transaction.amountDisplay }}</td>
                    <td class="px-4 py-3 data-mono" dir="ltr">
                      {{ transaction.outstandingBalanceDisplay }}
                    </td>
                    <td class="px-4 py-3">{{ transaction.currency }}</td>
                    <td class="px-4 py-3">{{ formatDate(transaction.createdAt ?? '') }}</td>
                    <td
                      class="max-w-48 truncate px-4 py-3 text-gray-600"
                      title="{{ transaction.notes ?? '' }}"
                    >
                      {{ transaction.notes ?? '—' }}
                    </td>
                    <td class="px-4 py-3 text-center">
                      <button
                        type="button"
                        class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800"
                        title="عرض الدفعات / تسجيل دفعة"
                        [attr.aria-label]="'دفعات معاملة ' + (transaction.supplierName ?? '')"
                        (click)="openPayments(transaction)"
                      >
                        <lucide-icon [img]="walletIcon" [size]="16" />
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
              {{ totalCount() }} معاملة — صفحة {{ currentPage() }} من {{ totalPages() }}
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

      <app-supplier-form-dialog
        [open]="formOpen()"
        [mode]="formMode()"
        [supplier]="formSupplier()"
        (openChange)="formOpen.set(false)"
        (saved)="formOpen.set(false)"
      />

      <app-supplier-detail-dialog
        [open]="detailOpen()"
        [supplierId]="detailSupplierId()"
        (openChange)="closeDetail()"
      />

      <app-delivery-dialog
        [open]="deliveryOpen()"
        (openChange)="deliveryOpen.set(false)"
        (saved)="deliveryOpen.set(false)"
      />
      <app-scrap-gold-dialog
        [open]="scrapOpen()"
        (openChange)="scrapOpen.set(false)"
        (saved)="scrapOpen.set(false)"
      />
      <app-manufacturing-dialog
        [open]="manufacturingOpen()"
        (openChange)="manufacturingOpen.set(false)"
        (saved)="manufacturingOpen.set(false)"
      />

      <app-financial-transaction-dialog
        [open]="transactionOpen()"
        (openChange)="transactionOpen.set(false)"
        (saved)="onTransactionSaved()"
      />

      <app-transaction-payments-dialog
        [open]="paymentsOpen()"
        [transaction]="paymentsTransaction()"
        (openChange)="closePayments()"
        (saved)="refreshTransactions()"
      />
    </main>
  `,
})
export class SuppliersPage {
  private readonly auth = inject(AuthStore);
  readonly canManageSuppliers = computed(
    () => this.auth.hasPermission('suppliers.manage') || this.auth.hasRole('store_admin'),
  );
  readonly store = inject(SuppliersStore);

  readonly suppliers = this.store.suppliers;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly kpis = this.store.kpis;
  readonly kpisLoading = this.store.kpisLoading;
  readonly tableLoading = this.store.tableLoading;
  readonly tableError = this.store.tableError;
  readonly mutatingId = this.store.mutatingId;

  readonly formOpen = signal(false);
  readonly formMode = signal<SupplierFormMode>('create');
  readonly formSupplier = signal<SupplierResponse | null>(null);
  readonly detailOpen = signal(false);
  readonly detailSupplierId = signal<string | null>(null);
  readonly deliveryOpen = signal(false);
  readonly scrapOpen = signal(false);
  readonly manufacturingOpen = signal(false);
  readonly transactionOpen = signal(false);
  readonly paymentsOpen = signal(false);
  readonly paymentsTransaction = signal<SupplierFinancialTransactionResponse | null>(null);

  readonly filterSupplierId = signal('');
  readonly filterDirection = signal('');
  readonly filterSearch = signal('');

  private readonly query = signal<SupplierTransactionQuery>({ page: 1, pageSize: PAGE_SIZE });

  readonly supplierRows = computed(() => (this.suppliers() ?? []).map(toRow));
  readonly transactionRows = computed(() =>
    (this.store.page()?.items ?? []).map((transaction) => ({
      ...transaction,
      amountDisplay: String(Number(transaction.amount ?? 0).toFixed(3)),
    })),
  );

  readonly totalCount = computed(() => Number(this.store.page()?.totalCount ?? 0));
  readonly currentPage = computed(() => Number(this.store.page()?.page ?? 1));
  readonly totalPages = computed(() => Number(this.store.page()?.totalPages ?? 0));

  readonly nameCell = viewChild<TemplateRef<{ $implicit: SupplierTableRow }>>('nameCell');
  readonly statusCell = viewChild<TemplateRef<{ $implicit: SupplierTableRow }>>('statusCell');
  readonly actionsCell = viewChild<TemplateRef<{ $implicit: SupplierTableRow }>>('actionsCell');

  readonly supplierColumns = computed<TableColumn<SupplierTableRow>[]>(() => [
    {
      key: 'name',
      header: 'اسم المورد',
      cell: (row) => row.name ?? '',
      cellTemplate: this.nameCell(),
      sortable: true,
      sortValue: (row) => row.name ?? '',
    },
    {
      key: 'primaryPhone',
      header: 'الهاتف',
      cell: (row) => row.primaryPhone ?? '',
    },
    {
      key: 'goldBalance',
      header: 'رصيد الذهب (21ك غ)',
      cell: (row) => row.goldBalanceText,
      numeric: true,
      sortable: true,
      sortValue: (row) => Number(row.goldBalance ?? 0),
    },
    {
      key: 'manufacturingBalance',
      header: 'أجور التصنيع',
      cell: (row) => row.manufacturingBalanceText,
      numeric: true,
      sortable: true,
      sortValue: (row) => Number(row.manufacturingBalance ?? 0),
    },
    {
      key: 'lastTransactionDate',
      header: 'آخر عملية',
      cell: (row) => (row.lastTransactionDate ? formatDate(row.lastTransactionDate) : '—'),
      sortable: true,
      sortValue: (row) =>
        row.lastTransactionDate ? new Date(row.lastTransactionDate).getTime() : 0,
    },
    {
      key: 'status',
      header: 'الحالة',
      cell: (row) => (row.isActive ? 'نشط' : 'متوقف'),
      cellTemplate: this.statusCell(),
    },
    {
      key: 'actions',
      header: 'إجراءات',
      cell: () => '',
      align: 'center',
      cellTemplate: this.actionsCell(),
    },
  ]);

  readonly skeletonRows = [0, 1, 2, 3, 4];
  readonly skeletonTableColumns = SKELETON_TABLE_COLUMNS;

  readonly walletIcon = resolveIcon('wallet');
  readonly eyeIcon = resolveIcon('eye');
  readonly pencilIcon = resolveIcon('pencil');
  readonly powerIcon = resolveIcon('power');

  readonly formatDate = formatDate;
  readonly Number = Number;

  constructor() {
    void this.store.ensureLoaded();
    void this.store.loadKpis();
    void this.store.loadTransactions(this.query());
  }

  reload(): void {
    void this.store.load();
  }

  toggleActive(row: SupplierResponse): void {
    void this.store.toggleActive(row.id ?? '');
  }

  openCreate(): void {
    this.formMode.set('create');
    this.formSupplier.set(null);
    this.formOpen.set(true);
  }

  openEdit(supplier: SupplierResponse): void {
    this.formMode.set('edit');
    this.formSupplier.set(supplier);
    this.formOpen.set(true);
  }

  openDetail(supplier: SupplierResponse): void {
    this.detailSupplierId.set(supplier.id ?? null);
    this.detailOpen.set(true);
    if (supplier.id) {
      void this.store.loadDetail(supplier.id);
    }
  }

  closeDetail(): void {
    this.detailOpen.set(false);
    this.store.clearDetail();
  }

  applyFilters(): void {
    this.query.set({
      page: 1,
      pageSize: PAGE_SIZE,
      supplierId: this.filterSupplierId() || undefined,
      direction:
        this.filterDirection() === '' ? undefined : (Number(this.filterDirection()) as 1 | 2),
      search: this.filterSearch() || undefined,
    });
    void this.store.loadTransactions(this.query());
  }

  resetFilters(): void {
    this.filterSupplierId.set('');
    this.filterDirection.set('');
    this.filterSearch.set('');
    this.applyFilters();
  }

  goToPage(page: number): void {
    this.query.update((current) => ({ ...current, page }));
    void this.store.loadTransactions(this.query());
  }

  refreshTransactions(): void {
    void this.store.loadKpis();
    void this.store.loadTransactions(this.query());
  }

  onTransactionSaved(): void {
    this.transactionOpen.set(false);
    this.refreshTransactions();
  }

  openPayments(transaction: SupplierFinancialTransactionResponse): void {
    this.paymentsTransaction.set(transaction);
    this.paymentsOpen.set(true);
  }

  closePayments(): void {
    this.paymentsOpen.set(false);
    this.paymentsTransaction.set(null);
  }

  onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.applyFilters();
    }
  }
}
