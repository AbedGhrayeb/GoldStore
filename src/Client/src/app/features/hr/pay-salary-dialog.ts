import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';

import type { ApiError } from '../../core/http/api-error';
import { Button, Dialog } from '../../shared/ui';
import { HrStore } from './hr-store';
import type { EmployeeResponse } from './hr-api.service';

function firstValidationMessage(error: ApiError): string | null {
  for (const messages of Object.values(error.validation ?? {})) {
    const message = messages[0];
    if (message !== undefined) return message;
  }
  return null;
}

function toDateInput(date: Date): string {
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 10);
}

/**
 * P3.12 — pay salary for an employee. Shows the period summary for the selected
 * `paymentDate` (scheduled date, net, alreadyPaid, remaining) and posts
 * `PaySalaryRequest` (`accountId`, `amount`, `paymentDate` YYYY-MM-DD, `notes`) to
 * `POST /employees/{id}/pay-salary`. The employee currency must match the account
 * currency; the server creates a financial OUT ledger entry. No `tenantId` ever sent.
 */
@Component({
  selector: 'app-pay-salary-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog],
  template: `
    <app-dialog
      [open]="open()"
      title="دفع راتب"
      [subtitle]="employee()?.fullName ?? ''"
      icon="wallet"
      (openChange)="onDismiss()"
    >
      <div class="space-y-5">
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <label class="block">
            <span class="mb-2 block text-sm font-medium text-gray-700">تاريخ الدفع</span>
            <input
              id="pay-date"
              type="date"
              dir="ltr"
              [value]="paymentDate()"
              (change)="onDateChange($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </label>
          <label class="block">
            <span class="mb-2 block text-sm font-medium text-gray-700">المبلغ</span>
            <input
              id="pay-amount"
              type="number"
              dir="ltr"
              step="0.01"
              min="0"
              [value]="amount()"
              (input)="amount.set($any($event.target).value)"
              placeholder="0.00"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (invalidAmount()) {
              <p class="mt-1 text-xs text-error">المبلغ يجب أن يكون أكبر من صفر.</p>
            }
            @if (exceedsRemaining()) {
              <p class="mt-1 text-xs text-error">
                المبلغ يتجاوز المتبقي ({{ periodSummary()?.remaining }}).
              </p>
            }
          </label>
        </div>

        <!-- Period summary (net/remaining) -->
        @if (periodSummaryLoading()) {
          <p class="text-xs text-gray-500">جاري حساب ملخص الفترة...</p>
        } @else if (periodSummaryError(); as err) {
          <p class="rounded-input bg-error/10 px-3 py-2 text-xs text-red-700">
            {{ err.detail ?? err.title ?? 'تعذّر تحميل ملخص الفترة' }}
          </p>
        } @else if (periodSummary(); as summary) {
          <div class="rounded-input bg-gold-container/20 px-4 py-3 text-xs">
            <div class="grid grid-cols-2 gap-2 md:grid-cols-4">
              <span class="text-gray-600">المستحق:</span>
              <span class="font-semibold data-mono">{{ summary.netAmount }} {{ employeeCurrencyCode() }}</span>
              <span class="text-gray-600">المدفوع:</span>
              <span class="font-semibold data-mono">{{ summary.alreadyPaid }} {{ employeeCurrencyCode() }}</span>
              <span class="text-gray-600">المتبقي:</span>
              <span class="font-semibold data-mono text-gold">{{ summary.remaining }} {{ employeeCurrencyCode() }}</span>
              <span class="text-gray-600">الخصم:</span>
              <span class="font-semibold data-mono">{{ summary.discountAmount }} {{ employeeCurrencyCode() }}</span>
            </div>
            <p class="mt-2 text-gray-500">
              الفترة: {{ summary.scheduledDate }} — الدورة: {{ summary.salaryCycleName }}
              @if (summary.isFullyPaid) {
                <span class="ms-2 inline-flex rounded bg-success/15 px-2 py-0.5 text-success">مكتملة</span>
              }
            </p>
          </div>
        }

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="pay-account"
            >الحساب المالي <span class="text-error">*</span></label
          >
          @if (accountsError()) {
            <p class="mb-2 rounded-input bg-warning/10 px-3 py-2 text-xs text-amber-800">
              {{ accountsError() }}
            </p>
          }
          <select
            id="pay-account"
            [value]="accountId()"
            (change)="accountId.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">اختر الحساب</option>
            @for (account of eligibleAccounts(); track account.id) {
              <option [value]="account.id">
                {{ account.name }} — {{ account.currency }}{{ account.accountNumber ? ' — ' + account.accountNumber : '' }}
              </option>
            }
          </select>
          @if (noEligibleAccount()) {
            <p class="mt-1 text-xs text-amber-700">لا يوجد حساب نشط بعملة الموظف ({{ employeeCurrencyCode() }}).</p>
          }
          @if (missingAccount()) {
            <p class="mt-1 text-xs text-error">اختر حساب الدفع.</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="pay-notes">ملاحظات</label>
          <textarea
            id="pay-notes"
            rows="2"
            [value]="notes()"
            (input)="notes.set($any($event.target).value)"
            placeholder="ملاحظات (اختياري)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
          ></textarea>
        </div>

        @if (saveError(); as error) {
          <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ errorMessage(error) }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button variant="secondary" type="button" [disabled]="saving()" (clicked)="onDismiss()">
            إلغاء
          </app-button>
          <app-button
            type="button"
            [loading]="saving()"
            [disabled]="invalidAmount() || missingAccount() || exceedsRemaining() || (periodSummary()?.isFullyPaid ?? false)"
            (clicked)="onSubmit()"
          >
            تأكيد الدفع
          </app-button>
        </div>
      </div>
    </app-dialog>
  `,
})
export class PaySalaryDialog {
  private readonly store = inject(HrStore);

  readonly open = input(false);
  readonly employee = input<EmployeeResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;
  readonly periodSummary = this.store.periodSummary;
  readonly periodSummaryLoading = this.store.periodSummaryLoading;
  readonly periodSummaryError = this.store.periodSummaryError;
  readonly accountsError = this.store.accountsError;

  readonly paymentDate = signal(toDateInput(new Date()));
  readonly amount = signal('');
  readonly accountId = signal('');
  readonly notes = signal('');

  readonly invalidAmount = computed(() => Number(this.amount()) <= 0);
  readonly missingAccount = computed(() => this.accountId() === '');
  readonly exceedsRemaining = computed(() => {
    const remaining = Number(this.periodSummary()?.remaining ?? Infinity);
    return Number(this.amount()) > remaining;
  });

  readonly employeeCurrencyCode = computed(() => {
    const currency = this.employee()?.currency as number | undefined;
    return currency === 2 ? 'USD' : currency === 3 ? 'ILS' : 'JOD';
  });

  readonly eligibleAccounts = computed(() => {
    const code = this.employeeCurrencyCode();
    // Finance account currency comes as string code (JOD/USD/ILS) per schema.
    return (this.store.accounts() ?? []).filter(
      (account) => (account.currency as unknown as string) === code && account.isActive,
    );
  });

  readonly noEligibleAccount = computed(() => this.eligibleAccounts().length === 0);

  constructor() {
    effect(() => {
      if (this.open() && this.employee()) {
        this.paymentDate.set(toDateInput(new Date()));
        this.amount.set('');
        this.accountId.set('');
        this.notes.set('');
        this.store.clearSaveError();
        void this.store.ensureAccounts();
        this.fetchSummary();
      }
      if (!this.open()) {
        this.store.clearPeriodSummary();
      }
    });
  }

  onDateChange(value: string): void {
    this.paymentDate.set(value);
    this.fetchSummary();
  }

  private fetchSummary(): void {
    const employee = this.employee();
    if (!employee) return;
    void this.store.loadPeriodSummary(employee.id as string, this.paymentDate());
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  async onSubmit(): Promise<void> {
    const employee = this.employee();
    if (!employee || this.invalidAmount() || this.missingAccount()) return;
    const ok = await this.store.paySalary(employee.id as string, {
      accountId: this.accountId(),
      amount: Number(this.amount()),
      paymentDate: this.paymentDate(),
      notes: this.notes().trim() || null,
    });
    if (ok) {
      this.saved.emit();
      this.onDismiss();
    }
  }
}
