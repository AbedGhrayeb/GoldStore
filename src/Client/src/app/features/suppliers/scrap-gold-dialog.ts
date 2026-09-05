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
 * P3.6 — record a scrap-gold payment to a supplier. Posts `{ supplierId, karat,
 * weightInGrams, notes }` only — the server validates stock and supplier balances and moves
 * the gold OUT of both ledgers. No account/amount fields: scrap gold is settled in grams.
 */
@Component({
  selector: 'app-scrap-gold-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      title="دفعة كسر لمورد"
      subtitle="الموردون"
      icon="coins"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="scrap-supplier"
            >المورد</label
          >
          <select
            id="scrap-supplier"
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
            <label class="mb-2 block text-sm font-medium text-gray-700" for="scrap-karat"
              >العيار</label
            >
            <select
              id="scrap-karat"
              [formField]="draftForm.karat"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            >
              <option value="">—</option>
              @for (karat of karats(); track karat.value) {
                <option [value]="karat.value">{{ karat.label }}</option>
              }
            </select>
            @if (draftForm.karat().touched() && draftForm.karat().errors().length > 0) {
              <p class="mt-1 text-xs text-error">{{ draftForm.karat().errors()[0].message }}</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="scrap-weight"
              >الوزن (غ)</label
            >
            <input
              id="scrap-weight"
              type="number"
              dir="ltr"
              step="0.001"
              [formField]="draftForm.weight"
              placeholder="0.000"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (weightError()) {
              <p class="mt-1 text-xs text-error">{{ weightError() }}</p>
            }
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="scrap-notes"
            >ملاحظات</label
          >
          <textarea
            id="scrap-notes"
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
export class ScrapGoldDialog {
  private readonly store = inject(SuppliersStore);
  private readonly reference = inject(ReferenceStore);

  readonly open = input(false);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly suppliers = this.store.suppliers;
  readonly karats = this.reference.karats;

  readonly activeSuppliers = computed(() =>
    (this.suppliers() ?? []).filter((supplier: SupplierResponse) => supplier.isActive !== false),
  );

  readonly weightError = signal<string | null>(null);

  readonly draft = signal({ supplierId: '', karat: '', weight: '', notes: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.supplierId, { message: 'اختر مورداً.' });
    required(schema.karat, { message: 'اختر العيار.' });
  });

  constructor() {
    void this.reference.ensureLoaded();
    effect(() => {
      if (this.open()) {
        this.draft.set({ supplierId: '', karat: '', weight: '', notes: '' });
        this.weightError.set(null);
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
      const weight = Number(this.draft().weight);
      if (this.draft().weight === '' || !Number.isFinite(weight) || weight <= 0) {
        this.weightError.set('أدخل وزناً أكبر من صفر.');
        return;
      }
      this.weightError.set(null);
      const draft = this.draft();
      const ok = await this.store.createScrapGoldPayment({
        supplierId: draft.supplierId,
        karat: Number(draft.karat),
        weightInGrams: weight,
        notes: draft.notes.trim() || null,
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }
}
