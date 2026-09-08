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
import { ReferenceStore } from '../../core/reference/reference-store';
import { Button, Dialog } from '../../shared/ui';
import { filterAccountsByCurrency } from '../../shared/finance/account-filters';
import { ExpensesStore } from './expenses-store';
import type { ExpenseResponse } from './expenses-api.service';

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

function toDateInput(value: string): string {
  if (!value) return new Date().toISOString().slice(0, 10);
  // ExpenseDate may arrive as YYYY-MM-DD or ISO; take the date part.
  return value.slice(0, 10);
}

/**
 * P3.11 — create or edit an expense. Mirrors `CreateExpenseRequest` / `UpdateExpenseRequest`
 * key-for-key (`expenseDate`, `categoryId`, `description`, `amount`, `accountId`) — never a
 * `tenantId`. The server creates a financial OUT entry; deleting reverses it. Account list is
 * best-effort (`feature: finance`) and degrades with an inline notice.
 */
@Component({
  selector: 'app-expense-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      [title]="editing() ? 'تعديل المصروف' : 'مصروف جديد'"
      subtitle="المصروفات"
      icon="trending-down"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="expense-date"
              >التاريخ <span class="text-error">*</span></label
            >
            <input
              id="expense-date"
              type="date"
              dir="ltr"
              [value]="expenseDate()"
              (change)="expenseDate.set($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="expense-amount"
              >المبلغ <span class="text-error">*</span></label
            >
            <input
              id="expense-amount"
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
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="expense-category"
            >التصنيف</label
          >
          <select
            id="expense-category"
            [value]="categoryId()"
            (change)="categoryId.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">بدون تصنيف</option>
            @for (category of categories(); track category.id) {
              <option [value]="category.id">{{ category.name }}</option>
            }
          </select>
          <p class="mt-1 text-xs text-gray-500">
            اختياري — اتركه فارغاً إن لم يكن المصروف مصنّفاً.
          </p>
        </div>

        <div>
          <span class="mb-2 block text-sm font-medium text-gray-700">العملة</span>
          <div class="flex gap-2">
            @for (option of currencies(); track option.code) {
              <label class="flex-1 cursor-pointer">
                <input
                  class="peer sr-only"
                  type="radio"
                  name="expense-currency"
                  [value]="option.code"
                  [checked]="currency() === option.code"
                  (change)="onCurrencyChange(option.code)"
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
          <label class="mb-2 block text-sm font-medium text-gray-700" for="expense-account"
            >الحساب المالي <span class="text-error">*</span></label
          >
          @if (accountsError()) {
            <p class="mb-2 rounded-input bg-warning/10 px-3 py-2 text-xs text-amber-800">
              {{ accountsError() }}
            </p>
          }
          @if (noAccounts()) {
            <p class="mb-2 rounded-input bg-warning/10 px-3 py-2 text-xs text-amber-800">
              لا توجد حسابات مالية بعملة {{ currency() }} — أنشئ حساباً من صفحة المالية أولاً.
            </p>
          }
          <select
            id="expense-account"
            [value]="accountId()"
            (change)="accountId.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">اختر الحساب</option>
            @for (account of eligibleAccounts(); track account.id) {
              <option [value]="account.id">
                {{ account.name }} — {{ account.currency
                }}{{ account.accountNumber ? ' — ' + account.accountNumber : '' }}
              </option>
            }
          </select>
          @if (missingAccount()) {
            <p class="mt-1 text-xs text-error">اختر الحساب المالي.</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="expense-description"
            >الوصف</label
          >
          <textarea
            id="expense-description"
            rows="2"
            [formField]="draftForm.description"
            placeholder="وصف المصروف (اختياري)"
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
            {{ editing() ? 'حفظ التعديلات' : 'تسجيل المصروف' }}
          </app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class ExpenseDialog {
  private readonly store = inject(ExpensesStore);
  private readonly reference = inject(ReferenceStore);

  readonly open = input(false);
  /** When provided, the dialog edits; otherwise it creates. */
  readonly expense = input<ExpenseResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;
  readonly categories = this.store.categories;
  readonly accounts = this.store.accounts;
  readonly accountsError = this.store.accountsError;
  readonly currencies = this.reference.currencies;

  readonly expenseDate = signal(toDateInput(''));
  readonly categoryId = signal('');
  readonly accountId = signal('');
  readonly amount = signal('');
  readonly currency = signal('JOD');

  readonly draft = signal({ description: '' });
  // No field-level validators: amount/account are validated manually
  // (invalidAmount/missingAccount) because they live outside the draft model.
  readonly draftForm = form(this.draft, () => undefined);

  readonly invalidAmount = computed(() => Number(this.amount()) <= 0);
  readonly missingAccount = computed(() => this.accountId() === '');
  readonly eligibleAccounts = computed(() =>
    filterAccountsByCurrency(this.accounts(), this.currency()),
  );
  readonly noAccounts = computed(
    () => this.eligibleAccounts().length === 0 && !this.accountsError(),
  );
  readonly editing = computed(() => this.expense() !== null);

  constructor() {
    void this.reference.ensureLoaded();
    effect(() => {
      if (this.open()) {
        const expense = this.expense();
        this.expenseDate.set(toDateInput((expense?.expenseDate as string) ?? ''));
        this.categoryId.set((expense?.categoryId as string) ?? '');
        this.accountId.set((expense?.accountId as string) ?? '');
        this.amount.set(expense?.amount !== undefined ? String(expense.amount) : '');
        this.currency.set((expense as { currency?: string } | null)?.currency ?? 'JOD');
        this.draft.set({ description: (expense?.description as string) ?? '' });
        this.store.clearSaveError();
        void this.store.ensureCategories();
        void this.store.ensureAccounts();
      }
    });
  }

  onCurrencyChange(code: string): void {
    this.currency.set(code);
    const current = this.accountId();
    const stillEligible = this.eligibleAccounts().some((account) => account.id === current);
    if (!stillEligible) {
      this.accountId.set('');
    }
  }

  /** Required for Angular signals in `form()` — must not be called externally. */
  required = required;

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
      const description = this.draft().description.trim() || null;
      const categoryId = this.categoryId() || null;
      const accountId = this.accountId();
      const expenseDate = this.expenseDate();
      const amount = Number(this.amount());
      const existing = this.expense();
      const ok = existing
        ? await this.store.updateExpense(existing.id as string, {
            expenseDate,
            categoryId,
            description,
            amount,
            accountId,
          })
        : await this.store.createExpense({
            expenseDate,
            categoryId,
            description,
            amount,
            accountId,
          });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }
}
