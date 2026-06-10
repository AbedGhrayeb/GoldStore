import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="تغيير كلمة المرور" subtitle="تحديث كلمة المرور الخاصة بحسابك" />
    <div class="p-6">
      <div class="max-w-lg">
        <div class="bg-surface-card rounded-xl border border-gold-border/30 p-6 shadow-card">
          @if (success()) {
            <div class="p-4 rounded-lg bg-success/10 border border-success-border text-sm text-success text-center">تم تغيير كلمة المرور بنجاح</div>
          } @else {
            <form [formGroup]="passwordForm" (ngSubmit)="onSubmit()" class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">كلمة المرور الحالية</label>
                <input type="password" formControlName="currentPassword" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" />
              </div>
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">كلمة المرور الجديدة</label>
                <input type="password" formControlName="newPassword" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" />
              </div>
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">تأكيد كلمة المرور الجديدة</label>
                <input type="password" formControlName="confirmPassword" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" />
              </div>
              @if (error()) {
                <div class="p-3 rounded-lg bg-error-bg border border-error-border text-sm text-error">{{ error() }}</div>
              }
              <div class="flex justify-end pt-2">
                <button type="submit" [disabled]="passwordForm.invalid || loading()" class="min-h-[44px] inline-flex items-center justify-center gap-2 px-6 py-2 bg-gold-primary text-deep-black font-semibold text-sm rounded-lg transition-[background-color,opacity] duration-200 hover:bg-gold-primary-hover disabled:opacity-50 disabled:cursor-not-allowed">
                  {{ loading() ? 'جاري...' : 'تغيير كلمة المرور' }}
                </button>
              </div>
            </form>
          }
        </div>
      </div>
    </div>
  `,
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal(false);
  protected readonly passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required],
  }, { validators: (g) => g.get('newPassword')?.value === g.get('confirmPassword')?.value ? null : { mismatch: true } });

  protected onSubmit() {
    if (this.passwordForm.invalid) return;
    this.loading.set(true);
    this.error.set('');
    this.http.post('/api/auth/change-password', this.passwordForm.value).subscribe({
      next: () => { this.success.set(true); this.loading.set(false); },
      error: (err) => { this.error.set(err.error?.message || 'فشل تغيير كلمة المرور'); this.loading.set(false); },
    });
  }
}
