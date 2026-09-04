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
import { FormField, form, required, submit } from '@angular/forms/signals';

import type { ApiError } from '../../core/http/api-error';
import { ReferenceStore, type CurrencyReference } from '../../core/reference/reference-store';
import { Button, Dialog } from '../../shared/ui';
import type { AccountWithBalanceResponse } from './finance-api.service';
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
 * P3.10 — create a debt (ذمة). Direction decides the financial flow exactly like the MVC
 * builder: a receivable (لنا) takes the amount OUT of the selected same-currency account,
 * while a payable (علينا) brings it IN. Accounts are filtered to the chosen currency and
 * active ones only; server errors (currency mismatch, insufficient balance) render inline.
 */
@Component({
  selector: 'app-debt-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      title="ذمة جديدة"
      subtitle="المالية"
      icon="coins"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="debt-name"
            >اسم صاحب الذمة <span class="text-error">*</span></label
          >
          <input
            id="debt-name"
            type="text"
            autocomplete="off"
            placeholder="اسم الشخص أو الجهة"
            [formField]="draftForm.name"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (draftForm.name().touched() && draftForm.name().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ draftForm.name().errors()[0].message }}</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700">نوع الذمة</label>
          <div class="flex gap-2">
            <label class="flex-1 cursor-pointer">
              <input
                class="peer sr-only"
                type="radio"
                name="debt-direction"
                [value]="RECEIVABLE"
                [checked]="direction() === RECEIVABLE"
                (change)="direction.set(RECEIVABLE)"
              />
              <span
                class="block rounded-input border border-gray-300 py-2 text-center text-sm transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60 peer-checked:ring-1 peer-checked:ring-gold"
                >ذمة مدينة (لنا)</span
              >
            </label>
            <label class="flex-1 cursor-pointer">
              <input
                class="peer sr-only"
                type="radio"
                name="debt-direction"
                [value]="PAYABLE"
                [checked]="direction() === PAYABLE"
                (change)="direction.set(PAYABLE)"
              />
              <span
                class="block rounded-input border border-gray-300 py-2 text-center text-sm transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60 peer-checked:ring-1 peer-checked:ring-gold"
                >ذمة دائنة (علينا)</span
              >
            </label>
          </div>
          <p class="mt-1 text-xs text-gray-500">{{ directionHint() }}</p>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700">العملة</label>
          <div class="flex gap-2">
            @for (option of currencyOptions(); track option.code) {
              <label class="flex-1 cursor-pointer">
                <input
                  class="peer sr-only"
                  type="radio"
                  name="debt-currency"
                  [value]="option.code"
                  [checked]="currency() === option.code"
                  (change)="currency.set(option.code)"
                />
                <span
                  class="block rounded-input border border-gray-300 py-2 text-center text-sm transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60 peer-checked:ring-1 peer-checked:ring-gold"
                  >{{ option.code }}</span
                >
              </label>
            }
          </div>
        </div>

        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="debt-amount"
              >المبلغ <span class="text-error">*</span></label
            >
            <input
              id="debt-amount"
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
              <p class="mt-1 text-xs text-error">أدخل مبلغاً أكبر من صفر.</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="debt-date"
              >التاريخ</label
            >
            <input
              id="debt-date"
              type="date"
              dir="ltr"
              [value]="date()"
              (change)="date.set($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="debt-account"
            >الحساب المالي <span class="text-error">*</span></label
          >
          @if (noAccountsForCurrency()) {
            <p class="mb-2 rounded-input bg-warning/10 px-3 py-2 text-xs text-amber-800">
              لا يوجد حساب نشط بعملة {{ currency() }} — أنشئ حساباً أولاً.
            </p>
          }
          <select
            id="debt-account"
            [value]="accountId()"
            (change)="accountId.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">اختر الحساب</option>
            @for (account of eligibleAccounts(); track account.id) {
              <option [value]="account.id">
                {{ account.name }}{{ account.accountNumber ? ' — ' + account.accountNumber : '' }}
              </option>
            }
          </select>
          @if (missingAccount()) {
            <p class="mt-1 text-xs text-error">اختر الحساب المالي.</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="debt-notes"
            >ملاحظات</label
          >
          <textarea
            id="debt-notes"
            rows="2"
            [formField]="draftForm.notes"
            placeholder="ملاحظات إضافية (اختياري)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          ></textarea>
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
            type="submit"
            [loading]="saving()"
            [disabled]="invalidAmount() || missingAccount()"
          >
            إنشاء الذمة
          </app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class DebtDialog {
  private readonly store = inject(FinanceStore);
  private readonly reference = inject(ReferenceStore);

  private static readonly RECEIVABLE = 1;
  private static readonly PAYABLE = 2;

  /** Exposed for the template's radio bindings (`DebtDirection` enum values). */
  protected readonly RECEIVABLE = DebtDialog.RECEIVABLE;
  protected readonly PAYABLE = DebtDialog.PAYABLE;

  readonly open = input(false);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly currencyOptions = computed(() =>
    this.reference.currencies().map((option: CurrencyReference) => ({
      code: option.code,
      symbol: option.symbol,
    })),
  );

  readonly direction = signal(DebtDialog.RECEIVABLE);
  readonly currency = signal('JOD');
  readonly amount = signal('');
  readonly accountId = signal('');
  readonly date = signal(toLocalDateInput(new Date()));
  readonly draft = signal({ name: '', notes: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.name, { message: 'اسم صاحب الذمة مطلوب.' });
  });

  readonly invalidAmount = computed(() => Number(this.amount()) <= 0);
  readonly missingAccount = computed(() => this.accountId() === '');

  /** Active accounts matching the selected currency — the server rejects any mismatch. */
  readonly eligibleAccounts = computed<AccountWithBalanceResponse[]>(() =>
    (this.store.accounts() ?? []).filter(
      (account) => account.currency === this.currency() && account.isActive,
    ),
  );

  readonly noAccountsForCurrency = computed(
    () => this.eligibleAccounts().length === 0,
  );

  readonly directionHint = computed(() =>
    this.direction() === DebtDialog.RECEIVABLE
      ? 'إنشاء الذمة يُخرج المبلغ من الحساب المحدد، والسداد اللاحق يُعيده إليه.'
      : 'إنشاء الذمة يُدخل المبلغ في الحساب المحدد، والسداد اللاحق يُخرجه منه.',
  );

  constructor() {
    void this.reference.ensureLoaded();

    effect(() => {
      if (this.open()) {
        this.draft.set({ name: '', notes: '' });
        this.direction.set(DebtDialog.RECEIVABLE);
        this.currency.set('JOD');
        this.amount.set('');
        this.accountId.set('');
        this.date.set(toLocalDateInput(new Date()));
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
    submit(this.draftForm, async () => {
      if (this.invalidAmount() || this.missingAccount()) {
        return;
      }
      const ok = await this.store.createDebt({
        name: this.draft().name.trim(),
        phone: null,
        direction: this.direction(),
        currency: this.currency(),
        accountId: this.accountId(),
        amount: Number(this.amount()),
        notes: this.draft().notes.trim() || null,
        date: new Date(this.date()).toISOString(),
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }
}
