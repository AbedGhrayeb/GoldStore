import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-gradient-to-br from-surface-base via-[#f0edf5] to-surface-base flex items-center justify-center p-4">
      <div class="w-full max-w-md">
        <div class="bg-surface-card rounded-xl shadow-card border border-gold-border/30 p-8">
          <div class="text-center mb-6">
            <div class="w-14 h-14 rounded-full bg-gold-primary/10 flex items-center justify-center mx-auto mb-3">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="text-gold-primary"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0110 0v4"/></svg>
            </div>
            <h1 class="text-xl font-bold text-text-primary">نسيت كلمة المرور</h1>
            <p class="text-sm text-text-muted mt-1">أدخل بريدك الإلكتروني وسنرسل لك رابط إعادة التعيين</p>
          </div>
          @if (success()) {
            <div class="p-4 rounded-lg bg-success/10 border border-success-border text-sm text-success text-center">
              تم إرسال رابط إعادة تعيين كلمة المرور إلى بريدك الإلكتروني
            </div>
          } @else {
            <form [formGroup]="forgotForm" (ngSubmit)="onSubmit()" class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">البريد الإلكتروني</label>
                <input type="email" formControlName="email" class="w-full py-2.5 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" placeholder="admin@example.com" />
                @if (forgotForm.get('email')?.invalid && forgotForm.get('email')?.touched) {
                  <span class="text-xs text-error mt-1 block">البريد الإلكتروني مطلوب</span>
                }
              </div>
              @if (error()) {
                <div class="p-3 rounded-lg bg-error-bg border border-error-border text-sm text-error">{{ error() }}</div>
              }
              <button type="submit" [disabled]="forgotForm.invalid || loading()" class="w-full min-h-[48px] inline-flex items-center justify-center gap-2 px-6 py-2.5 bg-gold-primary text-deep-black font-semibold text-sm rounded-lg cursor-pointer transition-[background-color,opacity] duration-200 hover:bg-gold-primary-hover disabled:opacity-50 disabled:cursor-not-allowed">
                @if (loading()) {
                  <svg class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" class="opacity-25"/><path d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" fill="currentColor" class="opacity-75"/></svg>
                }
                {{ loading() ? 'جاري الإرسال...' : 'إرسال رابط إعادة التعيين' }}
              </button>
            </form>
          }
          <div class="text-center mt-4">
            <a routerLink="/login" class="text-sm text-gold-primary font-medium hover:text-gold-primary-hover no-underline transition-colors">العودة لتسجيل الدخول</a>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal(false);
  protected readonly forgotForm = this.fb.group({ email: ['', [Validators.required, Validators.email]] });

  protected onSubmit() {
    if (this.forgotForm.invalid) return;
    this.loading.set(true);
    this.error.set('');
    this.http.post('/api/auth/forgot-password', this.forgotForm.value).subscribe({
      next: () => { this.success.set(true); this.loading.set(false); },
      error: (err) => { this.error.set(err.error?.message || 'فشل الإرسال'); this.loading.set(false); },
    });
  }
}
