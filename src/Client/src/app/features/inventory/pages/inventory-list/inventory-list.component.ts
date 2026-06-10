import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../../../shared/components/search-box/search-box.component';
import { DataTableComponent, Column } from '../../../../shared/components/data-table/data-table.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { InventoryService } from '../../services/inventory.service';
import { InventoryItem } from '../../models/inventory.models';

@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [RouterLink, FormsModule, PageHeaderComponent, SearchBoxComponent, DataTableComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="المخزون" subtitle="إدارة مخزون الذهب والمجوهرات">
      <button routerLink="/inventory/create" class="px-4 py-2.5 bg-gold-primary text-deep-black rounded-lg text-sm font-medium hover:bg-gold-primary-hover transition-colors flex items-center gap-2">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
        إضافة عنصر
      </button>
    </app-page-header>
    <div class="p-6">
      <div class="mb-4">
        <app-search-box [(searchTerm)]="searchTerm" placeholder="بحث في المخزون..." />
      </div>
      <app-data-table
        [columns]="columns"
        [data]="items()"
        [total]="total()"
        [showToolbar]="true"
        (rowClick)="onRowClick($event)"
      >
        <ng-container empty-state>
          <app-empty-state title="لا توجد عناصر" message="لم يتم إضافة أي عناصر للمخزون بعد" />
        </ng-container>
      </app-data-table>
    </div>
  `,
})
export class InventoryListComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly router = inject(Router);

  protected readonly items = signal<InventoryItem[]>([]);
  protected readonly total = signal(0);
  protected readonly searchTerm = signal('');

  protected readonly columns: Column[] = [
    { key: 'name', label: 'الاسم' },
    { key: 'category', label: 'الفئة' },
    { key: 'karat', label: 'العيار' },
    { key: 'weight', label: 'الوزن', format: (v: unknown) => `${v}g` },
    { key: 'quantity', label: 'الكمية' },
    { key: 'sellingPrice', label: 'سعر البيع', format: (v: unknown) => `${(v as number).toLocaleString('ar-SA')} ر.س` },
    { key: 'status', label: 'الحالة', format: (v: unknown) => v === 'in_stock' ? 'متوفر' : v === 'low_stock' ? 'منخفض' : 'نافد' },
  ];

  ngOnInit() {
    this.loadItems();
  }

  private loadItems() {
    this.inventoryService.getItems().subscribe(res => {
      this.items.set(res.items);
      this.total.set(res.total);
    });
  }

  protected onRowClick(row: unknown) {
    this.router.navigate(['/inventory', (row as InventoryItem).id]);
  }
}
