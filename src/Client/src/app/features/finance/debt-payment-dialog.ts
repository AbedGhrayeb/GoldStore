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
import { formatCurrency } from '../../shared/format/formatters';
import { Button, Dialog } from '../../shared/ui';
import type { AccountWithBalanceResponse, DebtResponse } from './finance-api.service';
import { FinanceStore } from './finance-store';

/** First validation message from the server's RFC 9457 `errors` object, if any. */
function firstValidationMessage(error: ApiError): string | null {
  for (const messages of Object.values(error.validation ?? {})) {
    const message = messages[0];
    if (message !== undefined) {
      return message;
    }
  }
  return null;
}

function toLocalDateInput(value: Date): string {
  const offset = value.getTimezoneOffset();
  const local = new Date(value.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 10);
}

/**
 * P3.10 — pay down a debt. The account must match the debt's currency (server returns
 * 409 otherwise) and the amount cannot exceed the outstanding balance; both are guarded
 * client-side and re-enforced server-side against the ledger. The flow mirrors creation:
 * paying a receivable brings money IN, paying a payable takes it OUT.
 */
@Component({
  selector: 'app-debt-payment-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog],
  template: `
    <app-dialog
      [open]="open()"
      title="سداد ذمة"
      subtitle="{{ debt()?.name ?? '' }}"
      icon="wallet"
      (openChange)="onDismiss()"
    >
      <div class="space-y-5">
        <div class="rounded-input bg-gold-container/40 px-4 py-3">
          <div class="flex items-center justify-between text-sm text-gray-700">
            <span>{{ debt()?.directionLabel ?? '' }}</span>
            <span class="font-semibold" data-mono>{{ outstandingText() }}</span>
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="payment-amount"
            >مبلغ السداد <span class="text-error">*</span></label
          >
          <input
            id="payment-amount"
            type="number"
            dir="ltr"
            step="0.001"
            min="0"
            [value]="amount()"
            (input)="amount.set($any($event.target).value)"
            placeholder="0.000"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (invalidAmount()) {
            <p class="mt-1 text-xs text-error">
              أدخل مبلغاً بين 0 و{{ outstanding().toFixed(3) }}.
            </p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="payment-account"
            >الحساب المالي <span class="text-error">*</span></label
          >
          @if (noAccountsForCurrency()) {
            <p class="mb-2 rounded-input bg-warning/10 px-3 py-2 text-xs text-amber-800">
              لا يوجد حساب نشط بعملة {{ debt()?.currency ?? '' }}.
            </p>
          }
          <select
            id="payment-account"
            [value]="accountId()"
            (change)="accountId.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">اختر الحساب</option>
            @for (account of eligibleAccounts(); track account.id) {
              <option [value]="account.id">{{ account.name }}</option>
            }
          </select>
          @if (missingAccount()) {
            <p class="mt-1 text-xs text-error">اختر الحساب المالي.</p>
          }
        </div>

        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="payment-date"
              >التاريخ</label
            >
            <input
              id="payment-date"
              type="date"
              dir="ltr"
              [value]="date()"
              (change)="date.set($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="payment-notes"
              >ملاحظات</label
            >
            <input
              id="payment-notes"
              type="text"
              autocomplete="off"
              [value]="notes()"
              (input)="notes.set($any($event.target).value)"
              placeholder="اختياري"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
        </div>

        @if (saveError(); as error) {
          <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ errorMessage(error) }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button
            variant="secondary"
            type="button"
            [disabled]="saving()"
            (clicked)="onDismiss()"
          >
            إلغاء
          </app-button>
          <app-button
            type="button"
            [loading]="saving()"
            [disabled]="invalidAmount() || missingAccount()"
            (clicked)="onSubmit()"
          >
            تسجيل السداد
          </app-button>
        </div>
      </div>
    </app-dialog>
  `,
})
export class DebtPaymentDialog {
  private readonly store = inject(FinanceStore);

  readonly open = input(false);
  readonly debt = input<DebtResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly amount = signal('');
  readonly accountId = signal('');
  readonly date = signal(toLocalDateInput(new Date()));
  readonly notes = signal('');

  readonly outstanding = computed(() => Number(this.debt()?.outstandingBalance ?? 0));

  readonly outstandingText = computed(() =>
    formatCurrency(this.outstanding(), this.debt()?.currency ?? 'JOD'),
  );

  readonly invalidAmount = computed(() => {
    const value = Number(this.amount());
    return !Number.isFinite(value) || value <= 0 || value > this.outstanding() + 0.0005;
  });

  readonly missingAccount = computed(() => this.accountId() === '');

  /** Active accounts matching the debt's currency — the server rejects any mismatch. */
  readonly eligibleAccounts = computed<AccountWithBalanceResponse[]>(() =>
    (this.store.accounts() ?? []).filter(
      (account) => account.currency === this.debt()?.currency && account.isActive,
    ),
  );

  readonly noAccountsForCurrency = computed(
    () => this.debt() !== null && this.eligibleAccounts().length === 0,
  );

  constructor() {
    effect(() => {
      if (this.open()) {
        this.amount.set('');
        this.accountId.set('');
        this.date.set(toLocalDateInput(new Date()));
        this.notes.set('');
        this.store.clearSaveError();
        if (!this.store.accounts()) {
          void this.store.loadAccounts();
        }
      }
    });
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  onSubmit(): void {
    const debt = this.debt();
    if (!debt?.id || this.invalidAmount() || this.missingAccount()) {
      return;
    }
    void (async () => {
      const ok = await this.store.payDebt(debt.id!, {
        accountId: this.accountId(),
        amount: Number(this.amount()),
        date: new Date(this.date()).toISOString(),
        notes: this.notes().trim() || null,
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    })();
  }
}
