import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="الملف الشخصي" subtitle="عرض وتعديل معلومات الملف الشخصي" />
    <div class="p-6">
      <div class="max-w-2xl">
        <div class="bg-surface-card rounded-xl border border-gold-border/30 p-6 shadow-card">
          <div class="flex items-center gap-4 mb-6 pb-6 border-b border-gold-border/20">
            <div class="w-16 h-16 rounded-full bg-gold-primary/20 flex items-center justify-center text-xl font-bold text-gold-primary-dark shrink-0">
              {{ userInitial() }}
            </div>
            <div>
              <h2 class="text-lg font-semibold text-text-primary">{{ auth.user()?.name }}</h2>
              <p class="text-sm text-text-muted">{{ auth.user()?.email }}</p>
            </div>
          </div>
          <form [formGroup]="profileForm" class="space-y-4">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">الاسم</label>
                <input type="text" formControlName="name" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" />
              </div>
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">البريد الإلكتروني</label>
                <input type="email" formControlName="email" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" />
              </div>
              <div>
                <label class="block text-sm font-medium text-text-primary mb-1.5">رقم الهاتف</label>
                <input type="tel" formControlName="phone" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted" />
              </div>
            </div>
            <div class="flex justify-end pt-4">
              <button class="min-h-[44px] inline-flex items-center justify-center gap-2 px-6 py-2 bg-gold-primary text-deep-black font-semibold text-sm rounded-lg transition-[background-color,opacity] duration-200 hover:bg-gold-primary-hover">حفظ التغييرات</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
})
export class ProfileComponent {
  protected readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  protected readonly profileForm = this.fb.group({ name: [''], email: [''], phone: [''] });

  protected userInitial(): string {
    return this.auth.user()?.name?.charAt(0) || 'م';
  }
}
