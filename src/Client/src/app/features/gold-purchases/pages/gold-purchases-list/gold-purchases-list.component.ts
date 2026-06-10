import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-gold-purchases-list',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="مشتريات الذهب" subtitle="إدارة مشتريات الذهب" />
    <div class="p-6">
      <p class="text-text-muted">قائمة مشتريات الذهب - قيد التطوير</p>
    </div>
  `,
})
export class GoldPurchasesListComponent {}
