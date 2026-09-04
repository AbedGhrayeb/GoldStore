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
import type { SupplierResponse } from './suppliers-api.service';
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

/**
 * P3.6 — record a manufacturing-fee payment to a supplier. Posts
 * `{ supplierId, accountId, amount, currency, notes }`; the server checks the supplier's
 * manufacturing balance and the account's available balance. Requires the account options,
 * which are fetched best-effort (`feature: finance` gate) — the form degrades with a notice
 * when they are unavailable.
 */
@Component({
  selector: 'app-manufacturing-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      title="دفعة أجرة تصنيع"
      subtitle="الموردون"
      icon="receipt"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="mfg-supplier"
            >المورد</label
          >
          <select
            id="mfg-supplier"
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

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="mfg-amount"
              >المبلغ</label
            >
            <input
              id="mfg-amount"
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
            <label class="mb-2 block text-sm font-medium text-gray-700" for="mfg-currency"
              >العملة</label
            >
            <select
              id="mfg-currency"
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
          <label class="mb-2 block text-sm font-medium text-gray-700" for="mfg-account"
            >الحساب المالي</label
          >
          <select
            id="mfg-account"
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
          <label class="mb-2 block text-sm font-medium text-gray-700" for="mfg-notes"
            >ملاحظات</label
          >
          <textarea
            id="mfg-notes"
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
          <app-button type="submit" [loading]="saving()">حفظ الدفعة</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class ManufacturingDialog {
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

  readonly amountError = signal<string | null>(null);

  readonly draft = signal({ supplierId: '', amount: '', currency: 'JOD', accountId: '', notes: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.supplierId, { message: 'اختر مورداً.' });
    required(schema.currency, { message: 'اختر العملة.' });
    required(schema.accountId, { message: 'اختر الحساب المالي.' });
  });

  constructor() {
    void this.reference.ensureLoaded();
    effect(() => {
      if (this.open()) {
        void this.store.ensureAccounts();
        this.draft.set({
          supplierId: '',
          amount: '',
          currency: this.reference.currencies()[0]?.code ?? 'JOD',
          accountId: '',
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
      const ok = await this.store.createManufacturingPayment({
        supplierId: draft.supplierId,
        accountId: draft.accountId,
        amount,
        currency: draft.currency,
        notes: draft.notes.trim() || null,
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }
}