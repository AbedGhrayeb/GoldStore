import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-employees-list',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="الموظفون" subtitle="إدارة الموظفين" />
    <div class="p-6">
      <p class="text-text-muted">قائمة الموظفين - قيد التطوير</p>
    </div>
  `,
})
export class EmployeesListComponent {}
