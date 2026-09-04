import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormField, form, required, submit } from '@angular/forms/signals';

import type { ApiError } from '../../core/http/api-error';
import { Button, Dialog } from '../../shared/ui';
import { SuppliersStore, type SupplierResponse } from './suppliers-store';

export type SupplierFormMode = 'create' | 'edit';

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
 * P3.6 — create/edit dialog for one supplier. Submits through {@link SuppliersStore};
 * server errors (duplicate name, validation) render inline. There is no delete endpoint
 * in the suppliers group — rows are deactivated via toggle instead.
 */
@Component({
  selector: 'app-supplier-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      [title]="mode() === 'edit' ? 'تعديل مورد' : 'إضافة مورد'"
      [subtitle]="'الموردون'"
      [icon]="mode() === 'edit' ? 'pencil' : 'truck'"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="supplier-name"
            >اسم المورد</label
          >
          <input
            id="supplier-name"
            type="text"
            autocomplete="off"
            [formField]="draftForm.name"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (draftForm.name().touched() && draftForm.name().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ draftForm.name().errors()[0].message }}</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="supplier-phone"
            >رقم الهاتف الأساسي</label
          >
          <input
            id="supplier-phone"
            type="tel"
            dir="ltr"
            autocomplete="off"
            [formField]="draftForm.primaryPhone"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (draftForm.primaryPhone().touched() && draftForm.primaryPhone().errors().length > 0) {
            <p class="mt-1 text-xs text-error">
              {{ draftForm.primaryPhone().errors()[0].message }}
            </p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="supplier-phone-2"
            >هاتف ثانوي</label
          >
          <input
            id="supplier-phone-2"
            type="tel"
            dir="ltr"
            autocomplete="off"
            [formField]="draftForm.secondaryPhone"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="supplier-bank"
            >رقم الحساب البنكي</label
          >
          <input
            id="supplier-bank"
            type="text"
            dir="ltr"
            autocomplete="off"
            [formField]="draftForm.bankAccountNumber"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="supplier-notes"
            >ملاحظات</label
          >
          <textarea
            id="supplier-notes"
            rows="3"
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
          <app-button type="submit" [loading]="saving()">حفظ</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class SupplierFormDialog {
  private readonly store = inject(SuppliersStore);

  readonly open = input(false);
  readonly mode = input<SupplierFormMode>('create');
  /** Edit target; null in create mode. */
  readonly supplier = input<SupplierResponse | null>(null);

  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly draft = signal({
    name: '',
    primaryPhone: '',
    secondaryPhone: '',
    bankAccountNumber: '',
    notes: '',
  });

  readonly draftForm = form(this.draft, (schema) => {
    required(schema.name, { message: 'اسم المورد مطلوب.' });
    required(schema.primaryPhone, { message: 'رقم الهاتف الأساسي مطلوب.' });
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        const supplier = this.supplier();
        this.draft.set({
          name: supplier?.name ?? '',
          primaryPhone: supplier?.primaryPhone ?? '',
          secondaryPhone: supplier?.secondaryPhone ?? '',
          bankAccountNumber: supplier?.bankAccountNumber ?? '',
          notes: supplier?.notes ?? '',
        });
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
      const input = {
        name: draft.name.trim(),
        primaryPhone: draft.primaryPhone.trim(),
        secondaryPhone: draft.secondaryPhone.trim() || null,
        bankAccountNumber: draft.bankAccountNumber.trim() || null,
        notes: draft.notes.trim() || null,
      };
      const target = this.supplier();
      const ok =
        this.mode() === 'edit' && target !== null
          ? await this.store.updateSupplier(target.id ?? '', input)
          : await this.store.createSupplier(input);
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }
}