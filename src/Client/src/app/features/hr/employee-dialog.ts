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
import { Button, Dialog } from '../../shared/ui';
import { HrStore } from './hr-store';
import type { EmployeeResponse } from './hr-api.service';

function firstValidationMessage(error: ApiError): string | null {
  for (const messages of Object.values(error.validation ?? {})) {
    const message = messages[0];
    if (message !== undefined) return message;
  }
  return null;
}

const ROLE_OPTIONS: ReadonlyArray<{ value: number; label: string }> = [
  { value: 1, label: 'مدير النظام' },
  { value: 2, label: 'مدير المتجر' },
  { value: 3, label: 'محاسب' },
  { value: 4, label: 'موظف مبيعات' },
];

const CURRENCY_OPTIONS: ReadonlyArray<{ value: number; code: string }> = [
  { value: 1, code: 'JOD' },
  { value: 2, code: 'USD' },
  { value: 3, code: 'ILS' },
];

const CYCLE_OPTIONS: ReadonlyArray<{ value: number; label: string }> = [
  { value: 1, label: 'يومي' },
  { value: 2, label: 'اسبوعي' },
  { value: 3, label: 'شهري' },
];

/**
 * P3.12 — create or edit an employee. Mirrors `CreateEmployeeRequest` /
 * `UpdateEmployeeRequest` key-for-key. Creation can optionally link an existing
 * tenant user (`existingUserId`) or create a new one (`newUserEmail`/`newUserPassword`);
 * the unlinked-users list is best-effort. No `tenantId` ever sent.
 */
@Component({
  selector: 'app-employee-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      [title]="employee() ? 'تعديل الموظف' : 'موظف جديد'"
      subtitle="الموظفون"
      icon="users"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="emp-firstName"
              >الاسم الأول <span class="text-error">*</span></label
            >
            <input
              id="emp-firstName"
              type="text"
              [formField]="draftForm.firstName"
              placeholder="مثال: أحمد"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (draftForm.firstName().touched() && draftForm.firstName().errors().length > 0) {
              <p class="mt-1 text-xs text-error">{{ draftForm.firstName().errors()[0].message }}</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="emp-lastName"
              >الاسم الأخير <span class="text-error">*</span></label
            >
            <input
              id="emp-lastName"
              type="text"
              [formField]="draftForm.lastName"
              placeholder="مثال: خليل"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (draftForm.lastName().touched() && draftForm.lastName().errors().length > 0) {
              <p class="mt-1 text-xs text-error">{{ draftForm.lastName().errors()[0].message }}</p>
            }
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700">الدور</label>
          <div class="grid grid-cols-2 gap-2">
            @for (option of roleOptions; track option.value) {
              <label class="cursor-pointer">
                <input
                  class="peer sr-only"
                  type="radio"
                  name="emp-role"
                  [value]="option.value"
                  [checked]="role() === option.value"
                  (change)="role.set(option.value)"
                />
                <span
                  class="block rounded-input border border-gray-300 py-2 text-center text-sm transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60 peer-checked:ring-1 peer-checked:ring-gold"
                  >{{ option.label }}</span
                >
              </label>
            }
          </div>
        </div>

        <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="emp-salary"
              >الراتب <span class="text-error">*</span></label
            >
            <input
              id="emp-salary"
              type="number"
              dir="ltr"
              step="0.01"
              min="0"
              [value]="salary()"
              (input)="salary.set($any($event.target).value)"
              placeholder="0.00"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (invalidSalary()) {
              <p class="mt-1 text-xs text-error">الراتب يجب أن يكون أكبر من صفر.</p>
            }
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700">العملة</label>
            <div class="flex gap-1">
              @for (option of currencyOptions; track option.value) {
                <label class="flex-1 cursor-pointer">
                  <input
                    class="peer sr-only"
                    type="radio"
                    name="emp-currency"
                    [value]="option.value"
                    [checked]="currency() === option.value"
                    (change)="currency.set(option.value)"
                  />
                  <span
                    class="block rounded-input border border-gray-300 py-1.5 text-center text-sm transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60"
                    >{{ option.code }}</span
                  >
                </label>
              }
            </div>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700">دورة القبض</label>
            <div class="flex gap-1">
              @for (option of cycleOptions; track option.value) {
                <label class="flex-1 cursor-pointer">
                  <input
                    class="peer sr-only"
                    type="radio"
                    name="emp-cycle"
                    [value]="option.value"
                    [checked]="salaryCycle() === option.value"
                    (change)="salaryCycle.set(option.value)"
                  />
                  <span
                    class="block rounded-input border border-gray-300 py-1.5 text-center text-xs transition-colors peer-checked:border-gold peer-checked:bg-gold-container/60"
                    >{{ option.label }}</span
                  >
                </label>
              }
            </div>
            @if (salaryCycle() === 1) {
              <p class="mt-1 text-xs text-amber-700">الراتب اليومي لا يدعم دفع الرواتب عبر النظام.</p>
            }
          </div>
        </div>

        @if (employee()) {
          <label class="flex items-center gap-2">
            <input
              type="checkbox"
              [checked]="isActive()"
              (change)="isActive.set($any($event.target).checked)"
              class="h-4 w-4 rounded border-gray-300 text-gold focus:ring-gold"
            />
            <span class="text-sm text-gray-700">نشط</span>
          </label>
        } @else {
          <!-- Create-only: link to user -->
          <div class="rounded-input border border-gray-200 bg-gray-50 px-4 py-3">
            <label class="flex items-center gap-2">
              <input
                type="checkbox"
                [checked]="connectToUser()"
                (change)="connectToUser.set($any($event.target).checked)"
                class="h-4 w-4 rounded border-gray-300 text-gold focus:ring-gold"
              />
              <span class="text-sm font-medium text-gray-700">ربط المستخدم</span>
            </label>

            @if (connectToUser()) {
              <div class="mt-3 space-y-3">
                <div class="flex gap-2">
                  <label class="flex-1 cursor-pointer">
                    <input
                      class="peer sr-only"
                      type="radio"
                      name="user-mode"
                      [value]="'existing'"
                      [checked]="userMode() === 'existing'"
                      (change)="userMode.set('existing')"
                    />
                    <span
                      class="block rounded-input border border-gray-300 bg-white py-2 text-center text-sm peer-checked:border-gold peer-checked:bg-gold-container/50"
                      >مستخدم موجود</span
                    >
                  </label>
                  <label class="flex-1 cursor-pointer">
                    <input
                      class="peer sr-only"
                      type="radio"
                      name="user-mode"
                      [value]="'new'"
                      [checked]="userMode() === 'new'"
                      (change)="userMode.set('new')"
                    />
                    <span
                      class="block rounded-input border border-gray-300 bg-white py-2 text-center text-sm peer-checked:border-gold peer-checked:bg-gold-container/50"
                      >مستخدم جديد</span
                    >
                  </label>
                </div>

                @if (userMode() === 'existing') {
                  @if (unlinkedUsersLoading()) {
                    <p class="text-xs text-gray-500">جاري التحميل...</p>
                  } @else {
                    <select
                      id="emp-existingUserId"
                      [value]="existingUserId()"
                      (change)="existingUserId.set($any($event.target).value)"
                      class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                    >
                      <option value="">اختر المستخدم</option>
                      @for (user of unlinkedUsers(); track user.id) {
                        <option [value]="user.id">{{ user.fullName }} — {{ user.email }}</option>
                      }
                    </select>
                    @if (unlinkedUsers().length === 0) {
                      <p class="text-xs text-gray-500">لا يوجد مستخدمون غير مرتبطين.</p>
                    }
                  }
                } @else {
                  <input
                    id="emp-newEmail"
                    type="email"
                    dir="ltr"
                    [value]="newUserEmail()"
                    (input)="newUserEmail.set($any($event.target).value)"
                    placeholder="newuser@example.com"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                  <input
                    id="emp-newPassword"
                    type="password"
                    dir="ltr"
                    [value]="newUserPassword()"
                    (input)="newUserPassword.set($any($event.target).value)"
                    placeholder="كلمة المرور (≥ 8)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                  <input
                    id="emp-newConfirmPassword"
                    type="password"
                    dir="ltr"
                    [value]="newUserConfirmPassword()"
                    (input)="newUserConfirmPassword.set($any($event.target).value)"
                    placeholder="تأكيد كلمة المرور"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                  @if (confirmPasswordError()) {
                    <p class="mt-1 text-xs text-error" role="alert">{{ confirmPasswordError() }}</p>
                  }
                }
              </div>
            }
          </div>
        }

        @if (saveError(); as error) {
          <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ errorMessage(error) }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button variant="secondary" type="button" [disabled]="saving()" (clicked)="onDismiss()">
            إلغاء
          </app-button>
          <app-button
            type="submit"
            [loading]="saving()"
            [disabled]="invalidSalary() || salaryCycle() === 1 && !employee()"
          >
            {{ employee() ? 'حفظ التعديلات' : 'إنشاء الموظف' }}
          </app-button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class EmployeeDialog {
  private readonly store = inject(HrStore);

  readonly open = input(false);
  readonly employee = input<EmployeeResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;
  readonly unlinkedUsers = this.store.unlinkedUsers;
  readonly unlinkedUsersLoading = signal(false);

  readonly roleOptions = ROLE_OPTIONS;
  readonly currencyOptions = CURRENCY_OPTIONS;
  readonly cycleOptions = CYCLE_OPTIONS;

  readonly role = signal(4);
  readonly currency = signal(1);
  readonly salaryCycle = signal(3);
  readonly salary = signal('');
  readonly isActive = signal(true);

  readonly connectToUser = signal(false);
  readonly userMode = signal<'existing' | 'new'>('existing');
  readonly existingUserId = signal('');
  readonly newUserEmail = signal('');
  readonly newUserPassword = signal('');
  readonly newUserConfirmPassword = signal('');
  readonly confirmPasswordError = signal<string | null>(null);

  readonly draft = signal({ firstName: '', lastName: '' });
  readonly draftForm = form(this.draft, (schema) => {
    required(schema.firstName, { message: 'الاسم الأول مطلوب.' });
    required(schema.lastName, { message: 'الاسم الأخير مطلوب.' });
  });

  readonly invalidSalary = computed(() => Number(this.salary()) <= 0);

  constructor() {
    effect(() => {
      if (this.open()) {
        const employee = this.employee();
        if (employee) {
          this.draft.set({
            firstName: (employee.firstName as string) ?? '',
            lastName: (employee.lastName as string) ?? '',
          });
          this.role.set((employee.role as number) ?? 4);
          this.currency.set((employee.currency as number) ?? 1);
          this.salaryCycle.set((employee.salaryCycle as number) ?? 3);
          this.salary.set(String(employee.salary ?? ''));
          this.isActive.set(!!employee.isActive);
        } else {
          this.draft.set({ firstName: '', lastName: '' });
          this.role.set(4);
          this.currency.set(1);
          this.salaryCycle.set(3);
          this.salary.set('');
          this.isActive.set(true);
          this.connectToUser.set(false);
          this.userMode.set('existing');
          this.existingUserId.set('');
          this.newUserEmail.set('');
          this.newUserPassword.set('');
          this.newUserConfirmPassword.set('');
          this.confirmPasswordError.set(null);
        }
        this.store.clearSaveError();
        if (!employee) {
          void this.store.ensureUnlinkedUsers();
        }
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
      if (this.invalidSalary()) return;
      if (!this.validateNewUserPassword()) return;
      const { firstName, lastName } = this.draft();
      const salary = Number(this.salary());
      const existing = this.employee();
      if (existing) {
        const ok = await this.store.updateEmployee(existing.id as string, {
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          role: this.role(),
          salary,
          currency: this.currency(),
          salaryCycle: this.salaryCycle(),
          isActive: this.isActive(),
        });
        if (ok) {
          this.saved.emit();
          this.onDismiss();
        }
      } else {
        const connect = this.connectToUser();
        const ok = await this.store.createEmployee({
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          role: this.role(),
          salary,
          currency: this.currency(),
          salaryCycle: this.salaryCycle(),
          connectToUser: connect,
          existingUserId: connect && this.userMode() === 'existing' ? this.existingUserId() || null : null,
          newUserEmail: connect && this.userMode() === 'new' ? this.newUserEmail().trim() || null : null,
          newUserPassword: connect && this.userMode() === 'new' ? this.newUserPassword() || null : null,
        });
        if (ok) {
          this.saved.emit();
          this.onDismiss();
        }
      }
    });
  }

  private validateNewUserPassword(): boolean {
    this.confirmPasswordError.set(null);
    if (!this.connectToUser() || this.userMode() !== 'new') return true;
    const pwd = this.newUserPassword();
    const confirm = this.newUserConfirmPassword();
    if (pwd.trim().length > 0 && confirm.trim().length === 0) {
      this.confirmPasswordError.set('تأكيد كلمة المرور مطلوب.');
      return false;
    }
    if (pwd !== confirm) {
      this.confirmPasswordError.set('كلمة المرور وتأكيدها غير متطابقتين');
      return false;
    }
    return true;
  }
}
