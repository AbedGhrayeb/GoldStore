import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth-store';

import {
  Badge,
  Button,
  Card,
  EmptyState,
  RetryButton,
  Skeleton,
  resolveIcon,
} from '../../shared/ui';
import { formatDate } from '../../shared/format/formatters';
import type {
  AccountWithBalanceResponse,
  DebtResponse,
  RecentTransactionResponse,
} from './finance-api.service';
import { FinanceStore } from './finance-store';
import { AccountDialog } from './account-dialog';
import { DebtDialog } from './debt-dialog';
import { DebtPaymentDialog } from './debt-payment-dialog';
import { SetBalanceDialog } from './set-balance-dialog';

const PAGE_SIZE = 15;
const SKELETON_TABLE_COLUMNS = [0, 1, 2, 3, 4, 5];

/** Currency display order for the accounts cards (unknown currencies last). */
const CURRENCY_ORDER = ['JOD', 'USD', 'ILS'];

function currencyRank(currency: string | null | undefined): number {
  const index = CURRENCY_ORDER.indexOf(currency ?? '');
  return index === -1 ? CURRENCY_ORDER.length : index;
}

/** Finance page tabs — operations (transactions) first, then debts, then accounts. */
type FinanceTab = 'operations' | 'debts' | 'accounts';

interface AccountTableRow extends AccountWithBalanceResponse {
  balanceText: string;
}

function toAccountRow(account: AccountWithBalanceResponse): AccountTableRow {
  return {
    ...account,
    balanceText: `${Number(account.balance ?? 0).toFixed(3)} ${account.currency ?? ''}`,
  };
}

interface TransactionTableRow extends RecentTransactionResponse {
  amountText: string;
}

/**
 * P3.10 — Finance (`feature: finance`). One page for the whole feature, split into tabs:
 * الحركات المالية (read-only paged financial transactions — first tab), الذمم (debt KPIs +
 * paged debts table with create + pay), and الحسابات المالية (ledger-derived balances with
 * create + set-balance). Each tab loads its data on first activation. Transactions are side
 * effects of other operations, so that section is read-only.
 */
@Component({
  selector: 'app-finance-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AccountDialog,
    Badge,
    Button,
    Card,
    DebtDialog,
    DebtPaymentDialog,
    EmptyState,
    LucideAngularModule,
    RetryButton,
    SetBalanceDialog,
    Skeleton,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">المالية</h1>
          <p class="mt-1 text-sm text-gray-600">
            الحسابات المالية، الذمم (الديون)، وسجل الحركات المالية.
          </p>
        </div>
        <div class="flex flex-wrap gap-2">
          <app-button
            variant="secondary"
            icon="wallet"
            [disabled]="!canManageFinance()"
            [title]="!canManageFinance() ? 'ليس لديك صلاحية' : ''"
            (clicked)="canManageFinance() && accountOpen.set(true)"
          >
            حساب جديد
          </app-button>
          <app-button
            icon="plus"
            [disabled]="!canManageFinance()"
            [title]="!canManageFinance() ? 'ليس لديك صلاحية' : ''"
            (clicked)="canManageFinance() && debtOpen.set(true)"
            >ذمة جديدة</app-button
          >
        </div>
      </div>

      <!-- Tabs: operations → debts → accounts -->
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
        <!-- Operations (financial transactions ledger, read-only) -->
        @case ('operations') {
          <app-card>
            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
              <h2 class="text-sm font-semibold text-gray-700">الحركات المالية</h2>
              <p class="text-xs text-gray-500">
                تُسجل تلقائياً من عمليات البيع والشراء والمصروفات والذمم.
              </p>
            </div>

            <form
              class="mb-4 space-y-4"
              novalidate
              (submit)="applyTxFilters(); $event.preventDefault()"
            >
              <div class="grid grid-cols-1 gap-4 md:grid-cols-3 xl:grid-cols-5">
                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">الحساب</span>
                  <input
                    type="text"
                    [value]="txFilterAccountName()"
                    (input)="onTxSearchInput($any($event.target).value)"
                    placeholder="اسم الحساب..."
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">نوع الحساب</span>
                  <select
                    id="tx-account-type-filter"
                    [value]="txFilterAccountType()"
                    (change)="onTxSelectChange('accountType', $any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  >
                    <option value="">الكل</option>
                    <option value="Cash">نقدي</option>
                    <option value="Bank">مصرفي</option>
                  </select>
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">العملة</span>
                  <select
                    id="tx-currency-filter"
                    [value]="txFilterCurrency()"
                    (change)="onTxSelectChange('currency', $any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  >
                    <option value="">الكل</option>
                    <option value="JOD">JOD</option>
                    <option value="USD">USD</option>
                    <option value="ILS">ILS</option>
                  </select>
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">من تاريخ</span>
                  <input
                    type="date"
                    dir="ltr"
                    [value]="txFilterFrom()"
                    (change)="onTxSelectChange('from', $any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">إلى تاريخ</span>
                  <input
                    type="date"
                    dir="ltr"
                    [value]="txFilterTo()"
                    (change)="onTxSelectChange('to', $any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>
              </div>

              <div class="flex flex-wrap gap-3">
                <app-button type="submit" icon="filter">تصفية</app-button>
                <app-button variant="secondary" type="button" (clicked)="resetTxFilters()">
                  إعادة تعيين
                </app-button>
              </div>
            </form>

            <div class="overflow-x-auto">
              <table class="w-full border-collapse text-sm">
                <thead>
                  <tr class="border-b border-gray-200">
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">الوصف</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">الحساب</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">المبلغ</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">العملة</th>
                  </tr>
                </thead>
                <tbody>
                  @if (txLoading()) {
                    @for (row of skeletonRows; track $index) {
                      <tr class="border-b border-gray-100">
                        @for (column of skeletonTableColumns; track column) {
                          <td class="px-4 py-3"><app-skeleton height="1rem" /></td>
                        }
                      </tr>
                    }
                  } @else if (txError(); as error) {
                    <tr>
                      <td colspan="5" class="px-4 py-10">
                        <app-empty-state
                          icon="alert-circle"
                          title="تعذّر تحميل الحركات"
                          [description]="error.detail ?? ''"
                        >
                          <app-retry-button (retry)="refreshTransactions()" />
                        </app-empty-state>
                      </td>
                    </tr>
                  } @else if (txRows().length === 0) {
                    <tr>
                      <td colspan="5" class="px-4 py-10">
                        <app-empty-state
                          icon="inbox"
                          title="لا توجد حركات مالية"
                          description="جرّب تعديل عوامل التصفية."
                        />
                      </td>
                    </tr>
                  } @else {
                    @for (transaction of txRows(); track transaction.id) {
                      <tr
                        class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15"
                      >
                        <td class="whitespace-nowrap px-4 py-3">
                          {{ formatDate(transaction.date ?? '') }}
                        </td>
                        <td
                          class="max-w-64 truncate px-4 py-3"
                          title="{{ transaction.description ?? '' }}"
                        >
                          {{ transaction.description ?? '—' }}
                        </td>
                        <td class="px-4 py-3 font-medium">{{ transaction.accountName }}</td>
                        <td class="px-4 py-3 data-mono" dir="ltr">
                          <span
                            [class.text-emerald-700]="transaction.transactionType === 'Inflow'"
                            [class.text-red-700]="transaction.transactionType === 'Outflow'"
                          >
                            {{ transaction.amountText }}
                          </span>
                        </td>
                        <td class="px-4 py-3">{{ transaction.currency }}</td>
                      </tr>
                    }
                  }
                </tbody>
              </table>
            </div>

            @if (txTotalPages() > 1) {
              <div class="mt-4 flex items-center justify-between border-t border-gray-100 pt-4">
                <span class="text-xs text-gray-500 data-mono">
                  {{ txTotalCount() }} حركة — صفحة {{ txCurrentPage() }} من {{ txTotalPages() }}
                </span>
                <div class="flex gap-2">
                  <app-button
                    variant="secondary"
                    size="sm"
                    icon="chevron-right"
                    [disabled]="txCurrentPage() <= 1"
                    (clicked)="goToTxPage(txCurrentPage() - 1)"
                  >
                    السابق
                  </app-button>
                  <app-button
                    variant="secondary"
                    size="sm"
                    icon="chevron-left"
                    [disabled]="txCurrentPage() >= txTotalPages()"
                    (clicked)="goToTxPage(txCurrentPage() + 1)"
                  >
                    التالي
                  </app-button>
                </div>
              </div>
            }
          </app-card>
        }

        <!-- Debts -->
        @case ('debts') {
          @if (debtKpis(); as kpis) {
            <section class="grid grid-cols-1 gap-4 md:grid-cols-3">
              <app-card>
                <div class="flex items-center justify-between">
                  <p class="text-xs font-medium text-gray-500">ذمم لنا (مدينة)</p>
                  <span
                    class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50"
                  >
                    <lucide-icon [img]="trendingUpIcon" [size]="18" class="text-gray-700" />
                  </span>
                </div>
                <p class="mt-3 text-2xl font-semibold text-gray-900 data-mono">
                  {{ kpis.totalReceivablesDisplay }}
                </p>
                <p class="mt-2 text-xs text-gray-500" data-mono>{{ kpis.receivableCount }} ذمة</p>
              </app-card>
              <app-card>
                <div class="flex items-center justify-between">
                  <p class="text-xs font-medium text-gray-500">ذمم علينا (دائنة)</p>
                  <span
                    class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50"
                  >
                    <lucide-icon [img]="trendingDownIcon" [size]="18" class="text-gray-700" />
                  </span>
                </div>
                <p class="mt-3 text-2xl font-semibold text-gray-900 data-mono">
                  {{ kpis.totalPayablesDisplay }}
                </p>
                <p class="mt-2 text-xs text-gray-500" data-mono>{{ kpis.payableCount }} ذمة</p>
              </app-card>
              <app-card>
                <div class="flex items-center justify-between">
                  <p class="text-xs font-medium text-gray-500">صافي الذمم</p>
                  <span
                    class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50"
                  >
                    <lucide-icon [img]="walletIcon" [size]="18" class="text-gray-700" />
                  </span>
                </div>
                <p
                  class="mt-3 text-2xl font-semibold data-mono"
                  [class.text-emerald-700]="kpis.isNetPositive"
                  [class.text-red-700]="!kpis.isNetPositive"
                >
                  {{ kpis.netBalanceDisplay }}
                </p>
                <p class="mt-2 text-xs text-gray-500">
                  {{ kpis.isNetPositive ? 'صافي لنا' : 'صافي علينا' }}
                </p>
              </app-card>
            </section>
          }

          <app-card>
            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
              <h2 class="text-sm font-semibold text-gray-700">الذمم (الديون)</h2>
              <app-button
                size="sm"
                icon="plus"
                [disabled]="!canManageFinance()"
                [title]="!canManageFinance() ? 'ليس لديك صلاحية' : ''"
                (clicked)="canManageFinance() && debtOpen.set(true)"
                >ذمة جديدة</app-button
              >
            </div>

            <form
              class="mb-4 space-y-4"
              novalidate
              (submit)="applyDebtFilters(); $event.preventDefault()"
            >
              <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
                <label class="block">
                  <span class="mb-2 block text-sm font-medium text-gray-700">الاتجاه</span>
                  <select
                    id="debt-direction-filter"
                    [value]="debtFilterDirection()"
                    (change)="onDebtDirectionChange($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  >
                    <option value="">الكل</option>
                    <option value="Receivable">ذمة مدينة (لنا)</option>
                    <option value="Payable">ذمة دائنة (علينا)</option>
                  </select>
                </label>

                <label class="block md:col-span-2">
                  <span class="mb-2 block text-sm font-medium text-gray-700">بحث</span>
                  <input
                    type="text"
                    [value]="debtFilterSearch()"
                    (input)="onDebtSearchInput($any($event.target).value)"
                    (keydown)="onDebtSearchKeydown($event)"
                    placeholder="اسم صاحب الذمة أو رقم الهاتف..."
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </label>
              </div>

              <div class="flex flex-wrap gap-3">
                <app-button type="submit" icon="filter">تصفية</app-button>
                <app-button variant="secondary" type="button" (clicked)="resetDebtFilters()">
                  إعادة تعيين
                </app-button>
              </div>
            </form>

            <div class="overflow-x-auto">
              <table class="w-full border-collapse text-sm">
                <thead>
                  <tr class="border-b border-gray-200">
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">صاحب الذمة</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">الهاتف</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">النوع</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">المتبقي</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">التاريخ</th>
                    <th class="px-4 py-3 text-start font-semibold text-gray-600">ملاحظات</th>
                    <th class="px-4 py-3 text-center font-semibold text-gray-600">إجراءات</th>
                  </tr>
                </thead>
                <tbody>
                  @if (debtsLoading()) {
                    @for (row of skeletonRows; track $index) {
                      <tr class="border-b border-gray-100">
                        @for (column of skeletonTableColumns; track column) {
                          <td class="px-4 py-3"><app-skeleton height="1rem" /></td>
                        }
                      </tr>
                    }
                  } @else if (debtsError(); as error) {
                    <tr>
                      <td colspan="7" class="px-4 py-10">
                        <app-empty-state
                          icon="alert-circle"
                          title="تعذّر تحميل الذمم"
                          [description]="error.detail ?? ''"
                        >
                          <app-retry-button (retry)="refreshDebts()" />
                        </app-empty-state>
                      </td>
                    </tr>
                  } @else if (debtRows().length === 0) {
                    <tr>
                      <td colspan="7" class="px-4 py-10">
                        <app-empty-state
                          icon="inbox"
                          title="لا توجد ذمم"
                          description="جرّب تعديل عوامل التصفية أو أنشئ ذمة جديدة."
                        />
                      </td>
                    </tr>
                  } @else {
                    @for (debt of debtRows(); track debt.id) {
                      <tr
                        class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15"
                      >
                        <td class="px-4 py-3 font-medium">{{ debt.name }}</td>
                        <td class="px-4 py-3 data-mono" dir="ltr">{{ debt.phone ?? '—' }}</td>
                        <td class="px-4 py-3">
                          @if (debt.direction === 'Receivable') {
                            <app-badge variant="gold">{{ debt.directionLabel }}</app-badge>
                          } @else {
                            <app-badge variant="warning">{{ debt.directionLabel }}</app-badge>
                          }
                        </td>
                        <td class="px-4 py-3 data-mono" dir="ltr">
                          {{ debt.outstandingBalanceDisplay }} {{ debt.currency }}
                        </td>
                        <td class="px-4 py-3">{{ formatDate(debt.createdAt ?? '') }}</td>
                        <td
                          class="max-w-48 truncate px-4 py-3 text-gray-600"
                          title="{{ debt.notes ?? '' }}"
                        >
                          {{ debt.notes ?? '—' }}
                        </td>
                        <td class="px-4 py-3 text-center">
                          @if (Number(debt.outstandingBalance ?? 0) > 0.0005) {
                            <button
                              type="button"
                              class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
                              title="سداد"
                              [attr.aria-label]="'سداد ذمة ' + (debt.name ?? '')"
                              [disabled]="!canManageFinance()"
                              (click)="canManageFinance() && openPayment(debt)"
                            >
                              <lucide-icon [img]="walletIcon" [size]="16" />
                            </button>
                          }
                        </td>
                      </tr>
                    }
                  }
                </tbody>
              </table>
            </div>

            @if (debtsTotalPages() > 1) {
              <div class="mt-4 flex items-center justify-between border-t border-gray-100 pt-4">
                <span class="text-xs text-gray-500 data-mono">
                  {{ debtsTotalCount() }} ذمة — صفحة {{ debtsCurrentPage() }} من
                  {{ debtsTotalPages() }}
                </span>
                <div class="flex gap-2">
                  <app-button
                    variant="secondary"
                    size="sm"
                    icon="chevron-right"
                    [disabled]="debtsCurrentPage() <= 1"
                    (clicked)="goToDebtsPage(debtsCurrentPage() - 1)"
                  >
                    السابق
                  </app-button>
                  <app-button
                    variant="secondary"
                    size="sm"
                    icon="chevron-left"
                    [disabled]="debtsCurrentPage() >= debtsTotalPages()"
                    (clicked)="goToDebtsPage(debtsCurrentPage() + 1)"
                  >
                    التالي
                  </app-button>
                </div>
              </div>
            }
          </app-card>
        }

        <!-- Accounts -->
        @case ('accounts') {
          <app-card title="الحسابات المالية">
            @if (accountsError(); as error) {
              <app-empty-state
                icon="alert-circle"
                title="تعذّر تحميل الحسابات"
                [description]="error.detail ?? ''"
              >
                <app-retry-button (retry)="reloadAccounts()" />
              </app-empty-state>
            } @else {
              @if (currencyTotals().length > 0) {
                <div
                  class="mb-4 flex flex-wrap gap-x-6 gap-y-1 rounded-input bg-gold-container/25 px-4 py-3"
                >
                  @for (total of currencyTotals(); track total.currency) {
                    <p class="text-sm text-gray-700">
                      إجمالي {{ total.currency }}:
                      <span class="font-semibold data-mono">{{ total.text }}</span>
                    </p>
                  }
                </div>
              }
              @if (accountsLoading() && accountRows().length === 0) {
                <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
                  @for (row of skeletonRows; track $index) {
                    <app-skeleton height="9rem" />
                  }
                </div>
              } @else if (accountRows().length === 0) {
                <app-empty-state
                  icon="wallet"
                  title="لا توجد حسابات"
                  description="أنشئ أول حساب مالي لتسجيل الحركات والذمم عليه."
                />
              } @else {
                <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
                  @for (account of accountRows(); track account.id) {
                    <div class="rounded-lg border border-gray-200 p-4">
                      <div class="flex items-start justify-between gap-2">
                        <p class="flex items-center gap-2 font-semibold text-gray-900">
                          {{ account.name }}
                          @if (!account.isActive) {
                            <app-badge variant="neutral">متوقف</app-badge>
                          }
                        </p>
                        @if (account.accountType === 'Cash') {
                          <app-badge variant="gold">نقدي</app-badge>
                        } @else {
                          <app-badge variant="neutral">مصرفي</app-badge>
                        }
                      </div>
                      <p class="mt-3 text-2xl font-semibold text-gray-900 data-mono" dir="ltr">
                        {{ account.balanceText }}
                      </p>
                      <div class="mt-2 flex items-center justify-between text-xs text-gray-500">
                        <span>{{ account.currency }}</span>
                        <span class="data-mono" dir="ltr">{{ account.accountNumber ?? '—' }}</span>
                      </div>
                      <div class="mt-3 border-t border-gray-100 pt-3">
                        <app-button
                          variant="secondary"
                          size="sm"
                          icon="pencil"
                          [disabled]="!canManageFinance()"
                          [title]="!canManageFinance() ? 'ليس لديك صلاحية' : 'تعديل الرصيد'"
                          (clicked)="canManageFinance() && openSetBalance(account)"
                        >
                          تعديل الرصيد
                        </app-button>
                      </div>
                    </div>
                  }
                </div>
              }
            }
          </app-card>
        }
      }

      <app-account-dialog
        [open]="accountOpen()"
        (openChange)="accountOpen.set(false)"
        (saved)="onSaved()"
      />

      <app-set-balance-dialog
        [open]="setBalanceOpen()"
        [account]="setBalanceAccount()"
        (openChange)="closeSetBalance()"
        (saved)="onSaved()"
      />

      <app-debt-dialog [open]="debtOpen()" (openChange)="debtOpen.set(false)" (saved)="onSaved()" />

      <app-debt-payment-dialog
        [open]="paymentOpen()"
        [debt]="paymentDebt()"
        (openChange)="closePayment()"
        (saved)="onSaved()"
      />
    </main>
  `,
})
export class FinancePage {
  private readonly auth = inject(AuthStore);
  readonly canManageFinance = computed(
    () => this.auth.hasPermission('finance.manage') || this.auth.hasRole('store_admin'),
  );
  readonly store = inject(FinanceStore);

  readonly accounts = this.store.accounts;
  readonly accountsLoading = this.store.accountsLoading;
  readonly accountsError = this.store.accountsError;
  readonly debtKpis = this.store.debtKpis;
  readonly debtsLoading = this.store.debtsLoading;
  readonly debtsError = this.store.debtsError;
  readonly txLoading = this.store.txLoading;
  readonly txError = this.store.txError;

  readonly accountOpen = signal(false);
  readonly setBalanceOpen = signal(false);
  readonly setBalanceAccount = signal<AccountWithBalanceResponse | null>(null);
  readonly debtOpen = signal(false);
  readonly paymentOpen = signal(false);
  readonly paymentDebt = signal<DebtResponse | null>(null);

  readonly activeTab = signal<FinanceTab>('operations');

  readonly walletIcon = resolveIcon('wallet');
  readonly trendingUpIcon = resolveIcon('trending-up');
  readonly trendingDownIcon = resolveIcon('trending-down');
  private readonly receiptIcon = resolveIcon('receipt');
  private readonly coinsIcon = resolveIcon('coins');

  /** Tab bar definition — operations first per the finance workflow. */
  readonly tabs: readonly {
    key: FinanceTab;
    label: string;
    icon: ReturnType<typeof resolveIcon>;
  }[] = [
    { key: 'operations', label: 'الحركات المالية', icon: this.receiptIcon },
    { key: 'debts', label: 'الذمم', icon: this.coinsIcon },
    { key: 'accounts', label: 'الحسابات', icon: this.walletIcon },
  ];

  readonly debtFilterDirection = signal('');
  readonly debtFilterSearch = signal('');

  readonly txFilterAccountName = signal('');
  readonly txFilterAccountType = signal('');
  readonly txFilterCurrency = signal('');
  readonly txFilterFrom = signal('');
  readonly txFilterTo = signal('');

  private readonly debtsQuery = signal<{
    page?: number;
    pageSize?: number;
    direction?: string;
    search?: string;
  }>({ page: 1, pageSize: PAGE_SIZE });
  private readonly txQuery = signal<{
    page?: number;
    pageSize?: number;
    accountName?: string;
    fromDate?: string;
    toDate?: string;
    currency?: string;
    accountType?: string;
  }>({ page: 1, pageSize: PAGE_SIZE });

  /** Accounts ordered for cards: cash accounts first, then by currency (JOD/USD/ILS), then name. */
  readonly accountRows = computed(() =>
    (this.accounts() ?? []).map(toAccountRow).sort((a, b) => {
      const aCash = a.accountType === 'Cash' ? 0 : 1;
      const bCash = b.accountType === 'Cash' ? 0 : 1;
      if (aCash !== bCash) {
        return aCash - bCash;
      }
      const currencyCompare = currencyRank(a.currency) - currencyRank(b.currency);
      if (currencyCompare !== 0) {
        return currencyCompare;
      }
      return (a.name ?? '').localeCompare(b.name ?? '', 'ar');
    }),
  );

  /** Per-currency totals across all accounts — mirrors the ledger-sum invariant visually. */
  readonly currencyTotals = computed(() => {
    const totals = new Map<string, number>();
    for (const account of this.accounts() ?? []) {
      const current = totals.get(account.currency ?? '') ?? 0;
      totals.set(account.currency ?? '', current + Number(account.balance ?? 0));
    }
    return [...totals.entries()]
      .filter(([currency]) => currency !== '')
      .map(([currency, amount]) => ({
        currency,
        text: `${amount.toFixed(3)} ${currency}`,
      }));
  });

  readonly debtRows = computed(() => this.store.debtsPage()?.items ?? []);
  readonly debtsTotalCount = computed(() => Number(this.store.debtsPage()?.totalCount ?? 0));
  readonly debtsCurrentPage = computed(() => Number(this.store.debtsPage()?.pageNumber ?? 1));
  readonly debtsTotalPages = computed(() => Number(this.store.debtsPage()?.totalPages ?? 0));

  readonly txRows = computed<TransactionTableRow[]>(() =>
    (this.store.txPage()?.items ?? []).map((transaction) => ({
      ...transaction,
      amountText:
        transaction.transactionType === 'Inflow'
          ? `+${Number(transaction.amount ?? 0).toFixed(3)}`
          : `−${Number(transaction.amount ?? 0).toFixed(3)}`,
    })),
  );
  readonly txTotalCount = computed(() => Number(this.store.txPage()?.totalCount ?? 0));
  readonly txCurrentPage = computed(() => Number(this.store.txPage()?.pageNumber ?? 1));
  readonly txTotalPages = computed(() => Number(this.store.txPage()?.totalPages ?? 0));

  readonly skeletonRows = [0, 1, 2, 3, 4];
  readonly skeletonTableColumns = SKELETON_TABLE_COLUMNS;

  readonly formatDate = formatDate;
  readonly Number = Number;

  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // Fresh data on every navigation — the component is recreated per visit.
    void this.store.loadTransactions(this.txQuery());
    void this.store.loadAccounts();
    void this.store.loadDebtKpis();
    void this.store.loadDebts(this.debtsQuery());
  }

  tabClass(key: FinanceTab): string {
    const base =
      'flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium transition-colors';
    return this.activeTab() === key
      ? `${base} border-gold text-gold`
      : `${base} border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700`;
  }

  setTab(tab: FinanceTab): void {
    if (this.activeTab() === tab) {
      return;
    }
    this.activeTab.set(tab);
    this.loadForTab(tab);
  }

  /** Reloads a tab's data on every activation so switching tabs always shows fresh data. */
  private loadForTab(tab: FinanceTab): void {
    if (tab === 'accounts') {
      void this.store.loadAccounts();
    } else if (tab === 'debts') {
      void this.store.loadDebtKpis();
      void this.store.loadDebts(this.debtsQuery());
    } else {
      void this.store.loadTransactions(this.txQuery());
    }
  }

  reloadAccounts(): void {
    void this.store.loadAccounts();
  }

  openSetBalance(account: AccountWithBalanceResponse): void {
    this.setBalanceAccount.set(account);
    this.setBalanceOpen.set(true);
  }

  closeSetBalance(): void {
    this.setBalanceOpen.set(false);
    this.setBalanceAccount.set(null);
  }

  openPayment(debt: DebtResponse): void {
    this.paymentDebt.set(debt);
    this.paymentOpen.set(true);
  }

  closePayment(): void {
    this.paymentOpen.set(false);
    this.paymentDebt.set(null);
  }

  /** Any successful mutation changes balances/debts/transactions — close dialogs, refresh everything. */
  onSaved(): void {
    this.accountOpen.set(false);
    this.debtOpen.set(false);
    this.closeSetBalance();
    this.closePayment();
    void this.store.loadAccounts();
    void this.store.loadDebtKpis();
    void this.store.loadDebts(this.debtsQuery());
    void this.store.loadTransactions(this.txQuery());
  }

  /** Runs a filter apply after a short pause so typing in search boxes refetches live. */
  private scheduleDebouncedApply(apply: () => void): void {
    if (this.searchDebounce !== null) {
      clearTimeout(this.searchDebounce);
    }
    this.searchDebounce = setTimeout(apply, 400);
  }

  onDebtSearchInput(value: string): void {
    this.debtFilterSearch.set(value);
    this.scheduleDebouncedApply(() => this.applyDebtFilters());
  }

  onDebtDirectionChange(value: string): void {
    this.debtFilterDirection.set(value);
    this.applyDebtFilters();
  }

  onTxSearchInput(value: string): void {
    this.txFilterAccountName.set(value);
    this.scheduleDebouncedApply(() => this.applyTxFilters());
  }

  onTxSelectChange(field: 'accountType' | 'currency' | 'from' | 'to', value: string): void {
    if (field === 'accountType') {
      this.txFilterAccountType.set(value);
    } else if (field === 'currency') {
      this.txFilterCurrency.set(value);
    } else if (field === 'from') {
      this.txFilterFrom.set(value);
    } else {
      this.txFilterTo.set(value);
    }
    this.applyTxFilters();
  }

  applyDebtFilters(): void {
    this.debtsQuery.set({
      page: 1,
      pageSize: PAGE_SIZE,
      direction: this.debtFilterDirection() || undefined,
      search: this.debtFilterSearch() || undefined,
    });
    void this.store.loadDebts(this.debtsQuery());
  }

  resetDebtFilters(): void {
    this.debtFilterDirection.set('');
    this.debtFilterSearch.set('');
    this.applyDebtFilters();
  }

  goToDebtsPage(page: number): void {
    this.debtsQuery.update((current) => ({ ...current, page }));
    void this.store.loadDebts(this.debtsQuery());
  }

  refreshDebts(): void {
    void this.store.loadDebtKpis();
    void this.store.loadDebts(this.debtsQuery());
  }

  onDebtSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.applyDebtFilters();
    }
  }

  applyTxFilters(): void {
    this.txQuery.set({
      page: 1,
      pageSize: PAGE_SIZE,
      accountName: this.txFilterAccountName() || undefined,
      fromDate: this.txFilterFrom() || undefined,
      toDate: this.txFilterTo() || undefined,
      currency: this.txFilterCurrency() || undefined,
      accountType: this.txFilterAccountType() || undefined,
    });
    void this.store.loadTransactions(this.txQuery());
  }

  resetTxFilters(): void {
    this.txFilterAccountName.set('');
    this.txFilterAccountType.set('');
    this.txFilterCurrency.set('');
    this.txFilterFrom.set('');
    this.txFilterTo.set('');
    this.applyTxFilters();
  }

  goToTxPage(page: number): void {
    this.txQuery.update((current) => ({ ...current, page }));
    void this.store.loadTransactions(this.txQuery());
  }

  refreshTransactions(): void {
    void this.store.loadTransactions(this.txQuery());
  }
}
