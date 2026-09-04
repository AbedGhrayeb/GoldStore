import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormField, form, required, submit } from '@angular/forms/signals';

import { ReferenceStore } from '../../core/reference/reference-store';
import type { ApiError } from '../../core/http/api-error';
import { Button, Dialog } from '../../shared/ui';
import { InventoryStore } from './inventory-store';

export interface AdjustmentTypeOption {
  value: number;
  label: string;
}

/** Mirrors `Domain.Inventory.InventoryAdjustmentType` (1..5). */
export const ADJUSTMENT_TYPES: readonly AdjustmentTypeOption[] = [
  { value: 1, label: 'زيادة' },
  { value: 2, label: 'نقصان' },
  { value: 3, label: 'تلف' },
  { value: 4, label: 'خسارة' },
  { value: 5, label: 'يدوي تصحيح' },
];

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
 * P3.7 — record an inventory adjustment. Posts the exact `CreateInventoryAdjustmentRequest`
 * shape (`adjustmentType`, `karat`, `weightInGrams`, `reason`, `notes`, `date`) — the server
 * writes the gold-ledger entry and computes the 21K-equivalent; the client never sends it.
 */
@Component({
  selector: 'app-adjustment-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      title="تسوية جردية"
      subtitle="المخزون"
      icon="boxes"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="adjustment-type"
              >نوع التسوية</label
            >
            <select
              id="adjustment-type"
              [value]="adjustmentType()"
              (change)="adjustmentType.set($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            >
              @for (option of ADJUSTMENT_TYPES; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="adjustment-karat"
              >العيار</label
            >
            <select
              id="adjustment-karat"
              [value]="karat()"
              (change)="karat.set($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            >
              @for (karatOption of karats(); track karatOption.value) {
                <option [value]="karatOption.value">{{ karatOption.label }}</option>
              }
            </select>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="adjustment-weight"
              >الوزن (غ)</label
            >
            <input
              id="adjustment-weight"
              type="number"
              dir="ltr"
              step="0.001"
              min="0"
              [value]="weight()"
              (input)="weight.set($any($event.target).value)"
              placeholder="0.000"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (fieldError('weight'); as message) {
              <p class="mt-1 text-xs text-error">{{ message }}</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="adjustment-date"
              >التاريخ</label
            >
            <input
              id="adjustment-date"
              type="date"
              [value]="date()"
              (change)="date.set($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (fieldError('date'); as message) {
              <p class="mt-1 text-xs text-error">{{ message }}</p>
            }
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="adjustment-reason"
            >السبب</label
          >
          <input
            id="adjustment-reason"
            type="text"
            autocomplete="off"
            [formField]="draftForm.reason"
            placeholder="مثال: كسر واجهة عرض"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (draftForm.reason().touched() && draftForm.reason().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ draftForm.reason().errors()[0].message }}</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="adjustment-notes"
            >ملاحظات</label
          >
          <textarea
            id="adjustment-notes"
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
          <app-button type="submit" [loading]="saving()">حفظ التسوية</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class AdjustmentDialog {
  private readonly store = inject(InventoryStore);
  private readonly reference = inject(ReferenceStore);

  readonly open = input(false);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly karats = this.reference.karats;
  readonly ADJUSTMENT_TYPES = ADJUSTMENT_TYPES;

  readonly adjustmentType = signal('1');
  readonly karat = signal('21');
  readonly weight = signal('');
  readonly date = signal('');

  readonly draft = signal({ reason: '', notes: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.reason, { message: 'السبب مطلوب.' });
  });

  private readonly fieldErrors = signal<Record<string, string>>({});

  constructor() {
    void this.reference.ensureLoaded();
    effect(() => {
      if (this.open()) {
        this.adjustmentType.set('1');
        this.karat.set('21');
        this.weight.set('');
        this.date.set(new Date().toISOString().slice(0, 10));
        this.draft.set({ reason: '', notes: '' });
        this.fieldErrors.set({});
        this.store.clearSaveError();
      }
    });
  }

  fieldError(field: string): string | null {
    const message = this.fieldErrors()[field];
    return message !== undefined ? message : null;
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  onSubmit(): void {
    submit(this.draftForm, async () => {
      if (!this.validateFields()) {
        return;
      }
      const draft = this.draft();
      const ok = await this.store.createAdjustment({
        adjustmentType: Number(this.adjustmentType()),
        karat: Number(this.karat()),
        weightInGrams: Number(this.weight()),
        reason: draft.reason,
        notes: draft.notes.trim() || null,
        date: new Date(`${this.date()}T00:00:00`).toISOString(),
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }

  private validateFields(): boolean {
    const errors: Record<string, string> = {};
    const weight = Number(this.weight());
    if (this.weight() === '' || !Number.isFinite(weight) || weight <= 0) {
      errors['weight'] = 'أدخل وزناً أكبر من صفر.';
    }
    if (this.date() === '') {
      errors['date'] = 'اختر التاريخ.';
    }
    this.fieldErrors.set(errors);
    return Object.keys(errors).length === 0;
  }
}