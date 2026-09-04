import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '../../core/auth/auth-api.service';
import { AuthStore } from '../../core/auth/auth-store';
import { PhoneAuthService } from '../../core/auth/phone-auth.service';
import { ToastStore } from '../../core/toast/toast-store';
import { Button } from '../../shared/ui';
import { buildNavItems } from '../../core/navigation/nav-items';

@Component({
  selector: 'app-verify-2fa-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button],
  template: `
    <main class="grid min-h-dvh place-items-center bg-surface px-4 py-8" dir="rtl">
      <div class="w-full max-w-md rounded-2xl border bg-white p-6 shadow">
        <h1 class="text-xl font-bold">التحقق بخطوتين</h1>
        <p class="mt-2 text-sm text-gray-600">تم إرسال رمز إلى هاتفك (+970***). أدخل الرمز أو استخدم رمز الاسترداد.</p>
        <div id="recaptcha-container"></div>
        <div class="mt-6 space-y-3">
          <button type="button" (click)="onVerifyPhone()" [disabled]="submitting()" class="w-full rounded bg-gold px-4 py-2 font-bold text-white">تحقق عبر الهاتف (Firebase)</button>
          <div class="flex gap-2">
            <input [(value)]="recoveryCode" placeholder="رمز الاسترداد" class="flex-1 rounded border px-3 py-2" />
            <button type="button" (click)="onVerifyRecovery()" class="rounded border px-4 py-2">استرداد</button>
          </div>
        </div>
        <p class="mt-4 text-xs text-gray-500">هاتف موثق: {{ masked() }}</p>
      </div>
    </main>
  `,
})
export class Verify2faPage {
  private readonly api = inject(AuthApi);
  private readonly auth = inject(AuthStore);
  private readonly phone = inject(PhoneAuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastStore);

  readonly submitting = signal(false);
  readonly masked = signal(this.route.snapshot.queryParamMap.get('masked') ?? '');
  readonly recoveryCodeSig = signal('');
  recoveryCode = '';

  async onVerifyPhone(): Promise<void> {
    this.submitting.set(true);
    try {
      const tempTicket = this.route.snapshot.queryParamMap.get('tempTicket') ?? sessionStorage.getItem('tempTicket') ?? '';
      const isHost = this.route.snapshot.queryParamMap.get('isHost') === 'true';
      const storedPhone = this.route.snapshot.queryParamMap.get('phone') ?? '+970592990484';
      const idToken = await this.phone.sendCode(storedPhone, isHost);
      await firstValueFrom(this.api.verifyPhone2fa(tempTicket, idToken, isHost));
      this.toast.success('تم التحقق');
      sessionStorage.removeItem('tempTicket');
      if (isHost) await this.router.navigateByUrl('/host/admin/tenants');
      else {
        await this.auth.restoreSessions();
        await this.router.navigateByUrl(buildNavItems(this.auth.permissions())[0]?.route ?? '/');
      }
    } catch {
      this.toast.error('رمز غير صحيح');
    } finally {
      this.submitting.set(false);
    }
  }

  async onVerifyRecovery(): Promise<void> {
    if (!this.recoveryCode) return;
    this.submitting.set(true);
    try {
      const tempTicket = this.route.snapshot.queryParamMap.get('tempTicket') ?? sessionStorage.getItem('tempTicket') ?? '';
      const isHost = this.route.snapshot.queryParamMap.get('isHost') === 'true';
      await firstValueFrom(this.api.verifyRecovery(tempTicket, this.recoveryCode, isHost));
      this.toast.success('تم التحقق برمز الاسترداد');
      sessionStorage.removeItem('tempTicket');
      if (isHost) await this.router.navigateByUrl('/host/admin/tenants');
      else {
        await this.auth.restoreSessions();
        await this.router.navigateByUrl('/');
      }
    } catch {
      this.toast.error('رمز استرداد غير صالح');
    } finally {
      this.submitting.set(false);
    }
  }
}
