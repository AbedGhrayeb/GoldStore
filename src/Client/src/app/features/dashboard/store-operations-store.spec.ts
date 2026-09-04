import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import type {
  EmployeeDayStatsResponse,
  PagedStoreOperationsResponse,
  StoreOperationDetailResponse,
  StoreOperationsKpiResponse,
} from './store-operations-api.service';
import { StoreOperationsStore } from './store-operations-store';

const KPIS: StoreOperationsKpiResponse = {
  todaySalesCount: 3,
  todayPurchasesCount: 1,
  todaySalesTotals: [
    { currency: 'JOD', symbol: 'د.أ', amount: 1250.5 },
    { currency: 'USD', symbol: '$', amount: 220 },
  ],
  todayPurchasesTotals: [{ currency: 'JOD', symbol: 'د.أ', amount: 400 }],
};

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
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
};

const EMPLOYEES = [
  { id: 'emp-1', name: 'أحمد' },
  { id: 'emp-2', name: 'سامر' },
];

const STATS: EmployeeDayStatsResponse[] = [
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
];

const DETAIL: StoreOperationDetailResponse = {
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
};

let pagedUrl: URL | null = null;
let kpisCalls = 0;
let statsCalls = 0;

const server = setupServer(
  http.get(apiUrl('/api/v1/dashboard/store-operations/kpis'), () => {
    kpisCalls += 1;
    return HttpResponse.json(KPIS);
  }),
  http.get(apiUrl('/api/v1/dashboard/store-operations/employees'), () =>
    HttpResponse.json(EMPLOYEES),
  ),
  http.get(apiUrl('/api/v1/dashboard/store-operations/today-employee-stats'), () => {
    statsCalls += 1;
    return HttpResponse.json(STATS);
  }),
  http.get(apiUrl('/api/v1/dashboard/store-operations'), ({ request }) => {
    pagedUrl = new URL(request.url);
    return HttpResponse.json(OPERATIONS);
  }),
  http.get(apiUrl('/api/v1/dashboard/store-operations/:id/detail'), ({ request }) => {
    const type = new URL(request.url).searchParams.get('operationType');
    return type === 'Sale' ? HttpResponse.json(DETAIL) : HttpResponse.json({}, { status: 404 });
  }),
);

describe('StoreOperationsStore', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    pagedUrl = null;
    kpisCalls = 0;
    statsCalls = 0;
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  it('loads today KPIs and exposes counts and per-currency totals', async () => {
    const store = TestBed.inject(StoreOperationsStore);

    await store.ensureKpis();

    expect(store.kpis()?.todaySalesCount).toBe(3);
    expect(store.kpis()?.todaySalesTotals?.[1].currency).toBe('USD');
    expect(store.kpisLoading()).toBe(false);
  });

  it('loads employees as { id, name } filter options (dashboard shape, not HR shape)', async () => {
    const store = TestBed.inject(StoreOperationsStore);

    await store.ensureEmployees();

    expect(store.employees()).toEqual([
      { id: 'emp-1', name: 'أحمد' },
      { id: 'emp-2', name: 'سامر' },
    ]);
  });

  it('sends the applied filters as query parameters on the paged call', async () => {
    const store = TestBed.inject(StoreOperationsStore);

    await store.loadOperations({
      page: 2,
      pageSize: 20,
      fromDate: '2026-08-01',
      toDate: '2026-08-19',
      operationType: 'Sale',
      employeeId: 'emp-1',
      accountId: 'acc-1',
      search: 'فاتورة',
    });

    expect(pagedUrl?.searchParams.get('page')).toBe('2');
    expect(pagedUrl?.searchParams.get('fromDate')).toBe('2026-08-01');
    expect(pagedUrl?.searchParams.get('toDate')).toBe('2026-08-19');
    expect(pagedUrl?.searchParams.get('operationType')).toBe('Sale');
    expect(pagedUrl?.searchParams.get('employeeId')).toBe('emp-1');
    expect(pagedUrl?.searchParams.get('accountId')).toBe('acc-1');
    expect(pagedUrl?.searchParams.get('search')).toBe('فاتورة');
    expect(store.page()?.totalCount).toBe(2);
    expect(store.tableLoading()).toBe(false);
  });

  it('omits empty filter values from the query string', async () => {
    const store = TestBed.inject(StoreOperationsStore);

    await store.loadOperations({ page: 1, pageSize: 20 });

    expect(pagedUrl?.searchParams.has('search')).toBe(false);
    expect(pagedUrl?.searchParams.has('fromDate')).toBe(false);
    expect(pagedUrl?.searchParams.has('operationType')).toBe(false);
  });

  it('surfaces a typed ApiError on paged failure and clears it on the next success', async () => {
    server.use(
      http.get(apiUrl('/api/v1/dashboard/store-operations'), () =>
        HttpResponse.json({ title: 'خطأ' }, { status: 500 }),
      ),
    );
    const store = TestBed.inject(StoreOperationsStore);

    await store.loadOperations({ page: 1, pageSize: 20 });

    expect(store.tableError()?.status).toBe(500);
    expect(store.page()).toBeNull();

    server.resetHandlers();
    await store.loadOperations({ page: 1, pageSize: 20 });

    expect(store.tableError()).toBeNull();
    expect((store.page() as any)?.items?.length).toBe(2);
  });

  it('loads today per-employee stats', async () => {
    const store = TestBed.inject(StoreOperationsStore);

    await store.loadTodayStats();

    expect(store.todayStats()).toHaveLength(1);
    expect(store.todayStats()[0].employeeName).toBe('أحمد');
    expect(store.statsLoading()).toBe(false);
  });

  it('loads the operation detail and clears it on close', async () => {
    const store = TestBed.inject(StoreOperationsStore);

    await store.loadDetail('op-sale-1', 'Sale');

    expect(store.detail()?.invoiceNumber).toBe('INV-1001');
    expect((store.detail() as any)?.items).toHaveLength(1);
    expect(store.detailLoading()).toBe(false);

    store.clearDetail();

    expect(store.detail()).toBeNull();
  });

  it('surfaces a typed ApiError when the detail is not found', async () => {
    const store = TestBed.inject(StoreOperationsStore);

    await store.loadDetail('op-buy-1', 'Buy');

    expect(store.detailError()?.status).toBe(404);
    expect(store.detail()).toBeNull();
  });

  it('refresh re-pulls KPIs and today stats', async () => {
    const store = TestBed.inject(StoreOperationsStore);

    await store.refresh();

    expect(kpisCalls).toBe(1);
    expect(statsCalls).toBe(1);
  });
});
