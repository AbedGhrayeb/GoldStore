import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-expenses-list',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="المصروفات" subtitle="إدارة المصروفات" />
    <div class="p-6">
      <p class="text-text-muted">قائمة المصروفات - قيد التطوير</p>
    </div>
  `,
})
export class ExpensesListComponent {}
