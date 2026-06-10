import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-employee-detail',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="تفاصيل الموظف" subtitle="عرض معلومات الموظف" />
    <div class="p-6">
      <p class="text-text-muted">تفاصيل الموظف - قيد التطوير</p>
    </div>
  `,
})
export class EmployeeDetailComponent {}
