import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import type { PagedStoreOperationsResponse } from './store-operations-api.service';
import { StoreOperationsPage } from './store-operations-page';
import { StoreOperationsStore } from './store-operations-store';

const OPERATIONS: PagedStoreOperationsResponse = {
  items: [
    {
      id: 'op-sale-1',
      invoiceNumber: 'INV-1001',
      operationType: 'Sale',
      operationTypeLabel: 'بيع',
      date: '2026-08-19T10:00:00',
      counterpartyName: 'ليلى',
      employeeName: 'أحمد',
      currency: 'JOD',
      currencySymbol: 'د.أ',
      totalAmount: 1500,
      amountPaid: 1500,
      remainingBalance: 0,
      status: 'Completed',
      statusLabel: 'مكتملة',
      accountName: 'الصندوق الرئيسي',
      itemsCount: 2,
    },
    {
      id: 'op-buy-1',
      invoiceNumber: 'PUR-2001',
      operationType: 'Buy',
      operationTypeLabel: 'شراء',
      date: '2026-08-19T11:00:00',
      counterpartyName: 'محمود',
      employeeName: 'سامر',
      currency: 'JOD',
      currencySymbol: 'د.أ',
      totalAmount: 800,
      amountPaid: 300,
      remainingBalance: 500,
      status: 'PartiallyPaid',
      statusLabel: 'مدفوعة جزئياً',
      accountName: null,
      itemsCount: 1,
    },
  ],
  totalCount: 25,
  page: 1,
  pageSize: 20,
  totalPages: 2,
};

let lastPagedUrl: URL | null = null;

const server = setupServer(
  http.get(apiUrl('/api/v1/dashboard/store-operations/kpis'), () =>
    HttpResponse.json({
      todaySalesCount: 3,
      todayPurchasesCount: 1,
      todaySalesTotals: [{ currency: 'JOD', symbol: 'د.أ', amount: 1250.5 }],
      todayPurchasesTotals: [{ currency: 'JOD', symbol: 'د.أ', amount: 400 }],
    }),
  ),
  http.get(apiUrl('/api/v1/dashboard/store-operations/employees'), () =>
    HttpResponse.json([
      { id: 'emp-1', name: 'أحمد' },
      { id: 'emp-2', name: 'سامر' },
    ]),
  ),
  http.get(apiUrl('/api/v1/dashboard/store-operations/today-employee-stats'), () =>
    HttpResponse.json([
      {
        employeeId: 'emp-1',
        employeeName: 'أحمد',
        salesCount: 2,
        salesWeight21K: 10.5,
        salesTotals: [{ currency: 'JOD', symbol: 'د.أ', amount: 900 }],
        purchasesCount: 0,
        purchasesWeight21K: 0,
        purchasesTotals: [],
      },
    ]),
  ),
  http.get(apiUrl('/api/v1/dashboard/store-operations'), ({ request }) => {
    lastPagedUrl = new URL(request.url);
    return HttpResponse.json(OPERATIONS);
  }),
  http.get(apiUrl('/api/v1/dashboard/store-operations/:id/detail'), () =>
    HttpResponse.json({
      id: 'op-sale-1',
      invoiceNumber: 'INV-1001',
      operationType: 'Sale',
      operationTypeLabel: 'بيع',
      date: '2026-08-19T10:00:00',
      counterpartyName: 'ليلى',
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
    }),
  ),
);

describe('StoreOperationsPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    lastPagedUrl = null;
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function createLoadedFixture(): Promise<ComponentFixture<StoreOperationsPage>> {
    const store = TestBed.inject(StoreOperationsStore);
    await Promise.all([
      store.loadKpis(),
      store.loadEmployees(),
      store.loadTodayStats(),
      store.loadOperations({ page: 1, pageSize: 20 }),
    ]);
    const fixture = TestBed.createComponent(StoreOperationsPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.tableLoading()).toBe(false));
    fixture.detectChanges();
    return fixture;
  }

  it('renders today KPI counts, currency totals and the operations table', async () => {
    const fixture = await createLoadedFixture();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('عمليات المتجر');
    expect(text).toContain('مبيعات اليوم');
    expect(text).toContain('3');
    expect(text).toContain('فاتورة');
    expect(text).toContain('INV-1001');
    expect(text).toContain('PUR-2001');
    expect(text).toContain('ليلى');
    expect(text).toContain('مدفوعة جزئياً');
  });

  it('renders the per-employee day stats table', async () => {
    const fixture = await createLoadedFixture();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('إحصائيات الموظفين اليوم');
    expect(text).toContain('أحمد');
    expect(text).toContain('وزن المبيعات 21 (غ)');
  });

  it('opens the detail dialog on the row action button and loads the detail', async () => {
    const fixture = await createLoadedFixture();
    const store = TestBed.inject(StoreOperationsStore);
    const loadDetail = vi.spyOn(store, 'loadDetail');

    const viewButtons = fixture.nativeElement.querySelectorAll(
      'tbody tr td:last-child button',
    ) as NodeListOf<HTMLButtonElement>;
    viewButtons[0].click();
    fixture.detectChanges();

    expect(loadDetail).toHaveBeenCalledWith('op-sale-1', 'Sale');
    await (loadDetail.mock.results[0]?.value as Promise<void>);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('INV-1001');
    expect(text).toContain('خواتم');
    expect(text).toContain('معادل 21');
  });

  it('applies the search filter from the input on submit', async () => {
    const fixture = await createLoadedFixture();
    const input = fixture.nativeElement.querySelector('input[type="text"]') as HTMLInputElement;
    input.value = 'INV';
    input.dispatchEvent(new Event('input'));

    const applyButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('تصفية'),
    ) as HTMLButtonElement;
    applyButton.click();

    await vi.waitFor(() => {
      expect(lastPagedUrl?.searchParams.get('search')).toBe('INV');
      expect(lastPagedUrl?.searchParams.get('page')).toBe('1');
    });
  });

  it('shows an inline retry state when the operations fetch fails', async () => {
    server.use(
      http.get(apiUrl('/api/v1/dashboard/store-operations'), () =>
        HttpResponse.json({ title: 'خطأ خادم' }, { status: 500 }),
      ),
    );
    const store = TestBed.inject(StoreOperationsStore);
    await store.loadOperations({ page: 1, pageSize: 20 });

    const fixture = TestBed.createComponent(StoreOperationsPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.tableLoading()).toBe(false));
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('تعذّر تحميل العمليات');
    expect(text).toContain('إعادة المحاولة');
  });
});
