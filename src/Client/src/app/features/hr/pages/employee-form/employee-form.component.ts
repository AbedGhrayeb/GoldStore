import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="بيانات الموظف" subtitle="إضافة أو تعديل بيانات الموظف" />
    <div class="p-6">
      <p class="text-text-muted">نموذج الموظف - قيد التطوير</p>
    </div>
  `,
})
export class EmployeeFormComponent {}
