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

import { ReferenceStore, type CurrencyReference } from '../../core/reference/reference-store';
import type { ApiError } from '../../core/http/api-error';
import { Button, Dialog, resolveIcon } from '../../shared/ui';
import { SalesStore } from './sales-store';

/** One invoice line draft (categoryId optional, karat defaulted to 21). */
export interface InvoiceItemDraft {
  categoryId: string;
  karat: number;
  weight: string;
  pricePerGram: string;
}

/** One multi-currency payment leg draft. */
export interface PaymentLegDraft {
  accountId: string;
  accountType: string;
  currency: string;
  amount: string;
  rate: string;
}

const CURRENCY_SYMBOLS: Readonly<Record<string, string>> = { JOD: 'د.أ', USD: '$', ILS: '₪' };

function round3(value: number): number {
  return Math.round(value * 1000) / 1000;
}

function toLocalDateInput(value: Date): string {
  const offset = value.getTimezoneOffset();
  const local = new Date(value.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 10);
}

function symbol(code: string): string {
  return CURRENCY_SYMBOLS[code] ?? code;
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

/**
 * P3.8 — create a sales invoice. Mirrors the MVC invoice builder: customer info, a line-item
 * editor (category/karat/weight/price — the server computes the 21K-equivalent), the selling
 * employee, invoice currency, the due total, a single payment (cash / bank) OR multi-currency
 * payment legs, notes, and a live summary. Posts the exact `CreateSalesInvoiceRequest` keys —
 * no `tenantId` — and surfaces the server 400 validation / 409 insufficient-stock inline.
 */
@Component({
  selector: 'app-invoice-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField, LucideAngularModule],
  template: `
    <app-dialog
      [open]="open()"
      title="فاتورة مبيعات جديدة"
      subtitle="المبيعات"
      icon="receipt"
      maxWidth="max-w-3xl"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <p class="text-xs text-gray-500">
          الرقم القادم:
          <span class="data-mono font-medium text-gray-800" dir="ltr">
            {{ nextNumber() || (nextNumberError() ? 'غير متاح' : 'جاري التحميل...') }}
          </span>
        </p>

        <!-- Customer -->
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="invoice-customer-name"
              >اسم العميل <span class="text-error">*</span></label
            >
            <input
              id="invoice-customer-name"
              type="text"
              autocomplete="off"
              [formField]="draftForm.customerName"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (draftForm.customerName().touched() && draftForm.customerName().errors().length > 0) {
              <p class="mt-1 text-xs text-error">{{ draftForm.customerName().errors()[0].message }}</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="invoice-customer-phone"
              >رقم الهاتف</label
            >
            <input
              id="invoice-customer-phone"
              type="tel"
              dir="ltr"
              autocomplete="off"
              [formField]="draftForm.customerPhone"
              placeholder="05XX XXX XXXX"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
        </div>

        <!-- Items editor -->
        <div class="rounded-lg border border-gray-200">
          <div class="flex items-center justify-between border-b border-gray-200 px-4 py-3">
            <p class="text-sm font-semibold text-gray-700">عناصر الفاتورة</p>
            <app-button
              variant="secondary"
              size="sm"
              icon="plus"
              type="button"
              (clicked)="addItem()"
            >
              إضافة صنف
            </app-button>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full min-w-[640px] border-collapse text-sm">
              <thead>
                <tr class="border-b border-gray-200 bg-gray-50 text-gray-600">
                  <th class="px-4 py-2.5 text-start font-semibold">الصنف</th>
                  <th class="px-3 py-2.5 text-center font-semibold">العيار</th>
                  <th class="px-3 py-2.5 text-start font-semibold">الوزن (غ)</th>
                  <th class="px-3 py-2.5 text-start font-semibold">السعر/جم</th>
                  <th class="px-4 py-2.5 text-end font-semibold">الإجمالي</th>
                  <th class="px-2 py-2.5 text-center"></th>
                </tr>
              </thead>
              <tbody>
                @for (item of items(); track $index; let i = $index) {
                  <tr class="border-b border-gray-100 last:border-0">
                    <td class="px-4 py-2.5">
                      <select
                        [attr.data-idx]="i"
                        data-field="categoryId"
                        [value]="item.categoryId"
                        (change)="setItemField(i, 'categoryId', $any($event.target).value)"
                        class="w-full rounded-input border border-gray-300 bg-white px-2 py-2 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                      >
                        <option value="">بدون تصنيف</option>
                        @for (category of activeCategories(); track category.id) {
                          <option [value]="category.id">{{ category.name }}</option>
                        }
                      </select>
                      @if (categoriesError(); as message) {
                        <p class="mt-1 text-xs text-gray-500">{{ message }}</p>
                      }
                    </td>
                    <td class="px-3 py-2.5">
                      <select
                        [attr.data-idx]="i"
                        data-field="karat"
                        [value]="item.karat"
                        (change)="setItemField(i, 'karat', $any($event.target).value)"
                        class="w-full rounded-input border border-gray-300 bg-white px-2 py-2 text-center text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                      >
                        @for (karat of karats(); track karat.value) {
                          <option [value]="karat.value">{{ karat.label }}</option>
                        }
                      </select>
                    </td>
                    <td class="px-3 py-2.5">
                      <input
                        [attr.data-idx]="i"
                        data-field="weight"
                        type="number"
                        dir="ltr"
                        step="0.001"
                        min="0"
                        [value]="item.weight"
                        (input)="setItemField(i, 'weight', $any($event.target).value)"
                        placeholder="0.000"
                        class="w-full rounded-input border border-gray-300 bg-white px-2 py-2 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                      />
                    </td>
                    <td class="px-3 py-2.5">
                      <input
                        [attr.data-idx]="i"
                        data-field="price"
                        type="number"
                        dir="ltr"
                        step="0.001"
                        min="0"
                        [value]="item.pricePerGram"
                        (input)="setItemField(i, 'price', $any($event.target).value)"
                        placeholder="0.000"
                        class="w-full rounded-input border border-gray-300 bg-white px-2 py-2 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                      />
                    </td>
                    <td class="px-4 py-2.5 text-end data-mono" dir="ltr">
                      {{ itemTotal(i) }}
                    </td>
                    <td class="px-2 py-2.5 text-center">
                      <button
                        type="button"
                        class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-error/10 hover:text-red-700"
                        [attr.aria-label]="'حذف الصنف ' + (i + 1)"
                        (click)="removeItem(i)"
                      >
                        <lucide-icon [img]="trashIcon" [size]="16" />
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="6" class="px-4 py-8 text-center text-sm text-gray-500">
                      أضف صنفاً واحداً على الأقل.
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (itemsError(); as message) {
            <p class="border-t border-gray-200 px-4 py-2 text-xs text-error">{{ message }}</p>
          }
        </div>

        <!-- Employee -->
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="invoice-employee"
            >البائع (الموظف) <span class="text-error">*</span></label
          >
          @if (employeesError(); as message) {
            <p class="mb-2 rounded-input bg-error/10 px-3 py-2 text-xs text-red-700">{{ message }}</p>
          }
          <select
            id="invoice-employee"
            [value]="employeeId()"
            (change)="employeeId.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">اختر الموظف</option>
            @for (employee of activeEmployees(); track employee.id) {
              <option [value]="employee.id">{{ employee.fullName ?? '' }}</option>
            }
          </select>
          @if (employeeError()) {
            <p class="mt-1 text-xs text-error">اختر الموظف البائع.</p>
          }
        </div>

        <!-- Currency + total -->
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700">عملة الفاتورة</label>
            <div class="flex gap-2">
              @for (option of currencyOptions(); track option.code) {
                <label class="flex-1 cursor-pointer">
                  <input
                    class="peer sr-only"
                    type="radio"
                    name="invoice-currency"
                    [value]="option.code"
                    [checked]="currency() === option.code"
                    (change)="currency.set(option.code)"
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
            <label class="mb-2 block text-sm font-medium text-gray-700" for="invoice-total"
              >المبلغ المستحق <span class="text-error">*</span></label
            >
            <input
              id="invoice-total"
              type="number"
              dir="ltr"
              step="0.001"
              min="0"
              [value]="totalAmount()"
              (input)="totalAmount.set($any($event.target).value)"
              placeholder="0.000"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            <p class="mt-1 text-xs text-gray-500">
              إجمالي المبلغ المستحق عن الفاتورة بعملة الفاتورة المحددة.
            </p>
          </div>
        </div>

        <!-- Payment -->
        <div class="rounded-lg border border-gray-200 p-4">
          <div class="mb-3 flex items-center justify-between">
            <p class="text-sm font-semibold text-gray-700">طريقة الدفع</p>
            <label class="inline-flex cursor-pointer items-center gap-2 select-none">
              <input
                type="checkbox"
                [checked]="paymentLegsEnabled()"
                (change)="toggleLegs($any($event.target).checked)"
                class="h-4 w-4 rounded border-gray-300 accent-gold"
              />
              <span class="text-sm text-gray-600">دفع بعملات متعددة</span>
            </label>
          </div>

          @if (!paymentLegsEnabled()) {
            <div class="space-y-3">
              <div class="flex gap-2">
                <label class="flex-1 cursor-pointer">
                  <input
                    class="peer sr-only"
                    type="radio"
                    name="invoice-payment-method"
                    value="1"
                    [checked]="paymentMethod() === '1'"
                    (change)="paymentMethod.set('1')"
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
                    name="invoice-payment-method"
                    value="2"
                    [checked]="paymentMethod() === '2'"
                    (change)="paymentMethod.set('2')"
                  />
                  <span
                    class="block rounded-input border border-gray-300 py-2 text-center text-sm transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60 peer-checked:ring-1 peer-checked:ring-gold"
                    >تحويل بنكي</span
                  >
                </label>
              </div>
              <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <label class="mb-2 block text-sm font-medium text-gray-700" for="invoice-amount-paid"
                    >المبلغ المدفوع الآن</label
                  >
                  <input
                    id="invoice-amount-paid"
                    type="number"
                    dir="ltr"
                    step="0.001"
                    min="0"
                    [value]="amountPaid()"
                    (input)="amountPaid.set($any($event.target).value)"
                    placeholder="0.000"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                  <p class="mt-1 text-xs text-gray-500">الرصيد المتبقي يُسجل ديناً على العميل.</p>
                </div>
                @if (paymentMethod() === '2') {
                  <div class="space-y-3 rounded-input bg-gray-50 p-3">
                    @if (accountsError(); as message) {
                      <p class="rounded-input bg-error/10 px-2 py-1.5 text-xs text-red-700">{{ message }}</p>
                    }
                    <div>
                      <label class="mb-1.5 block text-xs font-medium text-gray-600" for="invoice-account"
                        >حساب الاستلام <span class="text-error">*</span></label
                      >
                      <select
                        id="invoice-account"
                        [value]="accountId()"
                        (change)="accountId.set($any($event.target).value)"
                        class="w-full rounded-input border border-gray-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                      >
                        <option value="">اختر الحساب</option>
                        @for (account of bankAccounts(); track account.id) {
                          <option [value]="account.id">{{ account.name }} ({{ account.currency }})</option>
                        }
                      </select>
                    </div>
                    <div>
                      <label class="mb-1.5 block text-xs font-medium text-gray-600" for="invoice-buyer-account"
                        >رقم حساب المشتري <span class="text-error">*</span></label
                      >
                      <input
                        id="invoice-buyer-account"
                        type="text"
                        dir="ltr"
                        autocomplete="off"
                        [value]="buyerAccountNumber()"
                        (input)="buyerAccountNumber.set($any($event.target).value)"
                        placeholder="رقم الحساب"
                        class="w-full rounded-input border border-gray-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                      />
                    </div>
                  </div>
                }
              </div>
            </div>
          } @else {
            <div class="space-y-3">
              <div class="flex items-center justify-between">
                <p class="text-sm font-medium text-gray-700">الدفعات</p>
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
                <p class="rounded-input bg-error/10 px-3 py-2 text-xs text-red-700">{{ message }}</p>
              }
              <div class="overflow-x-auto">
                <table class="w-full min-w-[600px] border-collapse text-sm">
                  <thead>
                    <tr class="border-b border-gray-200 bg-gray-50 text-gray-600">
                      <th class="px-3 py-2.5 text-start font-semibold">الحساب</th>
                      <th class="px-3 py-2.5 text-center font-semibold">العملة</th>
                      <th class="px-3 py-2.5 text-start font-semibold">المبلغ</th>
                      <th class="px-3 py-2.5 text-start font-semibold">سعر الصرف</th>
                      <th class="px-3 py-2.5 text-end font-semibold">المكافئ</th>
                      <th class="px-2 py-2.5 text-center"></th>
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
                            @for (account of accounts(); track account.id) {
                              <option [value]="account.id">{{ account.name }} ({{ account.currency }})</option>
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
                            [readonly]="leg.currency === currency()"
                            (input)="setLegField(i, 'rate', $any($event.target).value)"
                            placeholder="1"
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
                        <td colspan="6" class="px-3 py-8 text-center text-sm text-gray-500">
                          أضف دفعة واحدة على الأقل.
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
              <div class="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-input bg-gray-50 px-3 py-2 text-sm">
                <span>
                  الإجمالي المكافئ:
                  <span class="font-semibold data-mono" dir="ltr"
                    >{{ legsTotalDisplay() }} {{ symbol(currency()) }}</span
                  >
                </span>
                <span>
                  المبلغ المستحق:
                  <span class="data-mono" dir="ltr"
                    >{{ totalAmount() === '' ? '0.000' : Number(totalAmount()).toFixed(3) }}
                    {{ symbol(currency()) }}</span
                  >
                </span>
                <span>
                  المتبقي:
                  <span class="font-semibold data-mono" dir="ltr"
                    >{{ legsRemaining() }} {{ symbol(currency()) }}</span
                  >
                </span>
              </div>
              @if (legsOverCap()) {
                <p class="text-xs text-error">مجموع الدفعات يتجاوز المبلغ المستحق.</p>
              }
              @if (legsError(); as message) {
                <p class="text-xs text-error">{{ message }}</p>
              }
            </div>
          }
        </div>

        <!-- Notes -->
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="invoice-notes"
            >ملاحظات</label
          >
          <textarea
            id="invoice-notes"
            rows="2"
            autocomplete="off"
            [formField]="draftForm.notes"
            placeholder="ملاحظات عامة على الفاتورة (اختياري)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          ></textarea>
        </div>

        <!-- Summary -->
        <div class="rounded-lg border border-gold/30 bg-gold-container/30 p-4">
          <div class="grid grid-cols-1 gap-3 sm:grid-cols-4">
            <div>
              <p class="text-xs text-gray-500">إجمالي الوزن (جم)</p>
              <p class="mt-1 font-semibold data-mono" dir="ltr">{{ totalWeight() }}</p>
            </div>
            <div>
              <p class="text-xs text-gray-500">قيمة الذهب</p>
              <p class="mt-1 font-semibold data-mono" dir="ltr">
                {{ goldValue() }} {{ symbol(currency()) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">المبلغ المدفوع</p>
              <p class="mt-1 font-semibold data-mono" dir="ltr">
                {{ paidAmount().toFixed(3) }} {{ symbol(currency()) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">الرصيد المتبقي (دين)</p>
              <p class="mt-1 font-semibold text-gold data-mono" dir="ltr">
                {{ remaining() }} {{ symbol(currency()) }}
              </p>
            </div>
          </div>
        </div>

        @if (formError(); as message) {
          <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ message }}
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
          <app-button type="submit" [loading]="saving()">إصدار الفاتورة</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class InvoiceDialog {
  private readonly store = inject(SalesStore);
  private readonly reference = inject(ReferenceStore);

  readonly open = input(false);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;
  readonly nextNumber = this.store.nextNumber;
  readonly nextNumberError = this.store.nextNumberError;
  readonly employees = this.store.employees;
  readonly employeesError = this.store.employeesError;
  readonly categories = this.store.categories;
  readonly categoriesError = this.store.categoriesError;
  readonly accounts = this.store.accounts;
  readonly accountsError = this.store.accountsError;

  readonly karats = this.reference.karats;
  readonly currencies = this.reference.currencies;

  readonly activeEmployees = computed(() =>
    (this.employees() ?? []).filter((employee) => employee.isActive !== false),
  );
  readonly activeCategories = computed(() =>
    (this.categories() ?? []).filter((category) => category.isActive !== false),
  );
  readonly currencyOptions = computed<CurrencyReference[]>(() => {
    const preferred = ['JOD', 'USD', 'ILS'];
    const available = this.currencies().filter((option) => preferred.includes(option.code));
    const fallback: CurrencyReference[] = preferred.map((code, index) => ({
      value: index,
      code,
      symbol: CURRENCY_SYMBOLS[code] ?? code,
    }));
    return available.length === 3 ? available : fallback;
  });
  readonly bankAccounts = computed(() =>
    (this.accounts() ?? []).filter(
      (account) => account.currency === this.currency() && account.accountType === 'Bank',
    ),
  );

  readonly items = signal<InvoiceItemDraft[]>([]);
  readonly itemsError = signal('');
  readonly employeeId = signal('');
  readonly employeeError = signal(false);
  readonly currency = signal('JOD');
  readonly date = signal('');
  readonly totalAmount = signal('');
  readonly paymentLegsEnabled = signal(false);
  readonly paymentMethod = signal<'1' | '2'>('1');
  readonly amountPaid = signal('');
  readonly accountId = signal('');
  readonly buyerAccountNumber = signal('');
  readonly legs = signal<PaymentLegDraft[]>([]);
  readonly legsError = signal('');
  readonly formError = signal('');

  readonly draft = signal({ customerName: '', customerPhone: '', notes: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.customerName, { message: 'اسم العميل مطلوب.' });
  });

  readonly trashIcon = resolveIcon('trash-2');
  readonly symbol = symbol;
  readonly Number = Number;

  constructor() {
    void this.reference.ensureLoaded();
    void this.store.loadNextNumber();
    effect(() => {
      if (this.open()) {
        this.draft.set({ customerName: '', customerPhone: '', notes: '' });
        this.items.set([
          { categoryId: '', karat: 21, weight: '', pricePerGram: '' },
        ]);
        this.itemsError.set('');
        this.employeeId.set('');
        this.employeeError.set(false);
        this.currency.set(this.currencyOptions()[0]?.code ?? 'JOD');
        this.date.set(toLocalDateInput(new Date()));
        this.totalAmount.set('');
        this.paymentLegsEnabled.set(false);
        this.paymentMethod.set('1');
        this.amountPaid.set('');
        this.accountId.set('');
        this.buyerAccountNumber.set('');
        this.legs.set([{ accountId: '', accountType: '', currency: '', amount: '', rate: '' }]);
        this.legsError.set('');
        this.formError.set('');
        this.store.clearSaveError();
      }
    });
  }

  itemTotal(index: number): string {
    const item = this.items()[index];
    if (!item) {
      return '0.000';
    }
    const weight = Number(item.weight);
    const price = Number(item.pricePerGram);
    const total = Number.isFinite(weight) && Number.isFinite(price) ? weight * price : 0;
    return total.toFixed(3);
  }

  setItemField(index: number, field: 'categoryId' | 'karat' | 'weight' | 'price', value: string): void {
    this.items.update((items) =>
      items.map((item, i) =>
        i === index
          ? {
              ...item,
              categoryId: field === 'categoryId' ? value : item.categoryId,
              karat: field === 'karat' ? Number(value) || item.karat : item.karat,
              weight: field === 'weight' ? value : item.weight,
              pricePerGram: field === 'price' ? value : item.pricePerGram,
            }
          : item,
      ),
    );
  }

  addItem(): void {
    this.items.update((items) => [
      ...items,
      { categoryId: '', karat: 21, weight: '', pricePerGram: '' },
    ]);
  }

  removeItem(index: number): void {
    this.items.update((items) => items.filter((_, i) => i !== index));
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
    const rate = leg.currency === this.currency() ? 1 : Number(leg.rate);
    const equivalent = Number.isFinite(rate) ? amount * rate : 0;
    return round3(equivalent).toFixed(3);
  }

  readonly legsTotal = computed(() =>
    round3(
      this.legs().reduce((sum, leg, index) => {
        const amount = Number(leg.amount);
        if (!Number.isFinite(amount) || amount <= 0) {
          return sum;
        }
        const rate = leg.currency === this.currency() ? 1 : Number(leg.rate);
        return sum + (Number.isFinite(rate) ? amount * rate : 0);
      }, 0),
    ),
  );

  readonly legsTotalDisplay = computed(() => this.legsTotal().toFixed(3));

  readonly legsRemaining = computed(() =>
    Math.max(0, (Number(this.totalAmount()) || 0) - this.legsTotal()).toFixed(3),
  );

  readonly legsOverCap = computed(
    () => this.totalAmount() !== '' && this.legsTotal() > Number(this.totalAmount()),
  );

  readonly paidAmount = computed(() =>
    this.paymentLegsEnabled() ? this.legsTotal() : Number(this.amountPaid()) || 0,
  );

  readonly remaining = computed(() =>
    Math.max(0, (Number(this.totalAmount()) || 0) - this.paidAmount()).toFixed(3),
  );

  readonly totalWeight = computed(() =>
    this.items()
      .reduce((sum, item) => sum + (Number(item.weight) || 0), 0)
      .toFixed(3),
  );

  readonly goldValue = computed(() =>
    this.items()
      .reduce((sum, item) => {
        const weight = Number(item.weight);
        const price = Number(item.pricePerGram);
        return sum + (Number.isFinite(weight) && Number.isFinite(price) ? weight * price : 0);
      }, 0)
      .toFixed(3),
  );

  setLegAccount(index: number, value: string): void {
    const account = this.accounts().find((candidate) => candidate.id === value);
    this.legs.update((legs) =>
      legs.map((leg, i) =>
        i === index
          ? {
              ...leg,
              accountId: value,
              accountType: account?.accountType ?? '',
              currency: account?.currency ?? '',
              rate: account && account.currency !== this.currency() ? '' : '1',
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

  addLeg(): void {
    this.legs.update((legs) => [
      ...legs,
      { accountId: '', accountType: '', currency: '', amount: '', rate: '' },
    ]);
  }

  removeLeg(index: number): void {
    this.legs.update((legs) => legs.filter((_, i) => i !== index));
  }

  toggleLegs(enabled: boolean): void {
    this.paymentLegsEnabled.set(enabled);
    this.legsError.set('');
    if (enabled && this.legs().length === 0) {
      this.legs.set([{ accountId: '', accountType: '', currency: '', amount: '', rate: '' }]);
    }
    if (!enabled) {
      this.amountPaid.set('');
    }
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  onSubmit(): void {
    submit(this.draftForm, async () => {
      this.formError.set('');
      this.employeeError.set(false);
      this.itemsError.set('');
      this.legsError.set('');

      if (this.employeeId() === '') {
        this.employeeError.set(true);
        this.formError.set('اختر الموظف البائع للفاتورة.');
        return;
      }
      if (this.items().length === 0 || !this.validateItems()) {
        this.formError.set('أضف صنفاً واحداً على الأقل بوزن وسعر أكبر من صفر.');
        return;
      }
      const total = Number(this.totalAmount());
      if (this.totalAmount() === '' || !Number.isFinite(total) || total <= 0) {
        this.formError.set('أدخل المبلغ المستحق (أكبر من صفر).');
        return;
      }

      const legsEnabled = this.paymentLegsEnabled();
      const paid = this.paidAmount();
      if (paid > total) {
        this.formError.set('المبلغ المدفوع يتجاوز المبلغ المستحق.');
        return;
      }
      if (legsEnabled) {
        if (!this.validateLegs()) {
          this.formError.set('تحقق من الدفعات (الحساب والمبلغ وسعر الصرف).');
          return;
        }
        if (this.legsOverCap()) {
          this.formError.set('مجموع الدفعات يتجاوز المبلغ المستحق.');
          return;
        }
      } else if (this.paymentMethod() === '2' && paid > 0) {
        if (this.accountId() === '') {
          this.formError.set('اختر حساب الاستلام للتحويل البنكي.');
          return;
        }
        if (this.buyerAccountNumber().trim() === '') {
          this.formError.set('أدخل رقم حساب المشتري للتحويل البنكي.');
          return;
        }
      }

      await this.submitInvoice(total, legsEnabled, paid);
    });
  }

  private validateItems(): boolean {
    return this.items().every((item) => {
      const weight = Number(item.weight);
      const price = Number(item.pricePerGram);
      return Number.isFinite(weight) && weight > 0 && Number.isFinite(price) && price > 0;
    });
  }

  private validateLegs(): boolean {
    const base = this.currency();
    return this.legs().every((leg) => {
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
  }

  private async submitInvoice(total: number, legsEnabled: boolean, paid: number): Promise<void> {
    const base = this.currency();
    const draft = this.draft();
    const payload = {
      customerName: draft.customerName.trim(),
      customerPhone: draft.customerPhone.trim() || null,
      date: new Date(this.date()).toISOString(),
      currency: base,
      items: this.items().map((item) => ({
        categoryId: item.categoryId || null,
        karat: item.karat,
        weightInGrams: Number(item.weight),
        pricePerGram: Number(item.pricePerGram),
      })),
      totalAmount: total,
      amountPaid: round3(paid),
      paymentMethod: legsEnabled
        ? (this.legs()[0]?.accountType === 'Bank' ? 2 : 1)
        : Number(this.paymentMethod()),
      accountId: legsEnabled
        ? (this.legs()[0]?.accountId ?? null)
        : (this.accountId() || null),
      buyerAccountNumber: legsEnabled
        ? null
        : (this.buyerAccountNumber().trim() || null),
      employeeId: this.employeeId(),
      paymentLegs: legsEnabled
        ? this.legs()
            .filter((leg) => leg.accountId !== '' && Number(leg.amount) > 0)
            .map((leg) => ({
              accountId: leg.accountId,
              currency: leg.currency,
              amount: Number(leg.amount),
              exchangeRate: leg.currency === base ? 1 : Number(leg.rate),
            }))
        : null,
      notes: draft.notes.trim() || null,
    };

    const ok = await this.store.createInvoice(payload);
    if (ok) {
      this.saved.emit();
      this.onDismiss();
    } else {
      const error = this.store.saveError();
      this.formError.set(error ? this.errorMessage(error) : 'تعذّر إصدار الفاتورة.');
    }
  }
}