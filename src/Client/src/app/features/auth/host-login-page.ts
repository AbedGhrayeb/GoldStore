import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormField, email, form, required, submit } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

import { AuthStore } from '../../core/auth/auth-store';
import { ToastStore } from '../../core/toast/toast-store';
import { Button } from '../../shared/ui';
import { resolveIcon } from '../../shared/ui/icon-registry';

@Component({
  selector: 'app-host-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField, LucideAngularModule],
  template: `
    <main class="grid min-h-dvh place-items-center bg-[#0f1115] px-4 py-8" dir="rtl">
      <!-- ambient glow -->
      <div class="pointer-events-none fixed inset-0" aria-hidden="true">
        <div class="absolute inset-0 bg-gradient-to-br from-amber-900/20 via-transparent to-gold/10"></div>
        <div class="absolute left-1/2 top-0 h-[600px] w-[800px] -translate-x-1/2 rounded-full bg-gold/10 blur-[120px]"></div>
      </div>

      <div class="relative flex w-full max-w-5xl overflow-hidden rounded-2xl border border-white/10 bg-white shadow-2xl">
        <!-- Showcase — sovereign vault -->
        <div class="relative hidden w-[46%] flex-col justify-between overflow-hidden bg-gradient-to-br from-gray-900 via-gray-800 to-[#1a150f] p-8 text-white lg:flex">
          <div class="pointer-events-none absolute inset-0 opacity-10" aria-hidden="true" style="background-image: radial-gradient(circle at 1px 1px, #D4AF37 1px, transparent 0); background-size: 22px 22px;"></div>
          <div class="relative">
            <span class="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/10 px-3 py-1 text-xs font-medium tracking-widest text-amber-200">
              <lucide-icon [img]="shieldIcon" [size]="14" />
              وحدة تحكم المضيف
            </span>
            <h2 class="mt-6 text-2xl font-bold leading-tight">
              سيادة المنصة<br />
              <span class="bg-gradient-to-r from-gold to-amber-300 bg-clip-text text-transparent">في مكان واحد</span>
            </h2>
            <p class="mt-3 text-sm leading-6 text-white/70">
              إدارة المستأجرين، دورة حياة الحالات، والمطابقة المحاسبية — طبقة السيادة محمية بجلسة مضيف مشفّرة.
            </p>
          </div>

          <div class="relative space-y-4">
            <div class="rounded-xl border border-white/10 bg-white/5 p-4 backdrop-blur">
              <div class="flex items-center gap-3">
                <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold text-white"><lucide-icon [img]="serverIcon" [size]="16" /></span>
                <div>
                  <p class="text-sm font-semibold">قاعة الخزائن</p>
                  <p class="text-xs text-white/60">كل مستأجر — خزنة معزولة بأرصدة محسوبة</p>
                </div>
              </div>
              <div class="mt-3 flex gap-2 text-xs">
                <span class="rounded-full bg-white/10 px-2.5 py-1 data-mono">عزل تام</span>
                <span class="rounded-full bg-white/10 px-2.5 py-1 data-mono">تدقيق شامل</span>
                <span class="rounded-full bg-gold/20 px-2.5 py-1 font-medium text-amber-200">مطابقة فورية</span>
              </div>
            </div>
            <p class="flex items-center gap-2 text-xs text-white/50">
              <lucide-icon [img]="lockIcon" [size]="12" />
              جلسة HttpOnly • XSRF محمي • HostOnly
            </p>
          </div>
        </div>

        <!-- Form -->
        <div class="flex flex-1 flex-col justify-center bg-white px-6 py-8 sm:px-8 lg:px-10">
          <div class="mx-auto w-full max-w-md">
            <div class="mb-8 flex items-center gap-3 lg:hidden">
              <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gray-900 text-gold"><lucide-icon [img]="shieldIcon" [size]="18" /></span>
              <span class="text-xs font-bold tracking-widest text-gray-500">HOST CONTROL</span>
            </div>

            <div class="mb-7 flex items-start justify-between gap-3">
              <div>
                <span class="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-gold to-amber-600 text-lg font-bold text-white shadow-sm">ذ</span>
                <h1 class="mt-4 text-2xl font-bold tracking-tight text-gray-900">دخول إدارة المنصة</h1>
                <p class="mt-1 text-sm text-gray-600">للمشرفين على المتاجر والاشتراكات فقط — جلسة المضيف.</p>
              </div>
              <span class="hidden rounded-full bg-amber-50 px-3 py-1 text-xs font-bold tracking-widest text-amber-700 sm:inline">HOST ONLY</span>
            </div>

            <form class="space-y-5" novalidate (submit)="onSubmit(); $event.preventDefault()">
              <div>
                <label class="mb-2 flex items-center gap-1.5 text-sm font-medium text-gray-700" for="email">
                  <lucide-icon [img]="mailIcon" [size]="14" class="text-gray-400" />
                  البريد الإلكتروني
                </label>
                <input
                  id="email"
                  type="email"
                  dir="ltr"
                  autocomplete="username"
                  [formField]="loginForm.email"
                  placeholder="platform@goldstore.app"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition placeholder:text-gray-400 focus:border-gold focus:ring-2 focus:ring-gold/30"
                />
                @if (loginForm.email().touched() && loginForm.email().errors().length > 0) {
                  <p class="mt-1.5 rounded bg-error/10 px-2 py-1 text-xs font-medium text-error">{{ loginForm.email().errors()[0].message }}</p>
                }
              </div>

              <div>
                <label class="mb-2 flex items-center gap-1.5 text-sm font-medium text-gray-700" for="password">
                  <lucide-icon [img]="lockIcon" [size]="14" class="text-gray-400" />
                  كلمة المرور
                </label>
                <input
                  id="password"
                  type="password"
                  dir="ltr"
                  autocomplete="current-password"
                  [formField]="loginForm.password"
                  placeholder="••••••••"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition placeholder:text-gray-400 focus:border-gold focus:ring-2 focus:ring-gold/30"
                />
                @if (loginForm.password().touched() && loginForm.password().errors().length > 0) {
                  <p class="mt-1.5 rounded bg-error/10 px-2 py-1 text-xs font-medium text-error">{{ loginForm.password().errors()[0].message }}</p>
                }
              </div>

              <app-button
                type="submit"
                size="lg"
                [loading]="submitting()"
                [disabled]="loginForm().invalid()"
                class="w-full"
              >
                <lucide-icon [img]="shieldIcon" [size]="16" />
                دخول الإدارة
              </app-button>

              <div class="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2.5 text-xs leading-5 text-amber-800">
                <lucide-icon [img]="infoIcon" [size]="14" class="shrink-0 text-amber-600" />
                هذه البوابة للمضيف فقط. حسابات المتاجر تسجّل عبر
                <a href="/login" class="font-bold underline underline-offset-4">تسجيل دخول المتجر</a>.
              </div>
              <a href="/forgot-password?isHost=true" class="mt-3 block text-center text-xs font-bold text-gold underline">نسيت كلمة المرور (مضيف) — عبر الهاتف</a>
              <div id="recaptcha-container" class="hidden"></div>
            </form>

            <p class="mt-6 text-center text-xs text-gray-400">محمي بـ XSRF وجلسة HttpOnly — ينتهي تلقائياً عند الإغلاق.</p>
          </div>
        </div>
      </div>
    </main>
  `,
})
export class HostLoginPage {
  private readonly auth = inject(AuthStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastStore);

  readonly credentials = signal({ email: '', password: '' });
  readonly loginForm = form(this.credentials, (schema) => {
    required(schema.email, { message: 'البريد الإلكتروني مطلوب.' });
    email(schema.email, { message: 'أدخل بريداً إلكترونياً صحيحاً.' });
    required(schema.password, { message: 'كلمة المرور مطلوبة.' });
  });
  readonly submitting = signal(false);

  readonly shieldIcon = resolveIcon('shield-check');
  readonly serverIcon = resolveIcon('server');
  readonly lockIcon = resolveIcon('shield');
  readonly mailIcon = resolveIcon('info');
  readonly infoIcon = resolveIcon('info');

  onSubmit(): void {
    submit(this.loginForm, async () => {
      this.submitting.set(true);
      try {
        const res = await this.auth.hostLogin(this.credentials().email, this.credentials().password) as unknown as { is2fa?: boolean; requiresEnrollment?: boolean; tempTicket?: string; maskedPhone?: string } | undefined;
        if (res && res.is2fa) {
          sessionStorage.setItem('tempTicket', res.tempTicket ?? '');
          if (res.requiresEnrollment) {
            await this.router.navigate(['/enroll-phone'], { queryParams: { tempTicket: res.tempTicket, isHost: true } });
          } else {
            await this.router.navigate(['/verify-2fa'], { queryParams: { tempTicket: res.tempTicket, masked: res.maskedPhone ?? '', isHost: true, phone: '+970592990484' } });
          }
          return;
        }
        await this.router.navigateByUrl(this.safeReturnUrl('/host/admin/tenants'));
      } catch {
        this.toasts.error('تعذّر تسجيل الدخول إلى إدارة المنصة. تحقّق من البيانات وحاول مجدداً.');
      } finally {
        this.submitting.set(false);
      }
    });
  }

  private safeReturnUrl(fallback: string): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : fallback;
  }
}
