import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-gold-price-dashboard',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="أسعار الذهب" subtitle="عرض أسعار الذهب حسب العيار" />
    <div class="p-6">
      <p class="text-text-muted">لوحة أسعار الذهب - قيد التطوير</p>
    </div>
  `,
})
export class GoldPriceDashboardComponent {}
