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
import { ExpensesStore } from './expenses-store';
import type { ExpenseCategoryResponse } from './expenses-api.service';

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
 * P3.11 — create or edit an expense category. The API only requires `name`; duplicates
 * (409) and validation errors render inline. Deleting is done from the table, not this dialog.
 */
@Component({
  selector: 'app-expense-category-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      [title]="category() ? 'تعديل التصنيف' : 'تصنيف جديد'"
      subtitle="المصروفات"
      icon="trending-down"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="category-name"
            >اسم التصنيف <span class="text-error">*</span></label
          >
          <input
            id="category-name"
            type="text"
            autocomplete="off"
            placeholder="مثال: إيجار، كهرباء، مصاريف مكتب"
            [formField]="draftForm.name"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (draftForm.name().touched() && draftForm.name().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ draftForm.name().errors()[0].message }}</p>
          }
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
          <app-button type="submit" [loading]="saving()">
            {{ category() ? 'حفظ التعديلات' : 'إنشاء التصنيف' }}
          </app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class ExpenseCategoryDialog {
  private readonly store = inject(ExpensesStore);

  readonly open = input(false);
  /** When provided, the dialog edits; otherwise it creates. */
  readonly category = input<ExpenseCategoryResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly draft = signal({ name: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.name, { message: 'اسم التصنيف مطلوب.' });
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        this.draft.set({ name: this.category()?.name ?? '' });
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
      const name = this.draft().name.trim();
      const existing = this.category();
      const ok = existing
        ? await this.store.updateCategory(existing.id as string, { name })
        : await this.store.createCategory({ name });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }
}
