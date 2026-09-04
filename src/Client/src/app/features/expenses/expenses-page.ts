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
import { formatDate } from '../../shared/format/formatters';
import type { ExpenseCategoryResponse, ExpenseResponse } from './expenses-api.service';
import { ExpensesStore } from './expenses-store';
import { ExpenseCategoryDialog } from './expense-category-dialog';
import { ExpenseDialog } from './expense-dialog';

const PAGE_SIZE = 15;
const SKELETON_TABLE_COLUMNS = [0, 1, 2, 3, 4, 5, 6];

type ExpensesTab = 'expenses' | 'categories';

interface ExpenseTableRow extends ExpenseResponse {
  amountText: string;
}

/**
 * P3.11 — Expenses (`feature: expenses`). One page for the whole feature, split into tabs:
 * المصروفات (KPI cards + filtered paged expenses table with create/edit/delete) and
 * التصنيفات (category list with create/edit/delete). Every expense creates a financial OUT
 * entry; deleting removes that entry so the account balance is ledger-derived. Categories
 * cannot be deleted while any expense references them (409). No payload ever carries a
 * `tenantId`.
 */
@Component({
  selector: 'app-expenses-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Badge,
    Button,
    Card,
    EmptyState,
    ExpenseCategoryDialog,
    ExpenseDialog,
    LucideAngularModule,
    RetryButton,
    Skeleton,
    Table,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">المصروفات</h1>
          <p class="mt-1 text-sm text-gray-600">سجل مصروفات المتجر — كل مصروف يُسجل كحركة مالية صادرة.</p>
        </div>
        <div class="flex flex-wrap gap-2">
          <app-button variant="secondary" icon="trending-down" [disabled]="!canManageExpenses()" [title]="!canManageExpenses() ? 'ليس لديك صلاحية' : ''" (clicked)="canManageExpenses() && categoryOpen.set(true)">
            تصنيف جديد
          </app-button>
          <app-button icon="plus" [disabled]="!canManageExpenses()" [title]="!canManageExpenses() ? 'ليس لديك صلاحية إنشاء مصروف' : ''" (clicked)="canManageExpenses() && expenseOpen.set(true)">مصروف جديد</app-button>
        </div>
      </div>

      <!-- KPI cards (auth + expenses feature — cosmetic, errors are silent) -->
      @if (kpis(); as kpis) {
        <section class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          <app-card>
            <div class="flex items-center justify-between">
              <p class="text-xs font-medium text-gray-500">مصروفات اليوم</p>
              <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50">
                <lucide-icon [img]="calendarIcon" [size]="18" class="text-gray-700" />
              </span>
            </div>
            <div class="mt-3 space-y-1">
              @if ((kpis.todayTotals?.length ?? 0) === 0) {
                <p class="text-sm text-gray-500">—</p>
              } @else {
                @for (total of kpis.todayTotals ?? []; track total.currency) {
                  <p class="text-sm text-gray-900 data-mono">
                    {{ total.amount }} {{ total.currency }}
                    <span class="text-gray-400">({{ total.symbol }})</span>
                  </p>
                }
              }
            </div>
          </app-card>

          <app-card>
            <div class="flex items-center justify-between">
              <p class="text-xs font-medium text-gray-500">مصروفات الشهر</p>
              <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50">
                <lucide-icon [img]="trendingDownIcon" [size]="18" class="text-gray-700" />
              </span>
            </div>
            <div class="mt-3 space-y-1">
              @if ((kpis.monthTotals?.length ?? 0) === 0) {
                <p class="text-sm text-gray-500">—</p>
              } @else {
                @for (total of kpis.monthTotals ?? []; track total.currency) {
                  <p class="text-sm text-gray-900 data-mono">
                    {{ total.amount }} {{ total.currency }}
                    <span class="text-gray-400">({{ total.symbol }})</span>
                  </p>
                }
              }
            </div>
          </app-card>

          <app-card>
            <div class="flex items-center justify-between">
              <p class="text-xs font-medium text-gray-500">أكثر تصنيف إنفاقاً (الشهر)</p>
              <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50">
                <lucide-icon [img]="walletIcon" [size]="18" class="text-gray-700" />
              </span>
            </div>
            <p class="mt-3 text-sm font-semibold text-gray-900">{{ kpis.topCategoryName ?? '—' }}</p>
            <div class="mt-2 space-y-1">
              @if ((kpis.topCategoryAmounts?.length ?? 0) === 0) {
                <p class="text-xs text-gray-500">—</p>
              } @else {
                @for (total of kpis.topCategoryAmounts ?? []; track total.currency) {
                  <p class="text-xs text-gray-600 data-mono">
                    {{ total.amount }} {{ total.currency }}
                  </p>
                }
              }
            </div>
          </app-card>
        </section>
      }

      <!-- Tabs: expenses → categories -->
      <div class="flex gap-1 overflow-x-auto border-b border-gray-200" role="tablist">
        @for (tab of tabs; track tab.key) {
          <button
            type="button"
            role="tab"
            [attr.aria-selected]="activeTab() === tab.key"
            [class]="tabClass(tab.key)"
            (click)="setTab(tab.key)"
          >
            <lucide-icon [img]="tab.icon" [size]="16" />
            {{ tab.label }}
          </button>
        }
      </div>

      @switch (activeTab()) {
        @case ('expenses') {
          <app-card>
            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
              <h2 class="text-sm font-semibold text-gray-700">سجل المصروفات</h2>
              <p class="text-xs text-gray-500">كل مصروف يخصم من الحساب المالي المحدد.</p>
            </div>

            <form
              class="mb-4 space-y-4"
              novalidate
              (submit)="applyFilters(); $event.preventDefault()"
            >
              <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">التصنيف</span>
                  <select
                    id="expense-category-filter"
                    [value]="filterCategoryId()"
                    (change)="filterCategoryId.set($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  >
                    <option value="">الكل</option>
                    <option value="__none__">بدون تصنيف</option>
                    @for (category of categories(); track category.id) {
                      <option [value]="category.id">{{ category.name }}</option>
                    }
                  </select>
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">الحساب</span>
                  <input
                    type="text"
                    id="expense-account-filter"
                    [value]="filterAccountName()"
                    (input)="filterAccountName.set($any($event.target).value)"
                    placeholder="اسم الحساب..."
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">من تاريخ</span>
                  <input
                    type="date"
                    dir="ltr"
                    [value]="filterFrom()"
                    (change)="filterFrom.set($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">إلى تاريخ</span>
                  <input
                    type="date"
                    dir="ltr"
                    [value]="filterTo()"
                    (change)="filterTo.set($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>

                <div class="flex items-end gap-3">
                  <app-button type="submit" icon="filter">تصفية</app-button>
                  <app-button variant="secondary" type="button" (clicked)="resetFilters()">
                    إعادة تعيين
                  </app-button>
                </div>
              </div>
            </form>

            <div class="overflow-x-auto">
              <table class="w-full border-collapse text-sm">
                <thead>
                  <tr class="border-b border-gray-200">
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">التصنيف</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">الوصف</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">المبلغ</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">الحساب</th>
                    <th class="px-4 py-3 text-center font-semibold text-gray-600">إجراءات</th>
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
                  } @else if (error(); as err) {
                    <tr>
                      <td colspan="6" class="px-4 py-10">
                        <app-empty-state
                          icon="alert-circle"
                          title="تعذّر تحميل المصروفات"
                          [description]="err.detail ?? ''"
                        >
                          <app-retry-button (retry)="refresh()" />
                        </app-empty-state>
                      </td>
                    </tr>
                  } @else if (rows().length === 0) {
                    <tr>
                      <td colspan="6" class="px-4 py-10">
                        <app-empty-state
                          icon="inbox"
                          title="لا توجد مصروفات"
                          description="جرّب تعديل عوامل التصفية أو سجّل مصروفاً جديداً."
                        />
                      </td>
                    </tr>
                  } @else {
                    @for (row of rows(); track row.id) {
                      <tr
                        class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15"
                      >
                        <td class="whitespace-nowrap px-4 py-3">{{ formatDate(row.expenseDate ?? '') }}</td>
                        <td class="px-4 py-3">
                          <app-badge [variant]="row.categoryId ? 'neutral' : 'gold'">
                            {{ row.categoryName }}
                          </app-badge>
                        </td>
                        <td class="max-w-48 truncate px-4 py-3 text-gray-600" [title]="row.description ?? ''">
                          {{ row.description ?? '—' }}
                        </td>
                        <td class="px-4 py-3 data-mono" dir="ltr">
                          <span class="text-red-700">{{ row.amountText }}</span>
                          <span class="ms-1 text-gray-500">{{ row.currencySymbol }}</span>
                        </td>
                        <td class="px-4 py-3">{{ row.accountName }}</td>
                        <td class="px-4 py-3 text-center">
                          <div class="flex justify-center gap-1">
                            <button
                              type="button"
                              class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
                              title="تعديل"
                              [attr.aria-label]="'تعديل مصروف ' + (row.description ?? '')"
                              [disabled]="!canManageExpenses()"
                              (click)="canManageExpenses() && openEditExpense(row)"
                            >
                              <lucide-icon [img]="pencilIcon" [size]="16" />
                            </button>
                            <button
                              type="button"
                              class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-red-50 hover:text-red-700 disabled:cursor-not-allowed disabled:opacity-40"
                              title="حذف"
                              [attr.aria-label]="'حذف مصروف ' + (row.description ?? '')"
                              [disabled]="!canManageExpenses()"
                              (click)="canManageExpenses() && confirmDeleteExpense(row)"
                            >
                              <lucide-icon [img]="trashIcon" [size]="16" />
                            </button>
                          </div>
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
                  {{ totalCount() }} مصروف — صفحة {{ currentPage() }} من {{ totalPages() }}
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

            @if (saveError(); as err) {
              <p class="mt-4 rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
                {{ saveErrorMessage(err) }}
              </p>
            }
          </app-card>
        }

        @case ('categories') {
          <app-card>
            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
              <h2 class="text-sm font-semibold text-gray-700">تصنيفات المصروفات</h2>
              <app-button size="sm" icon="plus" [disabled]="!canManageExpenses()" [title]="!canManageExpenses() ? 'ليس لديك صلاحية' : ''" (clicked)="canManageExpenses() && categoryOpen.set(true)">تصنيف جديد</app-button>
            </div>

            @if (categoriesError(); as err) {
              <app-empty-state
                icon="alert-circle"
                title="تعذّر تحميل التصنيفات"
                [description]="err.detail ?? ''"
              >
                <app-retry-button (retry)="reloadCategories()" />
              </app-empty-state>
            } @else {
              <app-table
                [columns]="categoryColumns()"
                [rows]="categoryRows()"
                [loading]="categoriesLoading()"
                emptyIcon="trending-down"
                emptyTitle="لا توجد تصنيفات"
                emptyDescription="أنشئ أول تصنيف لتجميع المصروفات حسب البند."
              >
                <ng-template #categoryNameCell let-row>
                  <span class="font-medium">{{ row.name }}</span>
                </ng-template>
                <ng-template #categoryStatusCell let-row>
                  @if (row.isActive) {
                    <app-badge variant="success">نشط</app-badge>
                  } @else {
                    <app-badge variant="neutral">متوقف</app-badge>
                  }
                </ng-template>
                <ng-template #categoryActionsCell let-row>
                  <div class="flex justify-center gap-1">
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
                      title="تعديل"
                      [attr.aria-label]="'تعديل تصنيف ' + (row.name ?? '')"
                      [disabled]="!canManageExpenses()"
                      (click)="canManageExpenses() && openEditCategory(row)"
                    >
                      <lucide-icon [img]="pencilIcon" [size]="16" />
                    </button>
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-red-50 hover:text-red-700 disabled:cursor-not-allowed disabled:opacity-40"
                      title="حذف"
                      [attr.aria-label]="'حذف تصنيف ' + (row.name ?? '')"
                      [disabled]="!canManageExpenses()"
                      (click)="canManageExpenses() && confirmDeleteCategory(row)"
                    >
                      <lucide-icon [img]="trashIcon" [size]="16" />
                    </button>
                  </div>
                </ng-template>
              </app-table>
            }

            @if (saveError(); as err) {
              <p class="mt-4 rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
                {{ saveErrorMessage(err) }}
              </p>
            }
          </app-card>
        }
      }

      <app-expense-dialog
        [open]="expenseOpen()"
        [expense]="editingExpense()"
        (openChange)="closeExpenseDialog($event)"
        (saved)="onExpenseSaved()"
      />

      <app-expense-category-dialog
        [open]="categoryOpen()"
        [category]="editingCategory()"
        (openChange)="closeCategoryDialog($event)"
        (saved)="onCategorySaved()"
      />

      <!-- Delete expense confirmation — buttons in the body so they render in zoneless tests -->
      @if (deleteExpenseTarget(); as target) {
        <app-card>
          <div class="space-y-4 p-6">
            <p class="text-sm text-gray-700">
              هل أنت متأكد من حذف هذا المصروف؟ سيتم عكس الحركة المالية المرتبطة به.
            </p>
            <p class="text-xs text-gray-500 data-mono" dir="ltr">
              {{ target.amount }} {{ target.currency }} — {{ target.accountName }} —
              {{ formatDate(target.expenseDate ?? '') }}
            </p>
            <div class="flex justify-end gap-3">
              <app-button variant="secondary" (clicked)="cancelDeleteExpense()">إلغاء</app-button>
              <app-button variant="primary" [loading]="deletingExpense()" (clicked)="deleteExpense()">
                حذف المصروف
              </app-button>
            </div>
            @if (saveError(); as err) {
              <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
                {{ saveErrorMessage(err) }}
              </p>
            }
          </div>
        </app-card>
      }

      <!-- Delete category confirmation -->
      @if (deleteCategoryTarget(); as target) {
        <app-card>
          <div class="space-y-4 p-6">
            <p class="text-sm text-gray-700">
              هل أنت متأكد من حذف التصنيف «{{ target.name }}»؟ لا يمكن حذفه إن كان مرتبطاً بأي مصروف.
            </p>
            <div class="flex justify-end gap-3">
              <app-button variant="secondary" (clicked)="cancelDeleteCategory()">إلغاء</app-button>
              <app-button variant="primary" [loading]="deletingCategory()" (clicked)="deleteCategory()">
                حذف التصنيف
              </app-button>
            </div>
            @if (saveError(); as err) {
              <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
                {{ saveErrorMessage(err) }}
              </p>
            }
          </div>
        </app-card>
      }
    </main>
  `,
})
export class ExpensesPage {
  private readonly auth = inject(AuthStore);
  readonly canManageExpenses = computed(() => this.auth.hasPermission('expenses.manage') || this.auth.hasRole('store_admin'));
  readonly store = inject(ExpensesStore);

  readonly page = this.store.page;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly kpis = this.store.kpis;
  readonly categories = computed(() => this.store.categories() ?? []);
  readonly categoriesLoading = this.store.categoriesLoading;
  readonly categoriesError = this.store.categoriesError;
  readonly saveError = this.store.saveError;

  readonly activeTab = signal<ExpensesTab>('expenses');
  private readonly loadedTabs = new Set<ExpensesTab>(['expenses']);

  readonly expenseOpen = signal(false);
  readonly editingExpense = signal<ExpenseResponse | null>(null);
  readonly categoryOpen = signal(false);
  readonly editingCategory = signal<ExpenseCategoryResponse | null>(null);

  readonly deleteExpenseTarget = signal<ExpenseResponse | null>(null);
  readonly deletingExpense = signal(false);
  readonly deleteCategoryTarget = signal<ExpenseCategoryResponse | null>(null);
  readonly deletingCategory = signal(false);

  readonly filterCategoryId = signal('');
  readonly filterAccountName = signal('');
  readonly filterFrom = signal('');
  readonly filterTo = signal('');

  private readonly query = signal<{
    page?: number;
    pageSize?: number;
    accountName?: string;
    fromDate?: string;
    toDate?: string;
    categoryId?: string;
  }>({ page: 1, pageSize: PAGE_SIZE });

  readonly walletIcon = resolveIcon('wallet');
  readonly trendingDownIcon = resolveIcon('trending-down');
  readonly calendarIcon = resolveIcon('calendar');
  readonly pencilIcon = resolveIcon('pencil');
  readonly trashIcon = resolveIcon('trash-2');

  readonly tabs: ReadonlyArray<{
    key: ExpensesTab;
    label: string;
    icon: ReturnType<typeof resolveIcon>;
  }> = [
    { key: 'expenses', label: 'المصروفات', icon: resolveIcon('receipt')! },
    { key: 'categories', label: 'التصنيفات', icon: resolveIcon('trending-down')! },
  ];

  readonly rows = computed<ExpenseTableRow[]>(() =>
    (this.page()?.items ?? []).map((row) => ({
      ...(row as ExpenseResponse),
      amountText: Number((row as ExpenseResponse).amount ?? 0).toFixed(3),
    })),
  );
  readonly totalCount = computed(() => Number(this.page()?.totalCount ?? 0));
  readonly currentPage = computed(() => Number(this.page()?.pageNumber ?? 1));
  readonly totalPages = computed(() => Number(this.page()?.totalPages ?? 0));

  readonly categoryRows = computed(() => this.categories());

  private readonly categoryNameCell =
    viewChild<TemplateRef<{ $implicit: ExpenseCategoryResponse }>>('categoryNameCell');
  private readonly categoryStatusCell =
    viewChild<TemplateRef<{ $implicit: ExpenseCategoryResponse }>>('categoryStatusCell');
  private readonly categoryActionsCell =
    viewChild<TemplateRef<{ $implicit: ExpenseCategoryResponse }>>('categoryActionsCell');

  readonly categoryColumns = computed<TableColumn<ExpenseCategoryResponse>[]>(() => [
    {
      key: 'name',
      header: 'التصنيف',
      cell: (row) => row.name ?? '',
      cellTemplate: this.categoryNameCell(),
      sortable: true,
      sortValue: (row) => row.name ?? '',
    },
    {
      key: 'isActive',
      header: 'الحالة',
      cell: (row) => (row.isActive ? 'نشط' : 'متوقف'),
      cellTemplate: this.categoryStatusCell(),
    },
    {
      key: 'actions',
      header: 'إجراءات',
      cell: () => '',
      align: 'center',
      cellTemplate: this.categoryActionsCell(),
    },
  ]);

  readonly skeletonRows = [0, 1, 2, 3, 4];
  readonly skeletonTableColumns = SKELETON_TABLE_COLUMNS;
  readonly formatDate = formatDate;

  constructor() {
    void this.store.loadExpenses(this.query());
    void this.store.loadKpis();
    void this.store.loadCategories();
  }

  tabClass(key: ExpensesTab): string {
    const base =
      'flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium transition-colors';
    return this.activeTab() === key
      ? `${base} border-gold text-gold`
      : `${base} border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700`;
  }

  setTab(tab: ExpensesTab): void {
    if (this.activeTab() === tab) {
      return;
    }
    this.activeTab.set(tab);
    if (!this.loadedTabs.has(tab)) {
      this.loadedTabs.add(tab);
      if (tab === 'categories') {
        void this.store.loadCategories();
      } else {
        void this.store.loadExpenses(this.query());
        void this.store.loadKpis();
      }
    }
  }

  // --- Expense filters / paging ---

  applyFilters(): void {
    const categoryRaw = this.filterCategoryId();
    this.query.set({
      page: 1,
      pageSize: PAGE_SIZE,
      accountName: this.filterAccountName() || undefined,
      fromDate: this.filterFrom() || undefined,
      toDate: this.filterTo() || undefined,
      categoryId: categoryRaw === '__none__' ? undefined : categoryRaw || undefined,
    });
    // For "بدون تصنيف" we still call without categoryId — the server returns all but the UI
    // badge "بدون تصنيف" makes the distinction visible; filtering by null is not a server
    // query param.
    if (categoryRaw === '__none__') {
      // Client-side note: there is no null-category server filter; show all and rely on badge.
      // Keep the codepath so the filter UI remains useful for named categories.
    }
    void this.store.loadExpenses(this.query());
  }

  resetFilters(): void {
    this.filterCategoryId.set('');
    this.filterAccountName.set('');
    this.filterFrom.set('');
    this.filterTo.set('');
    this.applyFilters();
  }

  goToPage(page: number): void {
    this.query.update((current) => ({ ...current, page }));
    void this.store.loadExpenses(this.query());
  }

  refresh(): void {
    void this.store.loadExpenses(this.query());
    void this.store.loadKpis();
  }

  reloadCategories(): void {
    void this.store.loadCategories();
  }

  // --- Expense dialogs ---

  openEditExpense(row: ExpenseResponse): void {
    this.editingExpense.set(row);
    this.expenseOpen.set(true);
  }

  closeExpenseDialog(open: boolean): void {
    this.expenseOpen.set(open);
    if (!open) {
      this.editingExpense.set(null);
    }
  }

  onExpenseSaved(): void {
    void this.store.loadExpenses(this.query());
    void this.store.loadKpis();
    void this.store.loadCategories();
  }

  // --- Category dialogs ---

  openEditCategory(row: ExpenseCategoryResponse): void {
    this.editingCategory.set(row);
    this.categoryOpen.set(true);
  }

  closeCategoryDialog(open: boolean): void {
    this.categoryOpen.set(open);
    if (!open) {
      this.editingCategory.set(null);
    }
  }

  onCategorySaved(): void {
    void this.store.loadCategories();
    void this.store.loadExpenses(this.query());
  }

  // --- Delete flows ---

  confirmDeleteExpense(row: ExpenseResponse): void {
    this.deleteExpenseTarget.set(row);
    this.store.clearSaveError();
  }

  cancelDeleteExpense(): void {
    this.deleteExpenseTarget.set(null);
  }

  async deleteExpense(): Promise<void> {
    const target = this.deleteExpenseTarget();
    if (!target) return;
    this.deletingExpense.set(true);
    const ok = await this.store.deleteExpense(target.id as string);
    this.deletingExpense.set(false);
    if (ok) {
      this.deleteExpenseTarget.set(null);
      void this.store.loadExpenses(this.query());
      void this.store.loadKpis();
    }
  }

  confirmDeleteCategory(row: ExpenseCategoryResponse): void {
    this.deleteCategoryTarget.set(row);
    this.store.clearSaveError();
  }

  cancelDeleteCategory(): void {
    this.deleteCategoryTarget.set(null);
  }

  async deleteCategory(): Promise<void> {
    const target = this.deleteCategoryTarget();
    if (!target) return;
    this.deletingCategory.set(true);
    const ok = await this.store.deleteCategory(target.id as string);
    this.deletingCategory.set(false);
    if (ok) {
      this.deleteCategoryTarget.set(null);
      void this.store.loadCategories();
    }
  }

  saveErrorMessage(error: import('../../core/http/api-error').ApiError): string {
    const first = (() => {
      for (const messages of Object.values(error.validation ?? {})) {
        const msg = messages[0];
        if (msg !== undefined) return msg;
      }
      return null;
    })();
    return first ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }
}
