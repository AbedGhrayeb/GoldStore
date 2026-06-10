import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-ledger',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="دفتر الأستاذ" subtitle="سجل المعاملات المالية" />
    <div class="p-6">
      <p class="text-text-muted">دفتر الأستاذ - قيد التطوير</p>
    </div>
  `,
})
export class LedgerComponent {}
