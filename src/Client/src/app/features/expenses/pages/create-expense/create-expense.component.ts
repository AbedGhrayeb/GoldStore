import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-create-expense',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="مصروف جديد" subtitle="تسجيل مصروف جديد" />
    <div class="p-6">
      <p class="text-text-muted">نموذج إضافة مصروف - قيد التطوير</p>
    </div>
  `,
})
export class CreateExpenseComponent {}
