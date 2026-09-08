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

export interface ManufacturingLegDraft {
  accountId: string;
  accountType: string;
  currency: string;
  amount: string;
  rate: string;
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

function round3(value: number): number {
  return Math.round(value * 1000) / 1000;
}

/**
 * Record a manufacturing-fee payment to a supplier. The supplier's outstanding dues — the
 * total of the delivery due amounts — are listed per currency and selectable; picking a due
 * starts the payment in that currency. The user then pays the full due or a part of it,
 * either from one account or — like sales invoices — split across several payment legs
 * picked by account (the leg currency follows the account). Cross-currency legs convert
 * into the due currency through an explicit exchange rate
 * (1 leg-currency = rate × due-currency), so totals and the remainder are always exact.
 */
@Component({
  selector: 'app-manufacturing-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField, LucideAngularModule],
  template: `
    <app-dialog
      [open]="open()"
      title="دفعة أجرة تصنيع"
      subtitle="الموردون"
      icon="receipt"
      maxWidth="max-w-4xl"
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

        @if (duesLoaded()) {
          <div>
            <p class="mb-2 text-sm font-medium text-gray-700">
              المستحق حسب العملة — اختر المستحق للدفع
            </p>
            @if (duesList().length > 0) {
              <div class="flex flex-wrap gap-2" role="group" aria-label="المستحق حسب العملة">
                @for (due of duesList(); track due.currency) {
                  <button
                    type="button"
                    [disabled]="due.amount <= 0"
                    [attr.aria-pressed]="due.currency === draft().currency"
                    (click)="selectDue(due.currency)"
                    class="rounded-input border px-4 py-2 text-sm transition data-mono"
                    [class]="
                      due.currency === draft().currency
                        ? 'border-gold bg-gold-container/60 font-semibold ring-1 ring-gold'
                        : 'border-gray-300 bg-white hover:border-gold disabled:cursor-not-allowed disabled:opacity-50'
                    "
                  >
                    <span dir="ltr">{{ due.amount.toFixed(3) }}</span> {{ due.currency }}
                  </button>
                }
              </div>
            } @else {
              <p class="rounded-lg bg-gray-50 px-4 py-2.5 text-sm text-gray-500">
                لا يوجد مبلغ مستحق للمورد
              </p>
            }
          </div>
        }

        <div class="grid grid-cols-2 gap-4">
          @if (!multiPay()) {
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
                (input)="amountTouched.set(true)"
                placeholder="0.000"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              />
              @if (amountError()) {
                <p class="mt-1 text-xs text-error">{{ amountError() }}</p>
              }
            </div>
          } @else {
            <div>
              <span class="mb-2 block text-sm font-medium text-gray-700">إجمالي الدفعات</span>
              <p
                class="rounded-input border border-gray-200 bg-gray-50 px-3 py-2.5 text-left text-sm font-semibold text-gold data-mono"
                dir="ltr"
              >
                {{ legsTotalText() }} {{ draft().currency }}
              </p>
              @if (dueRemainingText(); as remaining) {
                <p class="mt-1 text-xs text-gray-500">
                  المتبقي من المستحق:
                  <strong class="data-mono" dir="ltr">{{ remaining }}</strong>
                  {{ draft().currency }}
                </p>
              }
            </div>
          }
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="mfg-currency"
              >عملة الدفع</label
            >
            <select
              id="mfg-currency"
              [value]="draft().currency"
              (change)="setPayCurrency($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            >
              @for (item of currencies(); track item.code) {
                <option [value]="item.code">{{ item.code }} ({{ item.symbol }})</option>
              }
            </select>
          </div>
        </div>

        <div class="rounded-lg border border-gray-200 p-4">
          <div class="mb-3 flex items-center justify-between">
            <p class="text-sm font-semibold text-gray-700">طريقة الدفع</p>
            <label class="inline-flex cursor-pointer items-center gap-2 select-none">
              <input
                type="checkbox"
                [checked]="multiPay()"
                (change)="toggleMulti($any($event.target).checked)"
                class="h-4 w-4 rounded border-gray-300 accent-gold"
              />
              <span class="text-sm text-gray-600">دفع بعملات/حسابات متعددة</span>
            </label>
          </div>

          @if (!multiPay()) {
            <div class="space-y-3">
              <div class="flex gap-2">
                <label class="flex-1 cursor-pointer">
                  <input
                    class="peer sr-only"
                    type="radio"
                    name="mfg-payment-method"
                    value="1"
                    [checked]="singleMethod() === '1'"
                    (change)="setSingleMethod('1')"
                  />
                  <span
                    class="block rounded-input border border-gray-300 py-2 text-center text-sm transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60 peer-checked:ring-1 peer-checked:ring-gold"
                    >نقدي</span
                  >
                </label>
                <label class="flex-1 cursor-pointer">
                  <input
                    class="peer sr-only"
                    type="radio"
                    name="mfg-payment-method"
                    value="2"
                    [checked]="singleMethod() === '2'"
                    (change)="setSingleMethod('2')"
                  />
                  <span
                    class="block rounded-input border border-gray-300 py-2 text-center text-sm transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60 peer-checked:ring-1 peer-checked:ring-gold"
                    >تحويل بنكي</span
                  >
                </label>
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
                @if (accountError(); as message) {
                  <p class="mt-1 text-xs text-error">{{ message }}</p>
                }
                @if (activeAccounts().length === 0 && !accountsError()) {
                  <p class="mt-1 text-xs text-gray-500">
                    لا يوجد حساب {{ singleMethod() === '2' ? 'بنكي' : 'نقدي' }} بعملة
                    {{ draft().currency }} — أضف حساباً في المالية.
                  </p>
                }
                @if (accountsError(); as message) {
                  <p class="mt-1 text-xs text-gray-500" role="alert">{{ message }}</p>
                }
              </div>
            </div>
          } @else {
            <div class="space-y-3">
              <div class="flex items-center justify-between">
                <p class="text-xs text-gray-500">وزّع المبلغ على حسابات وعملات متعددة</p>
                <app-button
                  variant="secondary"
                  size="sm"
                  icon="plus"
                  type="button"
                  (clicked)="addLeg()"
                >
                  إضافة دفعة
                </app-button>
              </div>
              @if (accountsError(); as message) {
                <p class="rounded-input bg-error/10 px-3 py-2 text-xs text-red-700">
                  {{ message }}
                </p>
              }
              <div class="overflow-x-auto">
                <table class="w-full min-w-[600px] border-collapse text-sm">
                  <thead>
                    <tr class="border-b border-gray-200 bg-gray-50 text-gray-600">
                      <th class="px-3 py-2 text-start font-semibold">الحساب</th>
                      <th class="px-3 py-2 text-center font-semibold">العملة</th>
                      <th class="px-3 py-2 text-start font-semibold">المبلغ</th>
                      <th class="px-3 py-2 text-start font-semibold">سعر الصرف</th>
                      <th class="px-3 py-2 text-end font-semibold">المكافئ</th>
                      <th class="px-2 py-2 text-center"></th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (leg of legs(); track $index; let i = $index) {
                      <tr class="border-b border-gray-100 last:border-0">
                        <td class="px-3 py-2">
                          <select
                            [attr.data-idx]="i"
                            data-field="accountId"
                            [value]="leg.accountId"
                            (change)="setLegAccount(i, $any($event.target).value)"
                            class="w-full rounded-input border border-gray-300 bg-white px-2 py-2 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                          >
                            <option value="">اختر الحساب</option>
                            @for (account of allActiveAccounts(); track account.id) {
                              <option [value]="account.id">
                                {{ account.name }} ({{ account.currency }})
                              </option>
                            }
                          </select>
                        </td>
                        <td class="px-3 py-2 text-center">
                          <span
                            class="inline-block min-w-11 rounded bg-gold-container/50 px-2 py-1 font-medium data-mono"
                            >{{ leg.currency || '—' }}</span
                          >
                        </td>
                        <td class="px-3 py-2">
                          <input
                            [attr.data-idx]="i"
                            data-field="amount"
                            type="number"
                            dir="ltr"
                            step="0.001"
                            min="0"
                            [value]="leg.amount"
                            (input)="setLegField(i, 'amount', $any($event.target).value)"
                            placeholder="0.000"
                            class="w-full rounded-input border border-gray-300 bg-white px-2 py-2 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                          />
                        </td>
                        <td class="px-3 py-2">
                          <input
                            [attr.data-idx]="i"
                            data-field="rate"
                            type="number"
                            dir="ltr"
                            step="0.000001"
                            min="0"
                            [value]="leg.rate"
                            [readonly]="leg.currency === draft().currency"
                            (input)="setLegField(i, 'rate', $any($event.target).value)"
                            [placeholder]="ratePlaceholder(leg)"
                            [title]="rateTitle(leg)"
                            class="w-full rounded-input border border-gray-300 bg-white px-2 py-2 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30 disabled:opacity-50"
                          />
                        </td>
                        <td class="px-3 py-2 text-end font-semibold text-gold data-mono" dir="ltr">
                          {{ legEquivalent(i) }}
                        </td>
                        <td class="px-2 py-2 text-center">
                          <button
                            type="button"
                            class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-error/10 hover:text-red-700"
                            [attr.aria-label]="'حذف الدفعة ' + (i + 1)"
                            (click)="removeLeg(i)"
                          >
                            <lucide-icon [img]="trashIcon" [size]="16" />
                          </button>
                        </td>
                      </tr>
                    } @empty {
                      <tr>
                        <td colspan="6" class="px-4 py-6 text-center text-sm text-gray-500">
                          أضف دفعة واحدة على الأقل.
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
              @if (legsError(); as message) {
                <p class="text-xs text-error">{{ message }}</p>
              }
            </div>
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
  readonly balances = this.store.supplierBalances;
  readonly balancesFor = this.store.supplierBalancesFor;

  readonly suppliers = this.store.suppliers;
  readonly currencies = this.reference.currencies;

  readonly activeSuppliers = computed(() =>
    (this.suppliers() ?? []).filter((supplier: SupplierResponse) => supplier.isActive !== false),
  );

  readonly singleMethod = signal<'1' | '2'>('1');
  /** Single-mode accounts: match the payment currency AND the chosen method. */
  readonly activeAccounts = computed(() => {
    const method = this.singleMethod() === '2' ? 'Bank' : 'Cash';
    return (this.accounts() ?? []).filter(
      (account) =>
        account.isActive !== false &&
        account.currency === this.draft().currency &&
        account.accountType === method,
    );
  });

  /** Legs-mode accounts: every active account (currency follows the picked account). */
  readonly allActiveAccounts = computed(() =>
    (this.accounts() ?? []).filter((account) => account.isActive !== false),
  );

  /** Balances known for the currently selected supplier (never another supplier's). */
  readonly duesLoaded = computed(
    () =>
      this.draft().supplierId !== '' &&
      this.balancesFor() === this.draft().supplierId &&
      this.balances() !== null,
  );

  /** Outstanding dues per currency (the total of the delivery due amounts). */
  readonly duesList = computed(() =>
    ((this.duesLoaded() ? this.balances()?.manufacturingByCurrency : null) ?? []).map((entry) => ({
      currency: entry.currency,
      amount: Number(entry.netAmount),
    })),
  );

  /** Due manufacturing amount in the selected payment currency; null while unknown. */
  readonly currencyDue = computed(() => {
    if (!this.duesLoaded()) {
      return null;
    }
    const due = (this.balances()?.manufacturingByCurrency ?? []).find(
      (entry) => entry.currency.toUpperCase() === this.draft().currency.toUpperCase(),
    );
    // A loaded balance set with no entry for this currency means zero due.
    return due === undefined ? 0 : Number(due.netAmount);
  });

  readonly multiPay = signal(false);
  readonly legs = signal<ManufacturingLegDraft[]>([]);
  readonly legsError = signal('');

  /**
   * Total: each leg contributes its full raw amount when its currency equals the
   * payment currency, otherwise its converted equivalent. This mirrors the
   * "sum of المبلغ when same currency, else converted" rule.
   */
  readonly legsTotal = computed(() =>
    round3(
      this.legs().reduce((sum, leg) => {
        const amount = Number(leg.amount);
        if (!Number.isFinite(amount) || amount <= 0) {
          return sum;
        }
        if (leg.currency === this.draft().currency) {
          return sum + amount;
        }
        const rate = Number(leg.rate);
        return sum + (Number.isFinite(rate) ? amount * rate : 0);
      }, 0),
    ),
  );

  readonly legsTotalText = computed(() => this.legsTotal().toFixed(3));

  /** Remainder of the selected due after the typed legs; null while the due is unknown. */
  readonly dueRemainingText = computed(() => {
    const due = this.currencyDue();
    if (due === null || due <= 0) {
      return null;
    }
    return Math.max(0, round3(due - this.legsTotal())).toFixed(3);
  });

  readonly trashIcon = resolveIcon('trash-2');

  readonly amountError = signal<string | null>(null);
  readonly accountError = signal<string | null>(null);
  /** Set once the user types an amount; auto-suggest stops overriding it afterwards. */
  readonly amountTouched = signal(false);

  readonly draft = signal({
    supplierId: '',
    amount: '',
    currency: 'JOD',
    accountId: '',
    notes: '',
  });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.supplierId, { message: 'اختر مورداً.' });
    required(schema.currency, { message: 'اختر العملة.' });
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
        this.singleMethod.set('1');
        this.multiPay.set(false);
        this.legs.set([]);
        this.legsError.set('');
        this.amountError.set(null);
        this.accountError.set(null);
        this.amountTouched.set(false);
        this.store.clearSupplierBalances();
        this.store.clearSaveError();
      }
    });
    effect(() => {
      if (this.open()) {
        void this.store.ensureSupplierBalances(this.draft().supplierId);
      }
    });
    // Suggest the full due in the payment currency until the user types their own amount.
    effect(() => {
      if (this.open() && !this.multiPay() && !this.amountTouched() && this.currencyDue() !== null) {
        this.draft.update((current) => ({ ...current, amount: String(this.currencyDue()) }));
      }
    });
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  /** Pick a due card: pay in that currency, pre-filled with its full due amount. */
  selectDue(currency: string): void {
    this.setPayCurrency(currency);
    const due = this.currencyDue();
    if (due !== null) {
      this.draft.update((current) => ({ ...current, amount: String(due) }));
      this.amountTouched.set(false);
    }
  }

  /** Central currency change: keeps the single-mode account valid. */
  setPayCurrency(currency: string): void {
    if (currency === '' || currency === this.draft().currency) {
      return;
    }
    this.draft.update((current) => ({ ...current, currency }));
    const account = this.activeAccounts().find(
      (candidate) => candidate.id === this.draft().accountId,
    );
    if (account === undefined) {
      this.draft.update((current) => ({ ...current, accountId: '' }));
    }
  }

  setSingleMethod(method: '1' | '2'): void {
    this.singleMethod.set(method);
    const account = this.activeAccounts().find(
      (candidate) => candidate.id === this.draft().accountId,
    );
    if (account === undefined) {
      this.draft.update((current) => ({ ...current, accountId: '' }));
    }
  }

  /** Rate hint: 1 unit of leg currency buys `rate` units of the payment currency. */
  ratePlaceholder(leg: ManufacturingLegDraft): string {
    return leg.currency === this.draft().currency
      ? '1'
      : `1 ${leg.currency} = ؟ ${this.draft().currency}`;
  }

  rateTitle(leg: ManufacturingLegDraft): string {
    return leg.currency === this.draft().currency
      ? 'نفس العملة — السعر 1'
      : `أدخل سعر الصرف: كم ${this.draft().currency} تساوي الواحدة ${leg.currency}؟`;
  }

  toggleMulti(enabled: boolean): void {
    this.multiPay.set(enabled);
    this.legsError.set('');
    if (enabled) {
      this.amountError.set(null);
      this.accountError.set(null);
      this.legs.set([this.newLeg()]);
    } else {
      this.legs.set([]);
    }
  }

  addLeg(): void {
    this.legs.update((legs) => [...legs, this.newLeg()]);
  }

  removeLeg(index: number): void {
    this.legs.update((legs) => legs.filter((_, i) => i !== index));
  }

  setLegAccount(index: number, value: string): void {
    const account = (this.accounts() ?? []).find((candidate) => candidate.id === value);
    this.legs.update((legs) =>
      legs.map((leg, i) =>
        i === index
          ? {
              ...leg,
              accountId: value,
              accountType: (account?.accountType as string) ?? '',
              currency: (account?.currency as string) ?? '',
              rate: account && account.currency !== this.draft().currency ? '' : '1',
            }
          : leg,
      ),
    );
  }

  setLegField(index: number, field: 'amount' | 'rate', value: string): void {
    this.legs.update((legs) =>
      legs.map((leg, i) => (i === index ? { ...leg, [field]: value } : leg)),
    );
  }

  legEquivalent(index: number): string {
    const leg = this.legs()[index];
    if (!leg) {
      return '0.000';
    }
    const amount = Number(leg.amount);
    if (!Number.isFinite(amount) || amount <= 0) {
      return '0.000';
    }
    const rate = leg.currency === this.draft().currency ? 1 : Number(leg.rate);
    const equivalent = Number.isFinite(rate) ? amount * rate : 0;
    return round3(equivalent).toFixed(3);
  }

  onSubmit(): void {
    submit(this.draftForm, async () => {
      const draft = this.draft();
      if (this.multiPay()) {
        const paid = this.legsTotal();
        if (!this.validateLegs()) {
          return;
        }
        const due = this.currencyDue();
        if (due !== null && paid > due + 0.001) {
          this.legsError.set(`إجمالي الدفعات يتجاوز المستحق للمورد (${due.toFixed(3)}).`);
          return;
        }
        const legs = this.legs()
          .filter((leg) => leg.accountId !== '' && Number(leg.amount) > 0)
          .map((leg) => ({
            accountId: leg.accountId,
            currency: leg.currency || draft.currency,
            amount: Number(leg.amount),
            exchangeRate: leg.currency === draft.currency ? 1 : Number(leg.rate) || 0,
          }));
        const ok = await this.store.createManufacturingPayment({
          supplierId: draft.supplierId,
          // Top-level account/amount mirror the first leg and the converted legs total;
          // the server settles from `paymentLegs` in multi mode.
          accountId: legs[0]?.accountId ?? '',
          amount: paid,
          currency: draft.currency,
          paymentLegs: legs,
          notes: draft.notes.trim() || null,
        });
        if (ok) {
          this.saved.emit();
          this.onDismiss();
        }
        return;
      }
      const amount = Number(draft.amount);
      if (draft.amount === '' || !Number.isFinite(amount) || amount <= 0) {
        this.amountError.set('أدخل مبلغاً أكبر من صفر.');
        return;
      }
      if (draft.accountId === '') {
        this.accountError.set('اختر الحساب المالي.');
        return;
      }
      this.accountError.set(null);
      const due = this.currencyDue();
      if (due !== null && amount > due + 0.001) {
        this.amountError.set(`المبلغ يتجاوز المستحق للمورد (${due.toFixed(3)}).`);
        return;
      }
      this.amountError.set(null);
      const ok = await this.store.createManufacturingPayment({
        supplierId: draft.supplierId,
        accountId: draft.accountId,
        amount,
        currency: draft.currency,
        paymentLegs: null,
        notes: draft.notes.trim() || null,
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }

  private newLeg(): ManufacturingLegDraft {
    return { accountId: '', accountType: '', currency: '', amount: '', rate: '' };
  }

  private validateLegs(): boolean {
    if (this.legs().length === 0) {
      this.legsError.set('أضف دفعة واحدة على الأقل.');
      return false;
    }
    const base = this.draft().currency;
    const valid = this.legs().every((leg) => {
      if (leg.accountId === '') {
        return false;
      }
      const amount = Number(leg.amount);
      if (!Number.isFinite(amount) || amount <= 0) {
        return false;
      }
      if (leg.currency !== base) {
        const rate = Number(leg.rate);
        if (!Number.isFinite(rate) || rate <= 0) {
          return false;
        }
      }
      return true;
    });
    if (!valid) {
      this.legsError.set('تحقق من الدفعات (الحساب والمبلغ وسعر الصرف).');
      return false;
    }
    this.legsError.set('');
    return true;
  }
}
