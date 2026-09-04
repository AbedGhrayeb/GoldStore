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
import type { CategoryParentOption } from './catalog.model';
import { CatalogStore, type CategoryResponse } from './catalog-store';

export type CategoryFormMode = 'create' | 'edit';

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
 * P3.3 — create/edit dialog for one category. Fields: name (required), description,
 * parent category select (server rejects cycles — the page excludes self + descendants)
 * and an active toggle. Submission goes through {@link CatalogStore}; server errors
 * (duplicate name on the same level, validation) render inline under the fields.
 */
@Component({
  selector: 'app-category-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      [title]="mode() === 'edit' ? 'تعديل التصنيف' : 'إضافة تصنيف'"
      [subtitle]="'الكتالوج'"
      [icon]="mode() === 'edit' ? 'pencil' : 'plus'"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="category-name"
            >اسم التصنيف</label
          >
          <input
            id="category-name"
            type="text"
            [formField]="categoryForm.name"
            placeholder="مثال: خواتم"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (categoryForm.name().touched() && categoryForm.name().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ categoryForm.name().errors()[0].message }}</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="category-description"
            >الوصف</label
          >
          <textarea
            id="category-description"
            rows="3"
            [formField]="categoryForm.description"
            placeholder="وصف اختياري..."
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          ></textarea>
        </div>

        <div>
          <span class="mb-2 block text-sm font-medium text-gray-700">التصنيف الأب</span>
          <select
            [value]="parentId()"
            (change)="parentId.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">فئة رئيسية (بدون أب)</option>
            @for (parent of parentOptions(); track parent.id) {
              <option [value]="parent.id">{{ indent(parent) }}{{ parent.name }}</option>
            }
          </select>
        </div>

        <label class="flex cursor-pointer items-center gap-2">
          <input
            type="checkbox"
            [checked]="isActive()"
            (change)="isActive.set($any($event.target).checked)"
            class="h-4 w-4 accent-gold"
          />
          <span class="text-sm font-medium text-gray-700">التصنيف نشط</span>
        </label>

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
export class CategoryFormDialog {
  private readonly store = inject(CatalogStore);

  readonly open = input(false);
  readonly mode = input<CategoryFormMode>('create');
  /** Edit target; null in create mode. */
  readonly category = input<CategoryResponse | null>(null);
  readonly parentOptions = input<CategoryParentOption[]>([]);

  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly draft = signal({ name: '', description: '' });
  readonly parentId = signal('');
  readonly isActive = signal(true);

  readonly categoryForm = form(this.draft, (schema) => {
    required(schema.name, { message: 'اسم التصنيف مطلوب.' });
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        const category = this.category();
        this.draft.set({
          name: category?.name ?? '',
          description: category?.description ?? '',
        });
        this.parentId.set(category?.parentCategoryId ?? '');
        this.isActive.set(category?.isActive ?? true);
        this.store.clearSaveError();
      }
    });
  }

  indent(parent: CategoryParentOption): string {
    return '\u00A0\u00A0\u00A0'.repeat(parent.depth);
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onSubmit(): void {
    submit(this.categoryForm, async () => {
      const input = {
        name: this.draft().name.trim(),
        description: this.draft().description.trim() || null,
        parentCategoryId: this.parentId() || null,
        isActive: this.isActive(),
      };
      const ok =
        this.mode() === 'edit' && this.category() !== null
          ? await this.store.updateCategory(this.category()!.id ?? '', input)
          : await this.store.createCategory(input);
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }
}
