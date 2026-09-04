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
import type { EmployeeResponse } from './hr-api.service';
import { HrStore } from './hr-store';
import { EmployeeDialog } from './employee-dialog';
import { PaySalaryDialog } from './pay-salary-dialog';

const PAGE_SIZE = 15;
const SKELETON_TABLE_COLUMNS = [0, 1, 2, 3, 4, 5, 6, 7];

type HrTab = 'employees' | 'payments';

/**
 * P3.12 — HR (`feature: hr`). One page for the whole feature, split into tabs:
 * الموظفون (list + create/update/toggle + pay-salary with period summary) and
 * سجل الرواتب (paged salary payments with employeeName/fromDate/toDate filters).
 * Salary payments create a financial OUT entry, so paying hits the ledger. No payload
 * ever carries a `tenantId`. The unlinked-users list is best-effort for creation.
 */
@Component({
  selector: 'app-hr-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Badge,
    Button,
    Card,
    EmployeeDialog,
    EmptyState,
    LucideAngularModule,
    PaySalaryDialog,
    RetryButton,
    Skeleton,
    Table,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">الموظفون</h1>
          <p class="mt-1 text-sm text-gray-600">إدارة الموظفين ورواتبهم — كل دفعة راتب تُسجل كحركة مالية صادرة.</p>
        </div>
        <app-button icon="plus" (clicked)="employeeOpen.set(true)">موظف جديد</app-button>
      </div>

      <!-- Tabs -->
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
        @case ('employees') {
          <app-card>
            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
              <h2 class="text-sm font-semibold text-gray-700">قائمة الموظفين</h2>
              <p class="text-xs text-gray-500">الراتب اليومي لا يدعم دفع الرواتب.</p>
            </div>

            @if (employeesError(); as err) {
              <app-empty-state
                icon="alert-circle"
                title="تعذّر تحميل الموظفين"
                [description]="err.detail ?? ''"
              >
                <app-retry-button (retry)="reloadEmployees()" />
              </app-empty-state>
            } @else {
              <app-table
                [columns]="employeeColumns()"
                [rows]="employeeRows()"
                [loading]="employeesLoading()"
                emptyIcon="users"
                emptyTitle="لا يوجد موظفون"
                emptyDescription="أنشئ أول موظف لبدء إدارة الرواتب."
              >
                <ng-template #employeeNameCell let-row>
                  <div class="flex flex-col">
                    <span class="font-semibold">{{ row.fullName }}</span>
                    <span class="text-xs text-gray-500">{{ row.roleName }}</span>
                  </div>
                </ng-template>
                <ng-template #employeeSalaryCell let-row>
                  <span dir="ltr" class="data-mono">{{ row.salary }} {{ currencyCode(row.currency) }}</span>
                  <span class="ms-2 text-xs text-gray-500">{{ row.salaryCycleName }}</span>
                </ng-template>
                <ng-template #employeeUserCell let-row>
                  @if (row.userEmail) {
                    <span class="text-xs">{{ row.userEmail }}</span>
                  } @else {
                    <span class="text-xs text-gray-400">—</span>
                  }
                </ng-template>
                <ng-template #employeeStatusCell let-row>
                  @if (row.isActive) {
                    <app-badge variant="success">نشط</app-badge>
                  } @else {
                    <app-badge variant="neutral">متوقف</app-badge>
                  }
                </ng-template>
                <ng-template #employeeActionsCell let-row>
                  <div class="flex justify-center gap-1">
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800"
                      title="تعديل"
                      [attr.aria-label]="'تعديل ' + (row.fullName ?? '')"
                      (click)="openEdit(row)"
                    >
                      <lucide-icon [img]="pencilIcon" [size]="16" />
                    </button>
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800"
                      title="تفعيل/إيقاف"
                      [attr.aria-label]="'تفعيل/إيقاف ' + (row.fullName ?? '')"
                      (click)="toggleActive(row)"
                    >
                      <lucide-icon [img]="powerIcon" [size]="16" />
                    </button>
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800 disabled:opacity-40"
                      title="دفع راتب"
                      [attr.aria-label]="'دفع راتب ' + (row.fullName ?? '')"
                      [disabled]="!row.isActive || row.salaryCycle === 1"
                      (click)="openPay(row)"
                    >
                      <lucide-icon [img]="walletIcon" [size]="16" />
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

        @case ('payments') {
          <app-card>
            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
              <h2 class="text-sm font-semibold text-gray-700">سجل دفع الرواتب</h2>
              <p class="text-xs text-gray-500">كل دفعة تُسجل كحركة صادرة على الحساب المالي.</p>
            </div>

            <form
              class="mb-4 space-y-4"
              novalidate
              (submit)="applyPaymentFilters(); $event.preventDefault()"
            >
              <div class="grid grid-cols-1 gap-4 md:grid-cols-4">
                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">اسم الموظف</span>
                  <input
                    id="pay-employee-filter"
                    type="text"
                    [value]="paymentFilterEmployeeName()"
                    (input)="paymentFilterEmployeeName.set($any($event.target).value)"
                    placeholder="مثال: أحمد"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">من تاريخ</span>
                  <input
                    type="date"
                    dir="ltr"
                    [value]="paymentFilterFrom()"
                    (change)="paymentFilterFrom.set($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">إلى تاريخ</span>
                  <input
                    type="date"
                    dir="ltr"
                    [value]="paymentFilterTo()"
                    (change)="paymentFilterTo.set($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>
                <div class="flex items-end gap-3">
                  <app-button type="submit" icon="filter">تصفية</app-button>
                  <app-button variant="secondary" type="button" (clicked)="resetPaymentFilters()">
                    إعادة تعيين
                  </app-button>
                </div>
              </div>
            </form>

            <div class="overflow-x-auto">
              <table class="w-full border-collapse text-sm">
                <thead>
                  <tr class="border-b border-gray-200">
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">الموظف</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">تاريخ الدفع</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">المستحق</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">المبلغ</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">الحساب</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">ملاحظات</th>
                    <th class="px-4 py-3 text-center font-semibold text-gray-600">الحالة</th>
                  </tr>
                </thead>
                <tbody>
                  @if (paymentsLoading()) {
                    @for (row of skeletonRows; track $index) {
                      <tr class="border-b border-gray-100">
                        @for (col of skeletonTableColumns; track col) {
                          <td class="px-4 py-3"><app-skeleton height="1rem" /></td>
                        }
                      </tr>
                    }
                  } @else if (paymentsError(); as err) {
                    <tr>
                      <td colspan="7" class="px-4 py-10">
                        <app-empty-state
                          icon="alert-circle"
                          title="تعذّر تحميل سجل الرواتب"
                          [description]="err.detail ?? ''"
                        >
                          <app-retry-button (retry)="refreshPayments()" />
                        </app-empty-state>
                      </td>
                    </tr>
                  } @else if (paymentRows().length === 0) {
                    <tr>
                      <td colspan="7" class="px-4 py-10">
                        <app-empty-state
                          icon="inbox"
                          title="لا توجد دفعات رواتب"
                          description="جرّب تعديل عوامل التصفية."
                        />
                      </td>
                    </tr>
                  } @else {
                    @for (payment of paymentRows(); track payment.id) {
                      <tr class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15">
                        <td class="px-4 py-3 font-medium">{{ payment.employeeName }}</td>
                        <td class="px-4 py-3">{{ formatDate(payment.paymentDate ?? '') }}</td>
                        <td class="px-4 py-3 data-mono" dir="ltr">{{ payment.salaryAmount }}</td>
                        <td class="px-4 py-3 data-mono font-semibold text-red-700" dir="ltr">
                          −{{ payment.amount }}
                        </td>
                        <td class="px-4 py-3">{{ payment.accountName ?? '—' }}</td>
                        <td class="max-w-40 truncate px-4 py-3 text-gray-600" [title]="payment.notes ?? ''">
                          {{ payment.notes ?? '—' }}
                        </td>
                        <td class="px-4 py-3 text-center">
                          @if (payment.isOnSchedule) {
                            <app-badge variant="success">في الموعد</app-badge>
                          } @else {
                            <app-badge variant="warning">متأخر</app-badge>
                          }
                        </td>
                      </tr>
                    }
                  }
                </tbody>
              </table>
            </div>

            @if (paymentsTotalPages() > 1) {
              <div class="mt-4 flex items-center justify-between border-t border-gray-100 pt-4">
                <span class="text-xs text-gray-500 data-mono">
                  {{ paymentsTotalCount() }} دفعة — صفحة {{ paymentsPage() }} من {{ paymentsTotalPages() }}
                </span>
                <div class="flex gap-2">
                  <app-button
                    variant="secondary"
                    size="sm"
                    icon="chevron-right"
                    [disabled]="paymentsPage() <= 1"
                    (clicked)="goToPaymentsPage(paymentsPage() - 1)"
                  >
                    السابق
                  </app-button>
                  <app-button
                    variant="secondary"
                    size="sm"
                    icon="chevron-left"
                    [disabled]="paymentsPage() >= paymentsTotalPages()"
                    (clicked)="goToPaymentsPage(paymentsPage() + 1)"
                  >
                    التالي
                  </app-button>
                </div>
              </div>
            }
          </app-card>
        }
      }

      <app-employee-dialog
        [open]="employeeOpen()"
        [employee]="editingEmployee()"
        (openChange)="closeEmployeeDialog($event)"
        (saved)="onEmployeeSaved()"
      />

      <app-pay-salary-dialog
        [open]="payOpen()"
        [employee]="payEmployee()"
        (openChange)="closePayDialog($event)"
        (saved)="onPaySaved()"
      />
    </main>
  `,
})
export class HrPage {
  readonly store = inject(HrStore);

  readonly employees = this.store.employees;
  readonly employeesLoading = this.store.employeesLoading;
  readonly employeesError = this.store.employeesError;
  readonly paymentsLoading = this.store.salaryPaymentsLoading;
  readonly paymentsError = this.store.salaryPaymentsError;
  readonly saveError = this.store.saveError;

  readonly activeTab = signal<HrTab>('employees');
  private readonly loadedTabs = new Set<HrTab>(['employees']);

  readonly employeeOpen = signal(false);
  readonly editingEmployee = signal<EmployeeResponse | null>(null);
  readonly payOpen = signal(false);
  readonly payEmployee = signal<EmployeeResponse | null>(null);

  readonly paymentFilterEmployeeName = signal('');
  readonly paymentFilterFrom = signal('');
  readonly paymentFilterTo = signal('');

  private readonly paymentsQuery = signal<{
    page?: number;
    pageSize?: number;
    employeeName?: string;
    fromDate?: string;
    toDate?: string;
  }>({ page: 1, pageSize: PAGE_SIZE });

  readonly pencilIcon = resolveIcon('pencil');
  readonly powerIcon = resolveIcon('power');
  readonly walletIcon = resolveIcon('wallet');

  readonly tabs: ReadonlyArray<{ key: HrTab; label: string; icon: ReturnType<typeof resolveIcon> }> = [
    { key: 'employees', label: 'الموظفون', icon: resolveIcon('users')! },
    { key: 'payments', label: 'سجل الرواتب', icon: resolveIcon('receipt')! },
  ];

  readonly employeeRows = computed(() => this.employees() ?? []);
  readonly paymentRows = computed(() => this.store.salaryPayments()?.items ?? []);
  readonly paymentsTotalCount = computed(() => Number(this.store.salaryPayments()?.totalCount ?? 0));
  readonly paymentsPage = computed(() => Number(this.store.salaryPayments()?.pageNumber ?? 1));
  readonly paymentsTotalPages = computed(() => Number(this.store.salaryPayments()?.totalPages ?? 0));

  private readonly employeeNameCell = viewChild<TemplateRef<{ $implicit: EmployeeResponse }>>('employeeNameCell');
  private readonly employeeSalaryCell = viewChild<TemplateRef<{ $implicit: EmployeeResponse }>>('employeeSalaryCell');
  private readonly employeeUserCell = viewChild<TemplateRef<{ $implicit: EmployeeResponse }>>('employeeUserCell');
  private readonly employeeStatusCell = viewChild<TemplateRef<{ $implicit: EmployeeResponse }>>('employeeStatusCell');
  private readonly employeeActionsCell = viewChild<TemplateRef<{ $implicit: EmployeeResponse }>>('employeeActionsCell');

  readonly employeeColumns = computed<TableColumn<EmployeeResponse>[]>(() => [
    {
      key: 'fullName',
      header: 'الموظف',
      cell: (row) => row.fullName ?? '',
      cellTemplate: this.employeeNameCell(),
      sortable: true,
      sortValue: (row) => row.fullName ?? '',
    },
    {
      key: 'salary',
      header: 'الراتب',
      cell: (row) => `${row.salary ?? ''}`,
      cellTemplate: this.employeeSalaryCell(),
      sortable: true,
      sortValue: (row) => Number(row.salary ?? 0),
    },
    {
      key: 'userEmail',
      header: 'المستخدم',
      cell: (row) => row.userEmail ?? '',
      cellTemplate: this.employeeUserCell(),
    },
    {
      key: 'isActive',
      header: 'الحالة',
      cell: (row) => (row.isActive ? 'نشط' : 'متوقف'),
      cellTemplate: this.employeeStatusCell(),
    },
    {
      key: 'actions',
      header: 'إجراءات',
      cell: () => '',
      align: 'center',
      cellTemplate: this.employeeActionsCell(),
    },
  ]);

  readonly skeletonRows = [0, 1, 2, 3, 4];
  readonly skeletonTableColumns = SKELETON_TABLE_COLUMNS;
  readonly formatDate = formatDate;

  constructor() {
    void this.store.loadEmployees();
  }

  currencyCode(currency: unknown): string {
    return currency === 2 || currency === '2' ? 'USD' : currency === 3 || currency === '3' ? 'ILS' : 'JOD';
  }

  tabClass(key: HrTab): string {
    const base =
      'flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium transition-colors';
    return this.activeTab() === key
      ? `${base} border-gold text-gold`
      : `${base} border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700`;
  }

  setTab(tab: HrTab): void {
    if (this.activeTab() === tab) return;
    this.activeTab.set(tab);
    if (!this.loadedTabs.has(tab)) {
      this.loadedTabs.add(tab);
      if (tab === 'payments') {
        void this.store.loadSalaryPayments(this.paymentsQuery());
      }
    }
  }

  reloadEmployees(): void {
    void this.store.loadEmployees();
  }

  openEdit(row: EmployeeResponse): void {
    this.editingEmployee.set(row);
    this.employeeOpen.set(true);
  }

  closeEmployeeDialog(open: boolean): void {
    this.employeeOpen.set(open);
    if (!open) this.editingEmployee.set(null);
  }

  onEmployeeSaved(): void {
    void this.store.loadEmployees();
  }

  async toggleActive(row: EmployeeResponse): Promise<void> {
    const ok = await this.store.toggleActive(row.id as string);
    if (ok) void this.store.loadEmployees();
  }

  openPay(row: EmployeeResponse): void {
    this.payEmployee.set(row);
    this.payOpen.set(true);
  }

  closePayDialog(open: boolean): void {
    this.payOpen.set(open);
    if (!open) this.payEmployee.set(null);
  }

  onPaySaved(): void {
    void this.store.loadSalaryPayments(this.paymentsQuery());
    void this.store.loadEmployees();
  }

  applyPaymentFilters(): void {
    this.paymentsQuery.set({
      page: 1,
      pageSize: PAGE_SIZE,
      employeeName: this.paymentFilterEmployeeName() || undefined,
      fromDate: this.paymentFilterFrom() || undefined,
      toDate: this.paymentFilterTo() || undefined,
    });
    void this.store.loadSalaryPayments(this.paymentsQuery());
  }

  resetPaymentFilters(): void {
    this.paymentFilterEmployeeName.set('');
    this.paymentFilterFrom.set('');
    this.paymentFilterTo.set('');
    this.applyPaymentFilters();
  }

  goToPaymentsPage(page: number): void {
    this.paymentsQuery.update((current) => ({ ...current, page }));
    void this.store.loadSalaryPayments(this.paymentsQuery());
  }

  refreshPayments(): void {
    void this.store.loadSalaryPayments(this.paymentsQuery());
  }

  saveErrorMessage(error: import('../../core/http/api-error').ApiError): string {
    const first = (() => {
      for (const messages of Object.values(error.validation ?? {})) {
        const m = messages[0];
        if (m !== undefined) return m;
      }
      return null;
    })();
    return first ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }
}
