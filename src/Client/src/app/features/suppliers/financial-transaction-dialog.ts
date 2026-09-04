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

import { ReferenceStore } from '../../core/reference/reference-store';
import type { ApiError } from '../../core/http/api-error';
import { Button, Dialog } from '../../shared/ui';
import type { FinancialDirection, SupplierResponse } from './suppliers-api.service';
import { SuppliersStore } from './suppliers-store';

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

/** `yyyy-mm-dd` for the date input, local time. */
function todayInputValue(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/** Converts a `yyyy-mm-dd` input value to a UTC date-time string (local midnight). */
function toIsoDateTime(dateInput: string): string {
  return new Date(`${dateInput}T00:00:00`).toISOString();
}

/**
 * P3.6 — create a supplier financial transaction (سلفة مورد). Direction: 1 = له (سلفة من
 * مورد، inflow), 2 = لنا (سلفة لمورد، outflow). The server validates the account currency
 * and the available balance for outflows. Account options are best-effort
 * (`feature: finance` gate) — the form degrades with a notice when unavailable.
 */
@Component({
  selector: 'app-financial-transaction-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      title="معاملة مالية جديدة"
      subtitle="الموردون — السلف"
      icon="wallet"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="tx-supplier"
            >المورد</label
          >
          <select
            id="tx-supplier"
            [formField]="draftForm.supplierId"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">اختر مورداً</option>
            @for (supplier of activeSuppliers(); track supplier.id) {
              <option [value]="supplier.id">{{ supplier.name }}</option>
            }
          </select>
          @if (draftForm.supplierId().touched() && draftForm.supplierId().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ draftForm.supplierId().errors()[0].message }}</p>
          }
        </div>

        <div>
          <p class="mb-2 text-sm font-medium text-gray-700">اتجاه المعاملة</p>
          <div class="flex gap-3">
            <label class="cursor-pointer flex-1">
              <input
                class="peer sr-only"
                type="radio"
                name="tx-direction"
                [value]="1"
                [checked]="direction() === 1"
                (change)="direction.set(1)"
              />
              <span
                class="flex items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-3 py-2.5 text-sm transition peer-checked:border-gold peer-checked:bg-gold-container/30"
              >
                له (سلفة من مورد)
              </span>
            </label>
            <label class="cursor-pointer flex-1">
              <input
                class="peer sr-only"
                type="radio"
                name="tx-direction"
                [value]="2"
                [checked]="direction() === 2"
                (change)="direction.set(2)"
              />
              <span
                class="flex items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-3 py-2.5 text-sm transition peer-checked:border-gold peer-checked:bg-gold-container/30"
              >
                لنا (سلفة لمورد)
              </span>
            </label>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="tx-amount"
              >المبلغ</label
            >
            <input
              id="tx-amount"
              type="number"
              dir="ltr"
              step="0.001"
              [formField]="draftForm.amount"
              placeholder="0.000"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (amountError()) {
              <p class="mt-1 text-xs text-error">{{ amountError() }}</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="tx-currency"
              >العملة</label
            >
            <select
              id="tx-currency"
              [formField]="draftForm.currency"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            >
              @for (item of currencies(); track item.code) {
                <option [value]="item.code">{{ item.code }} ({{ item.symbol }})</option>
              }
            </select>
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="tx-account"
            >الحساب المالي</label
          >
          <select
            id="tx-account"
            [formField]="draftForm.accountId"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">اختر حساباً</option>
            @for (account of activeAccounts(); track account.id) {
              <option [value]="account.id">
                {{ account.name }} ({{ account.currency ?? '' }})
              </option>
            }
          </select>
          @if (draftForm.accountId().touched() && draftForm.accountId().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ draftForm.accountId().errors()[0].message }}</p>
          }
          @if (accountsError(); as message) {
            <p class="mt-1 text-xs text-gray-500" role="alert">{{ message }}</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="tx-date"
            >التاريخ</label
          >
          <input
            id="tx-date"
            type="date"
            [formField]="draftForm.date"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="tx-notes"
            >ملاحظات</label
          >
          <textarea
            id="tx-notes"
            rows="2"
            autocomplete="off"
            [formField]="draftForm.notes"
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
          <app-button type="submit" [loading]="saving()">حفظ المعاملة</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class FinancialTransactionDialog {
  private readonly store = inject(SuppliersStore);
  private readonly reference = inject(ReferenceStore);

  readonly open = input(false);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;
  readonly accounts = this.store.accounts;
  readonly accountsError = this.store.accountsError;

  readonly suppliers = this.store.suppliers;
  readonly currencies = this.reference.currencies;

  readonly activeSuppliers = computed(() =>
    (this.suppliers() ?? []).filter((supplier: SupplierResponse) => supplier.isActive !== false),
  );

  readonly activeAccounts = computed(() =>
    (this.accounts() ?? []).filter((account) => account.isActive !== false),
  );

  readonly direction = signal<FinancialDirection>(1);
  readonly amountError = signal<string | null>(null);

  readonly draft = signal({
    supplierId: '',
    amount: '',
    currency: 'JOD',
    accountId: '',
    date: todayInputValue(),
    notes: '',
  });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.supplierId, { message: 'اختر مورداً.' });
    required(schema.currency, { message: 'اختر العملة.' });
    required(schema.accountId, { message: 'اختر الحساب المالي.' });
    required(schema.date, { message: 'اختر التاريخ.' });
  });

  constructor() {
    void this.reference.ensureLoaded();
    effect(() => {
      if (this.open()) {
        void this.store.ensureAccounts();
        this.direction.set(1);
        this.draft.set({
          supplierId: '',
          amount: '',
          currency: this.reference.currencies()[0]?.code ?? 'JOD',
          accountId: '',
          date: todayInputValue(),
          notes: '',
        });
        this.amountError.set(null);
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
      const amount = Number(this.draft().amount);
      if (this.draft().amount === '' || !Number.isFinite(amount) || amount <= 0) {
        this.amountError.set('أدخل مبلغاً أكبر من صفر.');
        return;
      }
      this.amountError.set(null);
      const draft = this.draft();
      const ok = await this.store.createTransaction({
        supplierId: draft.supplierId,
        direction: this.direction(),
        amount,
        currency: draft.currency,
        accountId: draft.accountId,
        date: toIsoDateTime(draft.date),
        notes: draft.notes.trim() || null,
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }
}