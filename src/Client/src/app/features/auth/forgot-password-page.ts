import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '../../core/auth/auth-api.service';
import { PhoneAuthService } from '../../core/auth/phone-auth.service';
import { ToastStore } from '../../core/toast/toast-store';
import { Button } from '../../shared/ui';

@Component({
  selector: 'app-forgot-password-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField, RouterLink],
  template: `
    <main class="grid min-h-dvh place-items-center bg-surface px-4 py-8" dir="rtl">
      <div class="w-full max-w-md rounded-2xl border bg-white p-6 shadow">
        <h1 class="text-xl font-bold">نسيت كلمة المرور</h1>
        <p class="mt-2 text-sm text-gray-600">أدخل بريدك أو هاتفك (+970)، ثم وثّق هاتفك عبر Firebase لإعادة التعيين.</p>
        <div id="recaptcha-container"></div>
        <form class="mt-6 space-y-4" (submit)="onReset(); $event.preventDefault()">
          <input [formField]="credsForm.emailOrPhone" placeholder="admin@goldstore أو +970592990484" class="w-full rounded border px-3 py-2" />
          <input type="password" [formField]="credsForm.newPassword" placeholder="كلمة المرور الجديدة (≥8)" class="w-full rounded border px-3 py-2" />
          <input [formField]="credsForm.phone" placeholder="رقم الهاتف للتوثيق +970..." dir="ltr" class="w-full rounded border px-3 py-2 text-left" />
          <app-button type="submit" [loading]="submitting()" class="w-full">إعادة التعيين عبر الهاتف</app-button>
        </form>
        <a routerLink="/login" class="mt-4 inline-block text-sm text-gold underline">العودة</a>
      </div>
    </main>
  `,
})
export class ForgotPasswordPage {
  private readonly api = inject(AuthApi);
  private readonly phone = inject(PhoneAuthService);
  private readonly toast = inject(ToastStore);

  readonly creds = signal({ emailOrPhone: '', newPassword: '', phone: '+970592990484' });
  readonly credsForm = form(this.creds, (s) => {
    required(s.emailOrPhone, { message: 'مطلوب' });
    required(s.newPassword, { message: 'مطلوبة' });
    required(s.phone, { message: 'مطلوب' });
  });
  readonly submitting = signal(false);

  async onReset(): Promise<void> {
    submit(this.credsForm, async () => {
      this.submitting.set(true);
      try {
        const isHost = location.pathname.startsWith('/host');
        const idToken = await this.phone.sendCode(this.creds().phone, isHost);
        await firstValueFrom(this.api.forgotPasswordPhone(this.creds().emailOrPhone, idToken, this.creds().newPassword, isHost));
        this.toast.success('تمت إعادة التعيين، سجل الدخول الآن');
      } catch {
        this.toast.error('فشل — تحقق من الهاتف والبريد');
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
