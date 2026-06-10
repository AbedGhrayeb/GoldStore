import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-gold-price-history',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="تاريخ الأسعار" subtitle="سجل أسعار الذهب السابقة" />
    <div class="p-6">
      <p class="text-text-muted">تاريخ الأسعار - قيد التطوير</p>
    </div>
  `,
})
export class GoldPriceHistoryComponent {}
