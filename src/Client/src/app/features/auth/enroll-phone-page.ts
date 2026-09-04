import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '../../core/auth/auth-api.service';
import { PhoneAuthService } from '../../core/auth/phone-auth.service';
import { ToastStore } from '../../core/toast/toast-store';
import { Button } from '../../shared/ui';

@Component({
  selector: 'app-enroll-phone-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField, RouterLink],
  template: `
    <main class="grid min-h-dvh place-items-center bg-surface px-4 py-8" dir="rtl">
      <div class="w-full max-w-md rounded-2xl border bg-white p-6 shadow">
        <h1 class="text-xl font-bold">تفعيل المصادقة الثنائية</h1>
        <p class="mt-2 text-sm text-gray-600">المصادقة الثنائية إلزامية. أدخل رقم هاتفك الفلسطيني (+970) لتوثيقه عبر Firebase.</p>
        <div id="recaptcha-container"></div>
        <form class="mt-6 space-y-4" (submit)="onEnroll(); $event.preventDefault()">
          <label class="block text-sm font-medium">رقم الهاتف (E.164)</label>
          <input dir="ltr" [formField]="enrollForm.phone" placeholder="+970592990484" class="w-full rounded border px-3 py-2 text-left" />
          <app-button type="submit" [loading]="submitting()" class="w-full">إرسال رمز التحقق وتفعيل</app-button>
        </form>
        @if (recoveryCodes().length > 0) {
          <div class="mt-6 rounded border border-amber-200 bg-amber-50 p-4">
            <p class="text-sm font-bold text-amber-800">رموز الاسترداد — احفظها في مكان آمن (تظهر مرة واحدة):</p>
            <ul class="mt-2 grid grid-cols-2 gap-2 font-mono text-sm">
              @for (c of recoveryCodes(); track c) { <li class="rounded bg-white px-2 py-1 border">{{ c }}</li> }
            </ul>
            <a routerLink="/login" class="mt-4 inline-block text-sm font-bold text-gold underline">العودة لتسجيل الدخول</a>
          </div>
        }
      </div>
    </main>
  `,
})
export class EnrollPhonePage {
  private readonly api = inject(AuthApi);
  private readonly phone = inject(PhoneAuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastStore);

  readonly creds = signal({ phone: '+970592990484' });
  readonly enrollForm = form(this.creds, (s) => required(s.phone, { message: 'رقم الهاتف مطلوب' }));
  readonly submitting = signal(false);
  readonly recoveryCodes = signal<string[]>([]);

  async onEnroll(): Promise<void> {
    submit(this.enrollForm, async () => {
      this.submitting.set(true);
      try {
        const tempTicket = this.route.snapshot.queryParamMap.get('tempTicket') ?? sessionStorage.getItem('tempTicket') ?? '';
        const isHost = this.route.snapshot.queryParamMap.get('isHost') === 'true';
        if (!tempTicket) throw new Error('Missing tempTicket');
        const idToken = await this.phone.sendCode(this.creds().phone, isHost);
        const res = await firstValueFrom(this.api.setupPhone2fa(tempTicket, idToken, isHost));
        this.recoveryCodes.set(res.recoveryCodes);
        this.toast.success('تم تفعيل المصادقة الثنائية');
        sessionStorage.removeItem('tempTicket');
      } catch (e: unknown) {
        const msg = e instanceof Error ? e.message : 'فشل التفعيل';
        this.toast.error(msg);
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
