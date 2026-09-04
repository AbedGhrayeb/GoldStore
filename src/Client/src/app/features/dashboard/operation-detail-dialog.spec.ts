import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import type { StoreOperationDetailResponse } from './store-operations-api.service';
import { OperationDetailDialog } from './operation-detail-dialog';

const DETAIL: StoreOperationDetailResponse = {
  id: 'op-sale-1',
  invoiceNumber: 'INV-1001',
  operationType: 'Sale',
  operationTypeLabel: 'بيع',
  date: '2026-08-19T10:00:00',
  counterpartyName: 'ليلى',
  counterpartyPhone: '0791234567',
  employeeName: 'أحمد',
  currency: 'JOD',
  currencySymbol: 'د.أ',
  totalAmount: 1500,
  amountPaid: 1500,
  remainingBalance: 0,
  status: 'Completed',
  statusLabel: 'مكتملة',
  accountName: 'الصندوق الرئيسي',
  items: [
    {
      id: 'item-1',
      karat: 21,
      weightInGrams: 12,
      equivalent21KWeightInGrams: 12,
      pricePerGram: 45,
      goldAmount: 540,
      categoryName: 'خواتم',
    },
  ],
};

@Component({
  imports: [OperationDetailDialog],
  template: `
    <app-operation-detail-dialog
      [open]="open()"
      [detail]="detail()"
      [loading]="loading()"
      [error]="error()"
      (closed)="closed.emit()"
      (retry)="retries += 1"
    />
  `,
})
class HostComponent {
  open = signal(true);
  detail = signal<StoreOperationDetailResponse | null>(null);
  loading = signal(false);
  error = signal<{
    status: number;
    title: string | null;
    detail: string | null;
    validation: Record<string, string[]> | null;
  } | null>(null);
  retries = 0;

  readonly closed = { emit: (): void => undefined };
}

function createFixture(setup?: (host: HostComponent) => void): ComponentFixture<HostComponent> {
  const fixture = TestBed.createComponent(HostComponent);
  setup?.(fixture.componentInstance);
  fixture.detectChanges();
  return fixture;
}

describe('OperationDetailDialog', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('renders header info, amounts and items for a sale', () => {
    const fixture = createFixture((host) => {
      host.detail.set(DETAIL);
    });
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('INV-1001');
    expect(text).toContain('بيع');
    expect(text).toContain('مكتملة');
    expect(text).toContain('العميل');
    expect(text).toContain('ليلى');
    expect(text).toContain('البائع');
    expect(text).toContain('خواتم');
    expect(text).toContain('21K');
  });

  it('uses seller/buyer labels for a purchase', () => {
    const fixture = createFixture((host) => {
      host.detail.set({
        ...DETAIL,
        id: 'op-buy-1',
        operationType: 'Buy',
        operationTypeLabel: 'شراء',
      });
    });
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('البائع');
    expect(text).toContain('المشتري');
    expect(text).not.toContain('العميل');
  });

  it('renders skeletons while loading', () => {
    const fixture = createFixture((host) => {
      host.loading.set(true);
    });
    expect(fixture.nativeElement.querySelector('app-skeleton')).not.toBeNull();
  });

  it('renders an error state with a working retry button', () => {
    const fixture = createFixture((host) => {
      host.error.set({ status: 500, title: 'خطأ', detail: 'فشل الخادم', validation: null });
    });
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('تعذّر تحميل تفاصيل العملية');
    const retryButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('إعادة المحاولة'),
    ) as HTMLButtonElement;
    retryButton.click();
    expect(fixture.componentInstance.retries).toBe(1);
  });
});
