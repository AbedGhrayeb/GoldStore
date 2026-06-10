import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="المستخدمون والصلاحيات" subtitle="إدارة المستخدمين والأدوار" />
    <div class="p-6">
      <p class="text-text-muted">المستخدمون والصلاحيات - قيد التطوير</p>
    </div>
  `,
})
export class UsersListComponent {}
