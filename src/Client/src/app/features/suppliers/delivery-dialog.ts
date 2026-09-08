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
import { LucideAngularModule } from 'lucide-angular';

import { ReferenceStore } from '../../core/reference/reference-store';
import type { ApiError } from '../../core/http/api-error';
import { Button, Dialog, resolveIcon } from '../../shared/ui';
import type { SupplierResponse } from './suppliers-api.service';
import { SuppliersStore } from './suppliers-store';
import { CatalogStore } from '../catalog/catalog-store';

export interface DeliveryLineDraft {
  karat: number | null;
  weight: string;
  categoryId: string;
}

/** Client-side preview of the 21K-equivalent weight (server recomputes authoritatively). */
function toEquivalent21K(weight: number, karat: number | null): number {
  if (!Number.isFinite(weight) || weight <= 0 || karat === null) {
    return 0;
  }
  if (karat === 24) {
    return Math.round((weight / 875) * 1000 * 1000) / 1000;
  }
  if (karat === 18) {
    return Math.round(((weight * 700) / 875) * 1000) / 1000;
  }
  return Math.round(weight * 1000) / 1000;
}

function round3(value: number): number {
  return Math.round(value * 1000) / 1000;
}

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

function newLine(): DeliveryLineDraft {
  return { karat: null, weight: '', categoryId: '' };
}

/**
 * Record a supplier delivery. Lines carry only category/karat/weight (the server
 * computes 21K-equivalents); header-level manufacturing fee + due amount apply to the
 * total weight. The due amount is left outstanding — settle it later via the supplier
 * payment forms (scrap gold / manufacturing payments).
 */
@Component({
  selector: 'app-delivery-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField, LucideAngularModule],
  template: `
    <app-dialog
      [open]="open()"
      title="تسليم ذهب من مورد"
      subtitle="الموردون"
      icon="truck"
      maxWidth="max-w-4xl"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="delivery-supplier"
            >المورد</label
          >
          <select
            id="delivery-supplier"
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

        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <p class="text-sm font-medium text-gray-700">الخطوط (التصنيف والعيار والوزن)</p>
            <app-button
              variant="secondary"
              size="sm"
              icon="plus"
              type="button"
              (clicked)="addLine()"
            >
              إضافة خط
            </app-button>
          </div>

          @for (line of lines(); track $index; let i = $index) {
            <div
              class="grid grid-cols-2 gap-3 rounded-lg border border-gray-200 p-3 md:grid-cols-4"
            >
              <div class="col-span-2 md:col-span-2">
                <label
                  class="mb-1.5 block text-xs font-medium text-gray-600"
                  [for]="'line-category-' + i"
                  >التصنيف</label
                >
                <select
                  [id]="'line-category-' + i"
                  [value]="line.categoryId"
                  (change)="setLineCategory(i, $any($event.target).value)"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                >
                  <option value="">بدون تصنيف</option>
                  @for (category of activeCategories(); track category.id) {
                    <option [value]="category.id">{{ category.name }}</option>
                  }
                </select>
              </div>
              <div>
                <label
                  class="mb-1.5 block text-xs font-medium text-gray-600"
                  [for]="'line-karat-' + i"
                  >العيار</label
                >
                <select
                  [id]="'line-karat-' + i"
                  [value]="line.karat ?? ''"
                  (change)="setLineKarat(i, $any($event.target).value)"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                >
                  <option value="">—</option>
                  @for (karat of karats(); track karat.value) {
                    <option [value]="karat.value">{{ karat.label }}</option>
                  }
                </select>
                @if (lineError(i, 'karat'); as message) {
                  <p class="mt-1 text-xs text-error">{{ message }}</p>
                }
              </div>
              <div>
                <label
                  class="mb-1.5 block text-xs font-medium text-gray-600"
                  [for]="'line-weight-' + i"
                  >الوزن (غ)</label
                >
                <input
                  [id]="'line-weight-' + i"
                  type="number"
                  dir="ltr"
                  step="0.001"
                  min="0"
                  [value]="line.weight"
                  (input)="setLineWeight(i, $any($event.target).value)"
                  placeholder="0.000"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                />
                @if (lineError(i, 'weight'); as message) {
                  <p class="mt-1 text-xs text-error">{{ message }}</p>
                }
              </div>
              <div class="col-span-2 flex justify-end md:col-span-4">
                <button
                  type="button"
                  class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-error/10 hover:text-red-700"
                  title="حذف الخط"
                  [attr.aria-label]="'حذف الخط ' + (i + 1)"
                  (click)="removeLine(i)"
                >
                  <lucide-icon [img]="trashIcon" [size]="16" />
                </button>
              </div>
            </div>
          }

          <div
            class="flex flex-wrap gap-x-6 gap-y-1 rounded-lg bg-gold-container/30 px-4 py-2.5 text-sm text-gray-700"
          >
            <span
              >إجمالي الوزن:
              <strong class="data-mono" dir="ltr">{{ totalWeightText() }}</strong> غ</span
            >
            <span
              >مكافئ 21ك:
              <strong class="data-mono" dir="ltr">{{ totalEquivText() }}</strong> غ</span
            >
            <span
              >الإجمالي المحتسب:
              <strong class="data-mono" dir="ltr">{{ suggestedDueText() }}</strong>
              {{ dueCurrency() }}</span
            >
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="delivery-fee"
              >أجرة التصنيع (للغرام 21ك)</label
            >
            <input
              id="delivery-fee"
              type="number"
              dir="ltr"
              step="0.001"
              min="0"
              [value]="fee()"
              (input)="setFee($any($event.target).value)"
              placeholder="0.000"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="delivery-fee-currency"
              >عملة الأجرة</label
            >
            <select
              id="delivery-fee-currency"
              [value]="currency()"
              disabled
              title="أجور التصنيع بالدينار (JOD) فقط"
              class="w-full rounded-input border border-gray-300 bg-gray-100 px-3 py-2.5 text-sm outline-none transition"
            >
              <option value="JOD">JOD (د.أ)</option>
            </select>
            <p class="mt-1 text-xs text-gray-500">أجور التصنيع بالدينار فقط</p>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="delivery-due"
              >المبلغ المستحق (للإجمالي)</label
            >
            <input
              id="delivery-due"
              type="number"
              dir="ltr"
              step="0.01"
              min="0"
              [value]="due()"
              (input)="setDue($any($event.target).value)"
              placeholder="0.00"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (invalidDue()) {
              <p class="mt-1 text-xs text-error">أدخل مبلغاً صفراً أو أكبر.</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="delivery-due-currency"
              >عملة المستحق</label
            >
            <select
              id="delivery-due-currency"
              [value]="dueCurrency()"
              (change)="dueCurrency.set($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            >
              @for (item of currencies(); track item.code) {
                <option [value]="item.code">{{ item.code }} ({{ item.symbol }})</option>
              }
            </select>
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="delivery-notes"
            >ملاحظات</label
          >
          <textarea
            id="delivery-notes"
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
          <app-button type="submit" [loading]="saving()">حفظ التسليم</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class DeliveryDialog {
  private readonly store = inject(SuppliersStore);
  private readonly reference = inject(ReferenceStore);
  private readonly catalog = inject(CatalogStore);

  readonly open = input(false);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly suppliers = this.store.suppliers;
  readonly karats = this.reference.karats;
  readonly currencies = this.reference.currencies;

  readonly activeSuppliers = computed(() =>
    (this.suppliers() ?? []).filter((supplier: SupplierResponse) => supplier.isActive !== false),
  );

  readonly activeCategories = computed(() =>
    (this.catalog.categories() ?? []).filter((category) => category.isActive !== false),
  );

  readonly lines = signal<DeliveryLineDraft[]>([newLine()]);
  readonly lineErrors = signal<Record<number, string>>({});
  readonly fee = signal('');
  readonly currency = signal('JOD');
  readonly due = signal('');
  readonly dueTouched = signal(false);
  readonly dueCurrency = signal('JOD');

  readonly draft = signal({ supplierId: '', notes: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.supplierId, { message: 'اختر مورداً.' });
  });

  /** Sum of raw line weights (valid lines only). */
  readonly totalWeight = computed(() =>
    round3(
      this.lines().reduce((sum, line) => {
        const weight = Number(line.weight);
        return line.karat !== null && Number.isFinite(weight) && weight > 0 ? sum + weight : sum;
      }, 0),
    ),
  );

  /** Sum of 21K-equivalent weights (client preview; server recomputes authoritatively). */
  readonly totalEquiv = computed(() =>
    round3(
      this.lines().reduce((sum, line) => sum + toEquivalent21K(Number(line.weight), line.karat), 0),
    ),
  );

  /** Auto-suggested total due: fee-per-gram × total equiv weight. */
  readonly suggestedDue = computed(() => round3((Number(this.fee()) || 0) * this.totalEquiv()));

  readonly totalWeightText = computed(() => this.totalWeight().toFixed(3));
  readonly totalEquivText = computed(() => this.totalEquiv().toFixed(3));
  readonly suggestedDueText = computed(() => this.suggestedDue().toFixed(3));

  readonly dueAmount = computed(() => Number(this.due()));
  readonly invalidDue = computed(() => {
    const value = this.dueAmount();
    return this.due() === '' || !Number.isFinite(value) || value < 0;
  });

  readonly trashIcon = resolveIcon('trash-2');

  constructor() {
    void this.reference.ensureLoaded();
    void this.catalog.ensureLoaded();
    effect(() => {
      if (this.open()) {
        this.draft.set({ supplierId: '', notes: '' });
        this.lines.set([newLine()]);
        this.lineErrors.set({});
        this.fee.set('');
        this.currency.set('JOD');
        this.due.set('');
        this.dueTouched.set(false);
        this.dueCurrency.set('JOD');
        this.store.clearSaveError();
      }
    });
    // Auto-fill the due amount from fee × total weight until the user overrides it.
    effect(() => {
      if (this.open() && !this.dueTouched()) {
        this.due.set(String(this.suggestedDue()));
      }
    });
  }

  lineError(index: number, field: 'karat' | 'weight'): string | null {
    const message = this.lineErrors()[index];
    return message !== undefined && message.startsWith(`${field}:`)
      ? message.slice(field.length + 1)
      : null;
  }

  setLineKarat(index: number, value: string): void {
    this.lines.update((lines) =>
      lines.map((line, i) =>
        i === index ? { ...line, karat: value === '' ? null : Number(value) } : line,
      ),
    );
  }

  setLineWeight(index: number, value: string): void {
    this.lines.update((lines) =>
      lines.map((line, i) => (i === index ? { ...line, weight: value } : line)),
    );
  }

  setLineCategory(index: number, value: string): void {
    this.lines.update((lines) =>
      lines.map((line, i) => (i === index ? { ...line, categoryId: value } : line)),
    );
  }

  setFee(value: string): void {
    this.fee.set(value);
  }

  setDue(value: string): void {
    this.due.set(value);
    this.dueTouched.set(true);
  }

  addLine(): void {
    this.lines.update((lines) => [...lines, newLine()]);
  }

  removeLine(index: number): void {
    this.lines.update((lines) => lines.filter((_, i) => i !== index));
    this.lineErrors.update((errors) => {
      const next: Record<number, string> = {};
      for (const [key, value] of Object.entries(errors)) {
        const keyIndex = Number(key);
        if (keyIndex < index) {
          next[keyIndex] = value;
        } else if (keyIndex > index) {
          next[keyIndex - 1] = value;
        }
      }
      return next;
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
      if (!this.validateLines() || this.invalidDue()) {
        return;
      }
      const draft = this.draft();
      const ok = await this.store.createDelivery({
        supplierId: draft.supplierId,
        lines: this.lines().map((line) => ({
          karat: line.karat as number,
          weightInGrams: Number(line.weight),
          categoryId: line.categoryId || null,
        })),
        manufacturingFeePerGram: Number(this.fee()) || 0,
        manufacturingFeeCurrency: 'JOD',
        amountDue: Number(this.due()) || 0,
        amountDueCurrency: this.dueCurrency(),
        paymentLegs: null,
        notes: draft.notes.trim() || null,
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }

  private validateLines(): boolean {
    const errors: Record<number, string> = {};
    this.lines().forEach((line, index) => {
      if (line.karat === null) {
        errors[index] = 'karat:اختر العيار.';
      }
      const weight = Number(line.weight);
      if (line.weight === '' || !Number.isFinite(weight) || weight <= 0) {
        errors[index] = 'weight:أدخل وزناً أكبر من صفر.';
      }
    });
    this.lineErrors.set(errors);
    return Object.keys(errors).length === 0;
  }
}
