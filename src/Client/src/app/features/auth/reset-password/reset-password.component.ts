import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-gradient-to-br from-surface-base via-[#f0edf5] to-surface-base flex items-center justify-center p-4">
      <div class="w-full max-w-md">
        <div class="bg-surface-card rounded-xl shadow-card border border-gold-border/30 p-8">
          <div class="text-center mb-6">
            <h1 class="text-xl font-bold text-text-primary">إعادة تعيين كلمة المرور</h1>
            <p class="text-sm text-text-muted mt-1">أدخل كلمة المرور الجديدة</p>
          </div>
          @if (success()) {
            <div class="p-4 rounded-lg bg-success/10 border border-success-border text-sm text-success text-center mb-4">تم إعادة تعيين كلمة المرور بنجاح</div>
            <a routerLink="/login" class="block w-full text-center min-h-[48px] leading-[48px] bg-gold-primary text-deep-black font-semibold text-sm rounded-lg hover:bg-gold-primary-hover transition-colors">تسجيل الدخول</a>
          } @else {
            <form [formGroup]="resetForm" (ngSubmit)="onSubmit()" class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">كلمة المرور الجديدة</label>
                <input type="password" formControlName="password" class="w-full py-2.5 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" placeholder="••••••••" />
                @if (resetForm.get('password')?.invalid && resetForm.get('password')?.touched) {
                  <span class="text-xs text-error mt-1 block">كلمة المرور يجب أن تكون 6 أحرف على الأقل</span>
                }
              </div>
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">تأكيد كلمة المرور</label>
                <input type="password" formControlName="confirmPassword" class="w-full py-2.5 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" placeholder="••••••••" />
                @if (resetForm.errors?.['mismatch'] && resetForm.get('confirmPassword')?.touched) {
                  <span class="text-xs text-error mt-1 block">كلمة المرور غير متطابقة</span>
                }
              </div>
              @if (error()) {
                <div class="p-3 rounded-lg bg-error-bg border border-error-border text-sm text-error">{{ error() }}</div>
              }
              <button type="submit" [disabled]="resetForm.invalid || loading()" class="w-full min-h-[48px] inline-flex items-center justify-center gap-2 px-6 py-2.5 bg-gold-primary text-deep-black font-semibold text-sm rounded-lg cursor-pointer transition-[background-color,opacity] duration-200 hover:bg-gold-primary-hover disabled:opacity-50 disabled:cursor-not-allowed">
                {{ loading() ? 'جاري...' : 'إعادة التعيين' }}
              </button>
            </form>
          }
        </div>
      </div>
    </div>
  `,
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal(false);
  protected readonly resetForm = this.fb.group({
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]],
  }, { validators: (g) => g.get('password')?.value === g.get('confirmPassword')?.value ? null : { mismatch: true } });

  protected onSubmit() {
    if (this.resetForm.invalid) return;
    this.loading.set(true);
    this.error.set('');
    const token = this.route.snapshot.queryParams['token'];
    const email = this.route.snapshot.queryParams['email'];
    this.http.post('/api/auth/reset-password', { token, email, ...this.resetForm.value }).subscribe({
      next: () => { this.success.set(true); this.loading.set(false); },
      error: (err) => { this.error.set(err.error?.message || 'فشل إعادة التعيين'); this.loading.set(false); },
    });
  }
}
