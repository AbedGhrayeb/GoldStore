import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-supplier-detail',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="تفاصيل المورد" subtitle="عرض معلومات المورد" />
    <div class="p-6">
      <p class="text-text-muted">تفاصيل المورد - قيد التطوير</p>
    </div>
  `,
})
export class SupplierDetailComponent {}
