import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-gold-purchase-form',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="شراء ذهب جديد" subtitle="تسجيل عملية شراء ذهب" />
    <div class="p-6">
      <p class="text-text-muted">نموذج شراء الذهب - قيد التطوير</p>
    </div>
  `,
})
export class GoldPurchaseFormComponent {}
