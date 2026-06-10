import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { KpiCardComponent } from '../../../../shared/components/kpi-card/kpi-card.component';
import { DataTableComponent, Column } from '../../../../shared/components/data-table/data-table.component';
import { SalesService } from '../../services/sales.service';
import { SaleInvoice } from '../../models/sales.models';

@Component({
  selector: 'app-sale-detail',
  standalone: true,
  imports: [DatePipe, RouterLink, PageHeaderComponent, KpiCardComponent, DataTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'فاتورة #' + (invoice()?.invoiceNumber || '')" subtitle="تفاصيل فاتورة المبيعات">
      <button routerLink="/sales" class="px-4 py-2.5 border border-gold-border/40 text-text-primary rounded-lg text-sm font-medium hover:bg-surface-hover transition-colors">عودة</button>
    </app-page-header>
    <div class="p-6">
      @if (invoice(); as inv) {
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <app-kpi-card label="إجمالي الفاتورة" [value]="inv.finalAmount + ' ر.س'" />
          <app-kpi-card label="المدفوع" [value]="inv.paidAmount + ' ر.س'" />
          <app-kpi-card label="المتبقي" [value]="inv.remainingAmount + ' ر.س'" />
          <app-kpi-card label="الحالة" [value]="inv.status === 'completed' ? 'مكتمل' : inv.status === 'pending' ? 'معلق' : 'ملغي'" />
        </div>
        <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-6 mb-6">
          <div class="grid grid-cols-2 gap-4">
            <div><span class="text-sm text-text-muted">العميل</span><p class="text-sm font-medium text-text-primary mt-1">{{ inv.customerName }}</p></div>
            <div><span class="text-sm text-text-muted">طريقة الدفع</span><p class="text-sm font-medium text-text-primary mt-1">{{ inv.paymentMethod === 'cash' ? 'نقدي' : inv.paymentMethod === 'card' ? 'بطاقة' : inv.paymentMethod === 'bank_transfer' ? 'تحويل بنكي' : 'آجل' }}</p></div>
            <div><span class="text-sm text-text-muted">التاريخ</span><p class="text-sm font-medium text-text-primary mt-1">{{ inv.createdAt | date:'d MMM yyyy' }}</p></div>
          </div>
        </div>
        <app-data-table [columns]="itemColumns" [data]="inv.items" />
      }
    </div>
  `,
})
export class SaleDetailComponent implements OnInit {
  private readonly salesService = inject(SalesService);
  private readonly route = inject(ActivatedRoute);
  protected readonly invoice = signal<SaleInvoice | null>(null);

  protected readonly itemColumns: Column[] = [
    { key: 'productName', label: 'المنتج' },
    { key: 'karat', label: 'العيار' },
    { key: 'weight', label: 'الوزن', format: (v: unknown) => `${v}g` },
    { key: 'quantity', label: 'الكمية' },
    { key: 'unitPrice', label: 'سعر الوحدة', format: (v: unknown) => `${(v as number).toLocaleString('ar-SA')} ر.س` },
    { key: 'totalPrice', label: 'الإجمالي', format: (v: unknown) => `${(v as number).toLocaleString('ar-SA')} ر.س` },
  ];

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.salesService.getInvoice(id).subscribe(inv => this.invoice.set(inv));
  }
}
