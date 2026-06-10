import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { KpiCardComponent } from '../../../../shared/components/kpi-card/kpi-card.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { InventoryService } from '../../services/inventory.service';
import { InventoryItem } from '../../models/inventory.models';

@Component({
  selector: 'app-inventory-detail',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent, KpiCardComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="(item()?.name || 'تفاصيل المخزون')" subtitle="عرض تفاصيل عنصر المخزون">
      <button routerLink="/inventory" class="px-4 py-2.5 border border-gold-border/40 text-text-primary rounded-lg text-sm font-medium hover:bg-surface-hover transition-colors">عودة</button>
      <button [routerLink]="['/inventory', item()?.id, 'edit']" class="px-4 py-2.5 bg-gold-primary text-deep-black rounded-lg text-sm font-medium hover:bg-gold-primary-hover transition-colors">تعديل</button>
    </app-page-header>
    <div class="p-6">
      @if (item(); as i) {
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <app-kpi-card label="الوزن" [value]="i.weight + 'g'" icon="weight-icon" />
          <app-kpi-card label="الكمية" [value]="i.quantity" />
          <app-kpi-card label="سعر التكلفة" [value]="i.costPrice + ' ر.س'" />
          <app-kpi-card label="سعر البيع" [value]="i.sellingPrice + ' ر.س'" />
        </div>
        <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-6">
          <div class="grid grid-cols-2 gap-6">
            <div><span class="text-sm text-text-muted">الاسم</span><p class="text-sm font-medium text-text-primary mt-1">{{ i.name }}</p></div>
            <div><span class="text-sm text-text-muted">الفئة</span><p class="text-sm font-medium text-text-primary mt-1">{{ i.category }}</p></div>
            <div><span class="text-sm text-text-muted">العيار</span><p class="text-sm font-medium text-text-primary mt-1">{{ i.karat }}</p></div>
            <div><span class="text-sm text-text-muted">الحالة</span><p class="mt-1"><app-status-badge [variant]="i.status === 'in_stock' ? 'success' : i.status === 'low_stock' ? 'warning' : 'error'">{{ i.status === 'in_stock' ? 'متوفر' : i.status === 'low_stock' ? 'منخفض' : 'نافد' }}</app-status-badge></p></div>
            <div class="col-span-2"><span class="text-sm text-text-muted">الوصف</span><p class="text-sm font-medium text-text-primary mt-1">{{ i.description || 'لا يوجد وصف' }}</p></div>
          </div>
        </div>
      }
    </div>
  `,
})
export class InventoryDetailComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly route = inject(ActivatedRoute);

  protected readonly item = signal<InventoryItem | null>(null);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.inventoryService.getItem(id).subscribe(i => this.item.set(i));
  }
}
