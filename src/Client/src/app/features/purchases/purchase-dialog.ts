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

import type { ApiError } from '../../core/http/api-error';
import { ReferenceStore, type CurrencyReference } from '../../core/reference/reference-store';
import { Button, Dialog, resolveIcon } from '../../shared/ui';
import { PurchasesStore } from './purchases-store';

/** One purchase line draft (categoryId optional, karat defaulted to 21). */
export interface PurchaseItemDraft {
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
 * P3.9 — create a customer gold purchase invoice. Mirrors the MVC invoice builder: seller
 * info (name/ID number/year of birth/phone/address), a line-item editor (category/karat/
 * weight/price — the server computes the 21K-equivalent), the buying employee, invoice
 * currency, date, the due total, a single payment (cash / bank transfer + receiving account)
 * OR multi-currency payment legs, notes, and a live summary with per-karat weight totals.
 * Posts the exact `CreateCustomerPurchaseInvoiceRequest` keys — no `tenantId` — and surfaces
 * the server 400 validation / insufficient-balance inline. Emits `saved` after a successful
 * issue so the parent page can reload its list and KPIs.
 */
@Component({
  selector: 'app-purchase-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField, LucideAngularModule],
  template: `
    <app-dialog
      [open]="open()"
      title="فاتورة شراء ذهب جديدة"
      subtitle="شراء ذهب من العملاء"
      icon="shopping-bag"
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

        <!-- Seller -->
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="seller-name"
              >اسم البائع <span class="text-error">*</span></label
            >
            <input
              id="seller-name"
              type="text"
              autocomplete="off"
              [formField]="draftForm.sellerName"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (draftForm.sellerName().touched() && draftForm.sellerName().errors().length > 0) {
              <p class="mt-1 text-xs text-error">{{ draftForm.sellerName().errors()[0].message }}</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="seller-id-number"
              >رقم الهوية <span class="text-error">*</span></label
            >
            <input
              id="seller-id-number"
              type="text"
              dir="ltr"
              autocomplete="off"
              [formField]="draftForm.sellerIdNumber"
              placeholder="XXXXXXXXX"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (
              draftForm.sellerIdNumber().touched() && draftForm.sellerIdNumber().errors().length > 0
            ) {
              <p class="mt-1 text-xs text-error">
                {{ draftForm.sellerIdNumber().errors()[0].message }}
              </p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="seller-year-of-birth"
              >سنة الميلاد</label
            >
            <input
              id="seller-year-of-birth"
              type="number"
              dir="ltr"
              autocomplete="off"
              [formField]="draftForm.sellerYearOfBirth"
              placeholder="1990"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="seller-phone"
              >رقم الهاتف</label
            >
            <input
              id="seller-phone"
              type="tel"
              dir="ltr"
              autocomplete="off"
              [formField]="draftForm.sellerPhone"
              placeholder="05XX XXX XXXX"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
          <div class="md:col-span-2">
            <label class="mb-2 block text-sm font-medium text-gray-700" for="seller-address"
              >العنوان</label
            >
            <input
              id="seller-address"
              type="text"
              autocomplete="off"
              [formField]="draftForm.sellerAddress"
              placeholder="العنوان (اختياري)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
        </div>

        <!-- Items -->
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
          <div class="flex flex-wrap items-center gap-x-4 gap-y-1 border-t border-gray-200 px-4 py-2.5">
            @for (entry of karatTotals(); track entry.karat) {
              <p class="text-xs text-gray-600">
                إجمالي وزن {{ entry.karat }}:
                <span class="data-mono font-medium text-gray-900" dir="ltr"
                  >{{ entry.weight }} جم</span
                >
              </p>
            }
          </div>
          @if (itemsError(); as message) {
            <p class="border-t border-gray-200 px-4 py-2 text-xs text-error">{{ message }}</p>
          }
        </div>

        <!-- Employee + date -->
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="purchase-employee"
              >الموظف (المشتري) <span class="text-error">*</span></label
            >
            @if (employeesError(); as message) {
              <p class="mb-2 rounded-input bg-error/10 px-3 py-2 text-xs text-red-700">{{ message }}</p>
            }
            <select
              id="purchase-employee"
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
              <p class="mt-1 text-xs text-error">اختر الموظف المشتري.</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="purchase-date"
              >تاريخ الفاتورة <span class="text-error">*</span></label
            >
            <input
              id="purchase-date"
              type="date"
              [value]="date()"
              (change)="date.set($any($event.target).value)"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
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
                    name="purchase-currency"
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
            <label class="mb-2 block text-sm font-medium text-gray-700" for="purchase-total"
              >المبلغ المستحق <span class="text-error">*</span></label
            >
            <input
              id="purchase-total"
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
                    name="purchase-payment-method"
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
                    name="purchase-payment-method"
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
                  <label class="mb-2 block text-sm font-medium text-gray-700" for="purchase-amount-paid"
                    >المبلغ المدفوع الآن</label
                  >
                  <input
                    id="purchase-amount-paid"
                    type="number"
                    dir="ltr"
                    step="0.001"
                    min="0"
                    [value]="amountPaid()"
                    (input)="amountPaid.set($any($event.target).value)"
                    placeholder="0.000"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                  <p class="mt-1 text-xs text-gray-500">
                    الرصيد المتبقي يُسجل ديناً على المتجر (للبائع).
                  </p>
                </div>
                <div>
                  <label class="mb-2 block text-sm font-medium text-gray-700" for="purchase-account"
                    >حساب الدفع <span class="text-error">*</span></label
                  >
                  @if (accountsError(); as message) {
                    <p class="mb-2 rounded-input bg-error/10 px-3 py-2 text-xs text-red-700">{{ message }}</p>
                  }
                  <select
                    id="purchase-account"
                    [value]="accountId()"
                    (change)="accountId.set($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                  >
                    <option value="">اختر الحساب</option>
                    @for (account of matchingAccounts(); track account.id) {
                      <option [value]="account.id">{{ account.name }} ({{ account.currency }})</option>
                    }
                  </select>
                  <p class="mt-1 text-xs text-gray-500">
                    @if (paymentMethod() === '1') {
                      يتم اختيار حساب النقد المطابق للعملة تلقائياً.
                    } @else {
                      تظهر الحسابات البنكية المطابقة للعملة فقط.
                    }
                  </p>
                </div>
                @if (paymentMethod() === '2') {
                  <div class="md:col-span-2">
                    <label class="mb-2 block text-sm font-medium text-gray-700" for="seller-account-number"
                      >رقم حساب البائع</label
                    >
                    <input
                      id="seller-account-number"
                      type="text"
                      dir="ltr"
                      autocomplete="off"
                      [value]="sellerAccountNumber()"
                      (input)="sellerAccountNumber.set($any($event.target).value)"
                      placeholder="رقم الحساب البنكي للبائع"
                      class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                    />
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
                            @for (account of activeAccounts(); track account.id) {
                              <option [value]="account.id"
                                >{{ account.name }} ({{ account.currency }})</option
                              >
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
              <div
                class="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-input bg-gray-50 px-3 py-2 text-sm"
              >
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
          <label class="mb-2 block text-sm font-medium text-gray-700" for="purchase-notes"
            >ملاحظات</label
          >
          <textarea
            id="purchase-notes"
            rows="2"
            [formField]="draftForm.notes"
            placeholder="ملاحظات إضافية (اختياري)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          ></textarea>
        </div>

        <!-- Summary -->
        <div class="rounded-lg border border-gold/30 bg-gold-container/30 p-4">
          <div class="grid grid-cols-2 gap-3 sm:grid-cols-5">
            <div>
              <p class="text-xs text-gray-500">إجمالي الوزن</p>
              <p class="mt-0.5 font-semibold text-gray-900 data-mono" dir="ltr">
                {{ totalWeight() }} جم
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">قيمة الذهب</p>
              <p class="mt-0.5 font-semibold text-gray-900 data-mono" dir="ltr">
                {{ goldValue() }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">المستحق</p>
              <p class="mt-0.5 font-semibold text-gray-900 data-mono" dir="ltr">
                {{ totalAmount() === '' ? '0.000' : Number(totalAmount()).toFixed(3) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">المدفوع</p>
              <p class="mt-0.5 font-semibold text-emerald-700 data-mono" dir="ltr">
                {{ paidAmount().toFixed(3) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">المتبقي (دين)</p>
              <p class="mt-0.5 font-semibold text-red-700 data-mono" dir="ltr">
                {{ remaining() }}
              </p>
            </div>
          </div>
          <p class="mt-2 text-xs text-gray-500">
            جميع المبالغ بعملة الفاتورة: <span class="data-mono">{{ symbol(currency()) }}</span>
          </p>
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
          <app-button type="submit" [loading]="saving()">إصدار فاتورة الشراء</app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class PurchaseDialog {
  private readonly store = inject(PurchasesStore);
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
  readonly currencyOptions = computed(() =>
    this.reference.currencies().map((option: CurrencyReference) => ({
      code: option.code,
      symbol: option.symbol,
    })),
  );

  readonly draft = signal({ sellerName: '', sellerIdNumber: '', sellerYearOfBirth: '', sellerPhone: '', sellerAddress: '', notes: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.sellerName, { message: 'اسم البائع مطلوب.' });
    required(schema.sellerIdNumber, { message: 'رقم الهوية مطلوب.' });
  });

  readonly items = signal<PurchaseItemDraft[]>([]);
  readonly itemsError = signal('');
  readonly employeeId = signal('');
  readonly employeeError = signal(false);
  readonly date = signal('');
  readonly currency = signal('JOD');
  readonly totalAmount = signal('');
  readonly paymentMethod = signal<'1' | '2'>('1');
  readonly paymentLegsEnabled = signal(false);
  readonly amountPaid = signal('');
  readonly accountId = signal('');
  readonly sellerAccountNumber = signal('');
  readonly legs = signal<PaymentLegDraft[]>([]);
  readonly legsError = signal('');
  readonly formError = signal('');

  readonly activeEmployees = computed(() =>
    this.employees().filter((employee) => employee.isActive !== false),
  );
  readonly activeCategories = computed(() =>
    this.categories().filter((category) => category.isActive !== false),
  );
  readonly activeAccounts = computed(() =>
    this.accounts().filter((account) => account.isActive !== false),
  );

  /** Accounts usable by the current single payment method (Cash/Bank) + invoice currency. */
  readonly matchingAccounts = computed(() => {
    const method = this.paymentMethod() === '2' ? 'Bank' : 'Cash';
    return this.activeAccounts().filter(
      (account) => account.currency === this.currency() && account.accountType === method,
    );
  });

  readonly legsTotal = computed(() =>
    round3(
      this.legs().reduce((sum, leg) => {
        const amount = Number(leg.amount);
        if (!Number.isFinite(amount) || leg.currency === '') {
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

  readonly karatTotals = computed(() => {
    const totals = new Map<number, number>();
    for (const item of this.items()) {
      const weight = Number(item.weight);
      if (Number.isFinite(weight) && weight > 0) {
        totals.set(item.karat, (totals.get(item.karat) ?? 0) + weight);
      }
    }
    return [...totals.entries()]
      .sort((a, b) => b[0] - a[0])
      .map(([karat, weight]) => ({ karat, weight: weight.toFixed(3) }));
  });

  readonly trashIcon = resolveIcon('trash-2');
  readonly symbol = symbol;
  readonly Number = Number;

  constructor() {
    void this.reference.ensureLoaded();
    void this.store.ensureEmployees();
    void this.store.ensureCategories();
    void this.store.ensureAccounts();
    void this.store.loadNextNumber();

    // Keep the single-payment account in sync with currency/method/loaded accounts —
    // the API requires an accountId even for cash (mirrors the MVC auto-selection).
    effect(() => {
      const base = this.currency();
      const method = this.paymentMethod();
      const matching = this.matchingAccounts();
      void base;
      void method;
      void matching;
      if (!this.paymentLegsEnabled()) {
        const current = this.accountId();
        const first = matching[0];
        if (first !== undefined && !matching.some((account) => account.id === current)) {
          this.accountId.set(first.id ?? '');
        } else if (matching.length === 0 && current !== '') {
          this.accountId.set('');
        }
      }
    });

    effect(() => {
      if (this.open()) {
        this.draft.set({ sellerName: '', sellerIdNumber: '', sellerYearOfBirth: '', sellerPhone: '', sellerAddress: '', notes: '' });
        this.items.set([{ categoryId: '', karat: 21, weight: '', pricePerGram: '' }]);
        this.itemsError.set('');
        this.employeeId.set('');
        this.employeeError.set(false);
        this.date.set(toLocalDateInput(new Date()));
        this.currency.set('JOD');
        this.totalAmount.set('');
        this.paymentMethod.set('1');
        this.paymentLegsEnabled.set(false);
        this.amountPaid.set('0');
        this.accountId.set('');
        this.sellerAccountNumber.set('');
        this.legs.set([{ accountId: '', accountType: '', currency: '', amount: '', rate: '' }]);
        this.legsError.set('');
        this.formError.set('');
        this.store.clearSaveError();
      }
    });
  }

  itemTotal(index: number): string {
    const item = this.items()[index];
    if (item === undefined) {
      return '0.000';
    }
    const weight = Number(item.weight);
    const price = Number(item.pricePerGram);
    return Number.isFinite(weight) && Number.isFinite(price) ? (weight * price).toFixed(3) : '0.000';
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
    if (leg === undefined || leg.currency === '') {
      return '0.000';
    }
    const amount = Number(leg.amount);
    if (!Number.isFinite(amount) || amount <= 0) {
      return '0.000';
    }
    const rate = leg.currency === this.currency() ? 1 : Number(leg.rate);
    if (!Number.isFinite(rate) || rate <= 0) {
      return '0.000';
    }
    return round3(amount * rate).toFixed(3);
  }

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
      this.amountPaid.set('0');
    }
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  private errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onSubmit(): void {
    submit(this.draftForm, async () => {
      this.formError.set('');
      this.employeeError.set(false);
      this.itemsError.set('');
      this.legsError.set('');

      if (this.employeeId() === '') {
        this.employeeError.set(true);
        this.formError.set('اختر الموظف المشتري للفاتورة.');
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
      } else if (this.accountId() === '') {
        this.formError.set(
          this.accountsError() !== null
            ? 'تعذّر تحميل الحسابات المالية — يلزم اختيار حساب الدفع.'
            : 'لا يوجد حساب مطابق لعملة وطريقة الدفع المحددتين — أضف حساباً في شاشة المالية.',
        );
        return;
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
      sellerName: draft.sellerName.trim(),
      sellerPhone: draft.sellerPhone.trim() || null,
      sellerIdNumber: draft.sellerIdNumber.trim(),
      sellerYearOfBirth: draft.sellerYearOfBirth.trim() === ''
        ? null
        : Number(draft.sellerYearOfBirth),
      sellerAddress: draft.sellerAddress.trim() || null,
      employeeId: this.employeeId(),
      currency: base,
      date: new Date(this.date()).toISOString(),
      totalAmount: total,
      amountPaid: round3(paid),
      paymentMethod: legsEnabled
        ? (this.legs()[0]?.accountType === 'Bank' ? 2 : 1)
        : Number(this.paymentMethod()),
      accountId: legsEnabled
        ? (this.legs()[0]?.accountId ?? this.accountId())
        : this.accountId(),
      sellerAccountNumber: legsEnabled
        ? null
        : (this.sellerAccountNumber().trim() || null),
      notes: draft.notes.trim() || null,
      items: this.items().map((item) => ({
        categoryId: item.categoryId || null,
        karat: item.karat,
        weightInGrams: Number(item.weight),
        pricePerGram: Number(item.pricePerGram),
      })),
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