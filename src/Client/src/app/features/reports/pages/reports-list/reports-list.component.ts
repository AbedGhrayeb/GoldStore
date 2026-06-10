import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-reports-list',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="التقارير" subtitle="عرض وتصدير التقارير" />
    <div class="p-6">
      <p class="text-text-muted">التقارير - قيد التطوير</p>
    </div>
  `,
})
export class ReportsListComponent {}
