import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="الحسابات" subtitle="إدارة الحسابات المالية" />
    <div class="p-6">
      <p class="text-text-muted">الحسابات - قيد التطوير</p>
    </div>
  `,
})
export class AccountsComponent {}
