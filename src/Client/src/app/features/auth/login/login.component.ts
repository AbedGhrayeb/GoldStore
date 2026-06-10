import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-gradient-to-br from-surface-base via-[#f0edf5] to-surface-base flex items-center justify-center p-4">
      <div class="w-full max-w-md">
        <div class="bg-surface-card rounded-xl shadow-card border border-gold-border/30 p-8 relative">
          <div class="text-center mb-8">
            <div class="w-16 h-16 rounded-xl bg-gold-primary flex items-center justify-center mx-auto mb-4">
              <span class="text-deep-black font-bold text-2xl">م</span>
            </div>
            <h1 class="text-xl font-bold text-text-primary">مجوهرات سرحان</h1>
            <p class="text-sm text-text-muted mt-1">نظام إدارة الذهب والمجوهرات</p>
          </div>
          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-text-primary mb-1.5">البريد الإلكتروني</label>
              <input
                type="email"
                formControlName="email"
                class="w-full py-2.5 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted"
                placeholder="admin@example.com"
              />
              @if (loginForm.get('email')?.invalid && loginForm.get('email')?.touched) {
                <span class="text-xs text-error mt-1 block">البريد الإلكتروني مطلوب</span>
              }
            </div>
            <div>
              <label class="block text-sm font-medium text-text-primary mb-1.5">كلمة المرور</label>
              <input
                type="password"
                formControlName="password"
                class="w-full py-2.5 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted"
                placeholder="••••••••"
              />
              @if (loginForm.get('password')?.invalid && loginForm.get('password')?.touched) {
                <span class="text-xs text-error mt-1 block">كلمة المرور مطلوبة</span>
              }
            </div>
            @if (error()) {
              <div class="p-3 rounded-lg bg-error-bg border border-error-border text-sm text-error">{{ error() }}</div>
            }
            <button
              type="submit"
              [disabled]="loginForm.invalid || loading()"
              class="w-full min-h-[48px] inline-flex items-center justify-center gap-2 px-6 py-2.5 bg-gold-primary text-deep-black font-semibold text-sm rounded-lg cursor-pointer transition-[background-color,opacity] duration-200 hover:bg-gold-primary-hover disabled:opacity-50 disabled:cursor-not-allowed"
            >
              @if (loading()) {
                <svg class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" class="opacity-25"/><path d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" fill="currentColor" class="opacity-75"/></svg>
              }
              {{ loading() ? 'جاري تسجيل الدخول...' : 'تسجيل الدخول' }}
            </button>
          </form>
          <div class="text-center mt-4">
            <a routerLink="/forgot-password" class="text-sm text-gold-primary font-medium hover:text-gold-primary-hover no-underline transition-colors">نسيت كلمة المرور؟</a>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly error = signal('');

  protected readonly loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  protected onSubmit() {
    if (this.loginForm.invalid) return;
    this.loading.set(true);
    this.error.set('');
    this.auth.login(this.loginForm.value as any).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.error.set(err.error?.message || 'فشل تسجيل الدخول. تحقق من البيانات.');
        this.loading.set(false);
      },
    });
  }
}
