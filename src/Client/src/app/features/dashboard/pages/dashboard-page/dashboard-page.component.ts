import { Component, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import type { ApexAxisChartSeries, ApexNonAxisChartSeries, ApexChart, ApexXAxis, ApexYAxis, ApexStroke, ApexFill, ApexDataLabels, ApexGrid, ApexTooltip, ApexPlotOptions, ApexLegend } from 'ng-apexcharts';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { KpiCardComponent } from '../../../../shared/components/kpi-card/kpi-card.component';
import { ChartCardComponent } from '../../../../shared/components/chart-card/chart-card.component';
import { DashboardService } from '../../services/dashboard.service';

const GOLD_PRIMARY = '#D4AF37';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, NgApexchartsModule, PageHeaderComponent, KpiCardComponent, ChartCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="لوحة القيادة" />
    <div class="p-6">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <app-kpi-card label="إجمالي المبيعات" [value]="(stats().totalSales | currency:'SAR ') ?? ''" [change]="formatChange(stats().totalSalesChange)" [changeDir]="stats().totalSalesChangeDir" icon="<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><polyline points='22 7 13.5 15.5 8.5 10.5 2 17'/><polyline points='16 7 22 7 22 13'/></svg>" />
        <app-kpi-card label="قيمة المخزون" [value]="(stats().inventoryValue | currency:'SAR ') ?? ''" [change]="formatChange(stats().inventoryValueChange)" [changeDir]="stats().inventoryValueChangeDir" icon="<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><path d='M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z'/></svg>" />
        <app-kpi-card label="التدفق النقدي" [value]="(stats().cashFlow | currency:'SAR ') ?? ''" [change]="formatChange(stats().cashFlowChange)" [changeDir]="stats().cashFlowChangeDir" icon="<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><path d='M21 12V7H5a2 2 0 0 1 0-4h14v4'/><path d='M3 5v14a2 2 0 0 0 2 2h16v-5'/><path d='M18 12a2 2 0 0 0 0 4h4v-4Z'/></svg>" />
        <app-kpi-card label="سعر الذهب اليوم" [value]="(stats().goldPriceToday | currency:'SAR ') ?? ''" [change]="formatChange(stats().goldPriceChange)" [changeDir]="stats().goldPriceChangeDir" icon="<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><polyline points='22 12 18 12 15 21 9 3 6 12 2 12'/></svg>" />
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        <app-chart-card title="المبيعات والمصروفات">
          <div id="sales-chart"></div>
          <apx-chart [series]="salesSeries()" [chart]="salesChart" [xaxis]="salesXaxis()" [stroke]="chartStroke" [fill]="salesFill" [dataLabels]="dataLabelsOff" [grid]="chartGrid" [tooltip]="chartTooltip" [yaxis]="sharedYaxis" height="300"></apx-chart>
        </app-chart-card>
        <app-chart-card title="المصروفات حسب الفئة">
          <apx-chart [series]="expenseSeries()" [chart]="expenseChart" [xaxis]="expenseXaxis()" [plotOptions]="expensePlotOptions" [dataLabels]="dataLabelsOff" [grid]="chartGrid" [colors]="expenseColors" height="300"></apx-chart>
        </app-chart-card>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        <app-chart-card title="توزيع العيارات">
          <apx-chart [series]="goldKaratSeries()" [chart]="goldPieChart" [labels]="goldKaratLabels()" [dataLabels]="pieDataLabels" [legend]="pieLegend" [tooltip]="pieTooltip" [colors]="goldColors" height="300"></apx-chart>
        </app-chart-card>
        <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-5">
          <h3 class="text-base font-semibold text-text-primary mb-4">آخر النشاطات</h3>
          <div class="space-y-3">
            @for (activity of activities(); track activity.id) {
              <div class="flex items-center gap-3 py-2 border-b border-gold-border/10 last:border-0">
                <div class="w-8 h-8 rounded-full bg-gold-primary/10 flex items-center justify-center">
                  <span class="text-xs font-bold text-gold-primary">{{ activity.user.charAt(0) }}</span>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-sm text-text-primary truncate">{{ activity.action }}</p>
                  <p class="text-xs text-text-muted">{{ activity.timestamp }}</p>
                </div>
              </div>
            }
          </div>
        </div>
      </div>

      <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-5">
        <h3 class="text-base font-semibold text-text-primary mb-4">آخر العمليات</h3>
        <div class="overflow-x-auto">
          <table class="w-full text-right">
            <thead>
              <tr class="border-b border-gold-border/20">
                <th class="py-3 px-4 text-sm text-text-muted font-medium">العميل</th>
                <th class="py-3 px-4 text-sm text-text-muted font-medium">النوع</th>
                <th class="py-3 px-4 text-sm text-text-muted font-medium">المبلغ</th>
                <th class="py-3 px-4 text-sm text-text-muted font-medium">التاريخ</th>
                <th class="py-3 px-4 text-sm text-text-muted font-medium">الحالة</th>
              </tr>
            </thead>
            <tbody>
              @for (tx of transactions(); track tx.id) {
                <tr class="border-b border-gold-border/10 hover:bg-gold-primary/5 transition-colors">
                  <td class="py-3 px-4 text-sm text-text-primary">{{ tx.customerName }}</td>
                  <td class="py-3 px-4">
                    <span class="px-2 py-1 rounded-full text-xs font-medium"
                      [class]="tx.type === 'sale' ? 'bg-success/10 text-success' : tx.type === 'purchase' ? 'bg-gold-primary/10 text-gold-primary' : 'bg-error/10 text-error'"
                    >{{ tx.type === 'sale' ? 'بيع' : tx.type === 'purchase' ? 'مشتريات' : 'مصروفات' }}</span>
                  </td>
                  <td class="py-3 px-4 text-sm text-text-primary" dir="ltr">{{ tx.amount | currency:'SAR ' }}</td>
                  <td class="py-3 px-4 text-sm text-text-muted">{{ tx.date | date:'d MMM yyyy' }}</td>
                  <td class="py-3 px-4">
                    <span class="px-2 py-1 rounded-full text-xs font-medium"
                      [class]="tx.status === 'completed' ? 'bg-success/10 text-success' : tx.status === 'pending' ? 'bg-warning/10 text-warning' : 'bg-error/10 text-error'"
                    >{{ tx.status === 'completed' ? 'مكتمل' : tx.status === 'pending' ? 'معلق' : 'ملغي' }}</span>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="5" class="py-8 text-center text-text-muted">لا توجد معاملات حديثة</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
})
export class DashboardPageComponent {
  private readonly dashboardService = inject(DashboardService);

  protected readonly stats = toSignal(this.dashboardService.getStats(), {
    initialValue: { totalSales: 0, totalSalesChange: 0, totalSalesChangeDir: 'up', inventoryValue: 0, inventoryValueChange: 0, inventoryValueChangeDir: 'up', cashFlow: 0, cashFlowChange: 0, cashFlowChangeDir: 'up', goldPriceToday: 0, goldPriceChange: 0, goldPriceChangeDir: 'up' },
  });
  protected readonly salesData = toSignal(this.dashboardService.getSalesChart(), { initialValue: [] });
  protected readonly expenseData = toSignal(this.dashboardService.getExpenseChart(), { initialValue: [] });
  protected readonly goldKaratData = toSignal(this.dashboardService.getGoldKarat(), { initialValue: [] });
  protected readonly transactions = toSignal(this.dashboardService.getRecentTransactions(), { initialValue: [] });
  protected readonly activities = toSignal(this.dashboardService.getRecentActivities(), { initialValue: [] });

  protected readonly salesSeries = computed<ApexAxisChartSeries>(() => [
    { name: 'المبيعات', data: this.salesData().map(d => d.sales) },
    { name: 'المصروفات', data: this.salesData().map(d => d.expenses) },
  ]);
  protected readonly salesXaxis = computed<ApexXAxis>(() => ({ categories: this.salesData().map(d => d.month), type: 'category', labels: { style: { colors: '#9ca3af', fontSize: '12px' } }, axisBorder: { show: false }, axisTicks: { show: false } }));
  protected readonly expenseSeries = computed<ApexAxisChartSeries>(() => [{ name: 'المصروفات', data: this.expenseData().map(d => d.amount) }]);
  protected readonly expenseXaxis = computed<ApexXAxis>(() => ({ categories: this.expenseData().map(d => d.category), type: 'category', labels: { style: { colors: '#9ca3af', fontSize: '12px' } }, axisBorder: { show: false }, axisTicks: { show: false } }));
  protected readonly goldKaratSeries = computed<ApexNonAxisChartSeries>(() => this.goldKaratData().map(d => d.weight));
  protected readonly goldKaratLabels = computed(() => this.goldKaratData().map(d => `${d.karat} (${d.percentage}%)`));

  protected readonly salesChart: ApexChart = { type: 'area', height: 300, toolbar: { show: false }, fontFamily: 'inherit' };
  protected readonly expenseChart: ApexChart = { type: 'bar', height: 300, toolbar: { show: false }, fontFamily: 'inherit' };
  protected readonly goldPieChart: ApexChart = { type: 'pie', height: 300, toolbar: { show: false }, fontFamily: 'inherit' };
  protected readonly chartStroke: ApexStroke = { curve: 'smooth', width: 2, colors: [GOLD_PRIMARY, '#ef4444'] };
  protected readonly salesFill: ApexFill = { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.3, opacityTo: 0, stops: [0, 100] } };
  protected readonly expensePlotOptions: ApexPlotOptions = { bar: { borderRadius: 4, columnWidth: '60%', colors: { ranges: [{ from: 0, to: 10000, color: GOLD_PRIMARY }] } } };
  protected readonly sharedYaxis: ApexYAxis = { labels: { style: { colors: '#9ca3af', fontSize: '12px' }, formatter: (val: number) => val.toLocaleString('ar-SA') }, axisBorder: { show: false }, axisTicks: { show: false } };
  protected readonly dataLabelsOff: ApexDataLabels = { enabled: false };
  protected readonly chartGrid: ApexGrid = { show: true, borderColor: '#e5e7eb', strokeDashArray: 4, padding: { left: 0, right: 0 } };
  protected readonly chartTooltip: ApexTooltip = { theme: 'light', style: { fontSize: '12px' } };
  protected readonly pieLegend: ApexLegend = { position: 'bottom', labels: { colors: '#6b7280' } };
  protected readonly pieDataLabels: ApexDataLabels = { enabled: true, style: { colors: ['#111827'] } };
  protected readonly pieTooltip: ApexTooltip = { theme: 'dark' };
  protected readonly expenseColors = [GOLD_PRIMARY];
  protected readonly goldColors = ['#D4AF37', '#E5C55A', '#F0D878', '#F5E396', '#F9EDB4'];

  protected formatChange(value: number): string {
    const prefix = value >= 0 ? '+' : '';
    return prefix + value.toLocaleString('ar-SA');
  }
}
