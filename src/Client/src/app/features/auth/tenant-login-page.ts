import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormField, email, form, required, submit } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

import { AuthStore } from '../../core/auth/auth-store';
import { buildNavItems } from '../../core/navigation/nav-items';
import { TenantBrand } from '../../core/shell/tenant-brand';
import { ToastStore } from '../../core/toast/toast-store';
import { Button } from '../../shared/ui';
import { resolveIcon } from '../../shared/ui/icon-registry';

@Component({
  selector: 'app-tenant-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField, TenantBrand, LucideAngularModule],
  template: `
    <main class="grid min-h-dvh place-items-center bg-surface px-4 py-8" dir="rtl">
      <div class="pointer-events-none absolute inset-0 overflow-hidden" aria-hidden="true">
        <div class="absolute -left-20 -top-20 h-[500px] w-[500px] rounded-full bg-gold/10 blur-[100px]"></div>
        <div class="absolute -bottom-20 -right-20 h-[600px] w-[600px] rounded-full bg-amber-100/60 blur-[110px]"></div>
      </div>

      <div class="relative flex w-full max-w-5xl overflow-hidden rounded-2xl border border-amber-200/50 bg-white shadow-xl">
        <!-- Brand showcase -->
        <div class="relative hidden w-[46%] flex-col justify-between overflow-hidden bg-gradient-to-br from-amber-50 via-gold-container/30 to-white p-8 lg:flex">
          <div class="pointer-events-none absolute inset-0 opacity-[0.04]" aria-hidden="true" style="background-image: radial-gradient(circle at 1px 1px, #92400e 1px, transparent 0); background-size: 20px 20px;"></div>
          <div class="relative">
            <app-tenant-brand />
            <h2 class="mt-8 text-2xl font-bold leading-tight text-gray-900">
              متجرك الذهبي<br />
              <span class="bg-gradient-to-r from-amber-700 to-gold bg-clip-text text-transparent">يُدار بثقة</span>
            </h2>
            <p class="mt-3 text-sm leading-6 text-gray-600">
              إدارة المبيعات، المشتريات، المخزون الذهبي والذمم — واجهة واحدة مصممة لصائغ يقدّر الدقة.
            </p>
            <div class="mt-6 grid grid-cols-3 gap-2 text-center text-xs">
              <span class="rounded-xl border border-amber-200 bg-white px-2 py-3 shadow-sm">
                <span class="mx-auto flex h-8 w-8 items-center justify-center rounded-lg bg-amber-500 text-white"><lucide-icon [img]="coinsIcon" [size]="16" /></span>
                <span class="mt-2 block font-semibold">ذهب دقيق</span>
                <span class="text-gray-500">مكافئ 21K</span>
              </span>
              <span class="rounded-xl border border-amber-200 bg-white px-2 py-3 shadow-sm">
                <span class="mx-auto flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-500 text-white"><lucide-icon [img]="walletIcon" [size]="16" /></span>
                <span class="mt-2 block font-semibold">مالية</span>
                <span class="text-gray-500">3 عملات</span>
              </span>
              <span class="rounded-xl border border-amber-200 bg-white px-2 py-3 shadow-sm">
                <span class="mx-auto flex h-8 w-8 items-center justify-center rounded-lg bg-gray-900 text-white"><lucide-icon [img]="shieldIcon" [size]="16" /></span>
                <span class="mt-2 block font-semibold">معزول</span>
                <span class="text-gray-500">لكل متجر</span>
              </span>
            </div>
          </div>


        </div>

        <!-- Form -->
        <div class="flex flex-1 flex-col justify-center px-6 py-8 sm:px-8 lg:px-10">
          <div class="mx-auto w-full max-w-md">
            <div class="mb-8 lg:hidden">
              <app-tenant-brand />
            </div>
            <div class="mb-7">
              <h1 class="text-2xl font-bold tracking-tight text-gray-900">مرحباً بعودتك</h1>
              <p class="mt-2 text-sm text-gray-600">سجّل الدخول لإدارة أعمال متجرك — كل الأرصدة محسوبة لحظياً من القيود.</p>
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
                  placeholder="you@yourstore.com"
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
                تسجيل الدخول
              </app-button>

              <div class="flex items-center gap-2 text-xs">
                <a href="/forgot-password" class="font-bold text-gold underline">نسيت كلمة المرور؟ إعادة عبر الهاتف (+970)</a>
              </div>
              <div id="recaptcha-container" class="hidden"></div>
            </form>


          </div>
        </div>
      </div>
    </main>
  `,
})
export class TenantLoginPage {
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

  readonly coinsIcon = resolveIcon('coins');
  readonly walletIcon = resolveIcon('wallet');
  readonly shieldIcon = resolveIcon('shield-check');
  readonly mailIcon = resolveIcon('info');
  readonly lockIcon = resolveIcon('shield');

  onSubmit(): void {
    submit(this.loginForm, async () => {
      this.submitting.set(true);
      try {
        const res = await this.auth.login(this.credentials().email, this.credentials().password) as unknown as { is2fa?: boolean; requiresEnrollment?: boolean; tempTicket?: string; maskedPhone?: string } | undefined;
        if (res && res.is2fa) {
          sessionStorage.setItem('tempTicket', res.tempTicket ?? '');
          if (res.requiresEnrollment) {
            await this.router.navigate(['/enroll-phone'], { queryParams: { tempTicket: res.tempTicket, isHost: false } });
          } else {
            await this.router.navigate(['/verify-2fa'], { queryParams: { tempTicket: res.tempTicket, masked: res.maskedPhone ?? '', isHost: false, phone: '+970592990484' } });
          }
          return;
        }
        await this.router.navigateByUrl(this.safeReturnUrl(this.firstPermittedRoute()));
      } catch {
        this.toasts.error('تعذّر تسجيل الدخول. تحقّق من البريد الإلكتروني وكلمة المرور.');
      } finally {
        this.submitting.set(false);
      }
    });
  }

  private firstPermittedRoute(): string {
    return buildNavItems(this.auth.permissions())[0]?.route ?? '/403';
  }

  private safeReturnUrl(fallback: string): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : fallback;
  }
}
