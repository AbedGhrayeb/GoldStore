import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../../../shared/components/search-box/search-box.component';
import { DataTableComponent, Column } from '../../../../shared/components/data-table/data-table.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SalesService } from '../../services/sales.service';
import { SaleInvoice } from '../../models/sales.models';

@Component({
  selector: 'app-sales-list',
  standalone: true,
  imports: [RouterLink, FormsModule, PageHeaderComponent, SearchBoxComponent, DataTableComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="المبيعات" subtitle="إدارة فواتير المبيعات">
      <button routerLink="/sales/create" class="px-4 py-2.5 bg-gold-primary text-deep-black rounded-lg text-sm font-medium hover:bg-gold-primary-hover transition-colors flex items-center gap-2">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
        فاتورة جديدة
      </button>
    </app-page-header>
    <div class="p-6">
      <div class="mb-4"><app-search-box [(searchTerm)]="searchTerm" placeholder="بحث في الفواتير..." /></div>
      <app-data-table [columns]="columns" [data]="invoices()" [total]="total()" [showToolbar]="true" (rowClick)="onRowClick($event)">
        <ng-container empty-state><app-empty-state title="لا توجد فواتير" message="لم يتم إنشاء أي فواتير مبيعات بعد" /></ng-container>
      </app-data-table>
    </div>
  `,
})
export class SalesListComponent implements OnInit {
  private readonly salesService = inject(SalesService);
  private readonly router = inject(Router);
  protected readonly invoices = signal<SaleInvoice[]>([]);
  protected readonly total = signal(0);
  protected readonly searchTerm = signal('');

  protected readonly columns: Column[] = [
    { key: 'invoiceNumber', label: 'رقم الفاتورة' },
    { key: 'customerName', label: 'العميل' },
    { key: 'finalAmount', label: 'المبلغ', format: (v: unknown) => `${(v as number).toLocaleString('ar-SA')} ر.س` },
    { key: 'status', label: 'الحالة', format: (v: unknown) => v === 'completed' ? 'مكتمل' : v === 'pending' ? 'معلق' : 'ملغي' },
    { key: 'createdAt', label: 'التاريخ', format: (v: unknown) => new Date(v as string).toLocaleDateString('ar-SA') },
  ];

  ngOnInit() { this.loadInvoices(); }

  private loadInvoices() {
    this.salesService.getInvoices().subscribe(res => { this.invoices.set(res.invoices); this.total.set(res.total); });
  }

  protected onRowClick(row: unknown) { this.router.navigate(['/sales', (row as SaleInvoice).id]); }
}
