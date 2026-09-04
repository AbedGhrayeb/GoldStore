import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormField, email, form, required, submit } from '@angular/forms/signals';

import type { ApiError } from '../../core/http/api-error';
import { Button, Dialog } from '../../shared/ui';
import { UsersStore, type UserResponse } from './users-store';

export type UserFormMode = 'create' | 'edit';

const MIN_PASSWORD_LENGTH = 8;

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
 * P3.5 — create/edit dialog for one store user. Create mode asks for email + password;
 * edit mode is profile-only (the server permits editing only the current user) with an
 * optional password — a blank password keeps the current one. Submission goes through
 * {@link UsersStore}; server errors (duplicate email, validation) render inline.
 */
@Component({
  selector: 'app-user-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, FormField],
  template: `
    <app-dialog
      [open]="open()"
      [title]="mode() === 'edit' ? 'تعديل بياناتي' : 'إضافة مستخدم'"
      [subtitle]="'الإعدادات'"
      [icon]="mode() === 'edit' ? 'pencil' : 'user-plus'"
      (openChange)="onDismiss()"
    >
      <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
        @if (mode() === 'create') {
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="user-email"
              >البريد الإلكتروني</label
            >
            <input
              id="user-email"
              type="email"
              dir="ltr"
              autocomplete="off"
              [formField]="userForm.email"
              placeholder="user@example.com"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
            @if (userForm.email().touched() && userForm.email().errors().length > 0) {
              <p class="mt-1 text-xs text-error">{{ userForm.email().errors()[0].message }}</p>
            }
          </div>
        }

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="user-first-name"
            >الاسم الأول</label
          >
          <input
            id="user-first-name"
            type="text"
            autocomplete="off"
            [formField]="userForm.firstName"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (userForm.firstName().touched() && userForm.firstName().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ userForm.firstName().errors()[0].message }}</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="user-last-name"
            >الاسم الأخير</label
          >
          <input
            id="user-last-name"
            type="text"
            autocomplete="off"
            [formField]="userForm.lastName"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (userForm.lastName().touched() && userForm.lastName().errors().length > 0) {
            <p class="mt-1 text-xs text-error">{{ userForm.lastName().errors()[0].message }}</p>
          }
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="user-phone">رقم الهاتف</label>
            <input
              id="user-phone"
              type="tel"
              dir="ltr"
              autocomplete="off"
              [formField]="userForm.phoneNumber"
              placeholder="+962 7XXXXXXXX"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="user-whatsapp">رقم الواتساب</label>
            <input
              id="user-whatsapp"
              type="tel"
              dir="ltr"
              autocomplete="off"
              [formField]="userForm.whatsappNumber"
              placeholder="+962 7XXXXXXXX"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="user-password"
            >كلمة المرور</label
          >
          <input
            id="user-password"
            type="password"
            dir="ltr"
            [attr.autocomplete]="mode() === 'create' ? 'new-password' : 'off'"
            [formField]="userForm.password"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (mode() === 'edit') {
            <p class="mt-1 text-xs text-gray-500">اتركها فارغة للإبقاء على كلمة المرور الحالية.</p>
          }
          @if (passwordError()) {
            <p class="mt-1 text-xs text-error" role="alert">{{ passwordError() }}</p>
          }
        </div>

        @if (mode() === 'create') {
          <div>
            <label class="mb-2 block text-sm font-medium text-gray-700" for="user-confirm-password"
              >تأكيد كلمة المرور</label
            >
            <input
              id="user-confirm-password"
              type="password"
              dir="ltr"
              autocomplete="new-password"
              [formField]="userForm.confirmPassword"
              class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
              placeholder="أعد إدخال كلمة المرور"
            />
            @if (confirmPasswordError()) {
              <p class="mt-1 text-xs text-error" role="alert">{{ confirmPasswordError() }}</p>
            }
          </div>
        }

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
export class UserFormDialog {
  private readonly store = inject(UsersStore);

  readonly open = input(false);
  readonly mode = input<UserFormMode>('create');
  /** Edit target; null in create mode. */
  readonly user = input<UserResponse | null>(null);

  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly draft = signal({ email: '', firstName: '', lastName: '', password: '', confirmPassword: '', phoneNumber: '', whatsappNumber: '' });
  readonly passwordError = signal<string | null>(null);
  readonly confirmPasswordError = signal<string | null>(null);

  readonly userForm = form(this.draft, (schema) => {
    required(schema.email, { message: 'البريد الإلكتروني مطلوب.' });
    email(schema.email, { message: 'أدخل بريداً إلكترونياً صحيحاً.' });
    required(schema.firstName, { message: 'الاسم الأول مطلوب.' });
    required(schema.lastName, { message: 'الاسم الأخير مطلوب.' });
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        const user = this.user() as unknown as { email?: string; firstName?: string; lastName?: string; phoneNumber?: string | null; whatsappNumber?: string | null } | null;
        this.draft.set({
          email: user?.email ?? '',
          firstName: user?.firstName ?? '',
          lastName: user?.lastName ?? '',
          password: '',
          confirmPassword: '',
          phoneNumber: user?.phoneNumber ?? '',
          whatsappNumber: user?.whatsappNumber ?? '',
        });
        this.passwordError.set(null);
        this.confirmPasswordError.set(null);
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
    submit(this.userForm, async () => {
      if (!this.validatePassword()) {
        return;
      }
      const { firstName, lastName, password, phoneNumber, whatsappNumber } = this.draft();
      const ok =
        this.mode() === 'edit' && this.user() !== null
          ? await this.store.updateUser(this.user()!.id ?? '', {
              firstName: firstName.trim(),
              lastName: lastName.trim(),
              password: password.trim() || null,
              phoneNumber: phoneNumber.trim() || null,
              whatsappNumber: whatsappNumber.trim() || null,
            })
          : await this.store.createUser({
              email: this.draft().email.trim(),
              firstName: firstName.trim(),
              lastName: lastName.trim(),
              password: password.trim(),
              phoneNumber: phoneNumber.trim() || null,
              whatsappNumber: whatsappNumber.trim() || null,
            });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    });
  }

  private validatePassword(): boolean {
    const password = this.draft().password;
    const confirmPassword = this.draft().confirmPassword;
    this.passwordError.set(null);
    this.confirmPasswordError.set(null);
    if (this.mode() === 'create') {
      if (password.trim().length === 0) {
        this.passwordError.set('كلمة المرور مطلوبة.');
        return false;
      }
      if (password.trim().length < MIN_PASSWORD_LENGTH) {
        this.passwordError.set(`كلمة المرور يجب أن تكون ${MIN_PASSWORD_LENGTH} أحرف على الأقل.`);
        return false;
      }
      if (confirmPassword.trim().length === 0) {
        this.confirmPasswordError.set('تأكيد كلمة المرور مطلوب.');
        return false;
      }
      if (password !== confirmPassword) {
        this.confirmPasswordError.set('كلمة المرور وتأكيدها غير متطابقتين');
        return false;
      }
      return true;
    }
    if (password.trim().length > 0 && password.trim().length < MIN_PASSWORD_LENGTH) {
      this.passwordError.set(
        `كلمة المرور الجديدة يجب أن تكون ${MIN_PASSWORD_LENGTH} أحرف على الأقل.`,
      );
      return false;
    }
    return true;
  }
}
