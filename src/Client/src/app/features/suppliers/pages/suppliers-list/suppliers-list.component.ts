import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-suppliers-list',
  standalone: true,
  imports: [PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="الموردون" subtitle="إدارة الموردين" />
    <div class="p-6">
      <p class="text-text-muted">قائمة الموردين - قيد التطوير</p>
    </div>
  `,
})
export class SuppliersListComponent {}
