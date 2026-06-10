import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="الإعدادات" subtitle="إدارة إعدادات النظام" />
    <div class="p-6">
      <div class="max-w-3xl space-y-6">
        <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-6">
          <h3 class="text-base font-semibold text-text-primary mb-4">الإعدادات العامة</h3>
          <p class="text-sm text-text-muted">سيتم إضافة خيارات الإعدادات قريباً</p>
        </div>
      </div>
    </div>
  `,
})
export class SettingsPageComponent {}
