import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-finance-overview',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="المالية" subtitle="نظرة عامة على الحسابات المالية" />
    <div class="p-6">
      <p class="text-text-muted">نظرة عامة مالية - قيد التطوير</p>
    </div>
  `,
})
export class FinanceOverviewComponent {}
