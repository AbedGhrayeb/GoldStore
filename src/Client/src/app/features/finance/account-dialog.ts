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

/**
 * P3.10 — create a financial account. The server always creates accounts as Bank-type
 * (auto-numbering the account when omitted) and posts a positive opening balance as a
 * ManualAdjustment inflow, so only name/currency/number/notes/opening-balance are collected.
 * Server errors (duplicate name+currency, validation) render inline.
 */
@Component({
  selector: 'app-account-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      title="حساب مالي جديد"
      subtitle="المالية"
      icon="wallet"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="account-name"
            >اسم الحساب <span class="text-error">*</span></label
          >
          <input
            id="account-name"
            type="text"
            autocomplete="off"
            placeholder="مثال: الصندوق الرئيسي"
            [formField]="draftForm.name"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (draftForm.name().touched() && draftForm.name().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ draftForm.name().errors()[0].message }}</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700">عملة الحساب</label>
          <div class="flex gap-2">
            @for (option of currencyOptions(); track option.code) {
              <label class="flex-1 cursor-pointer">
                <input
                  class="peer sr-only"
                  type="radio"
                  name="account-currency"
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

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="account-number"
            >رقم الحساب / IBAN</label
          >
          <input
            id="account-number"
            type="text"
            dir="ltr"
            autocomplete="off"
            [formField]="draftForm.accountNumber"
            placeholder="يُولَّد تلقائياً إذا تُرك فارغاً"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="account-opening"
            >الرصيد الافتتاحي</label
          >
          <input
            id="account-opening"
            type="number"
            dir="ltr"
            step="0.001"
            min="0"
            [value]="openingBalance()"
            (input)="openingBalance.set($any($event.target).value)"
            placeholder="0.000"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          <p class="mt-1 text-xs text-gray-500">
            الرصيد الافتتاحي يُسجل كحركة تعديل يدوي (وارد) على الحساب.
          </p>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="account-notes"
            >ملاحظات</label
          >
          <textarea
            id="account-notes"
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
          <app-button type="submit" [loading]="saving()">إنشاء الحساب</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class AccountDialog {
  private readonly store = inject(FinanceStore);
  private readonly reference = inject(ReferenceStore);

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

  readonly currency = signal('JOD');
  readonly openingBalance = signal('');
  readonly draft = signal({ name: '', accountNumber: '', notes: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.name, { message: 'اسم الحساب مطلوب.' });
  });

  constructor() {
    void this.reference.ensureLoaded();

    effect(() => {
      if (this.open()) {
        this.draft.set({ name: '', accountNumber: '', notes: '' });
        this.openingBalance.set('');
        this.currency.set('JOD');
        this.store.clearSaveError();
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
      const draft = this.draft();
      const opening = Number(this.openingBalance());
      const ok = await this.store.createAccount({
        name: draft.name.trim(),
        currency: this.currency(),
        accountNumber: draft.accountNumber.trim() || null,
        notes: draft.notes.trim() || null,
        openingBalance: Number.isFinite(opening) && opening > 0 ? opening : 0,
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }
}
