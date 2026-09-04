import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type {
  PaginatedSalesInvoices,
  SalesInvoiceKpiResponse,
  SalesInvoiceResponse,
} from './sales-api.service';
import { SalesStore } from './sales-store';

const INVOICES_PAGE: PaginatedSalesInvoices = {
  items: [
    {
      id: 'inv-1',
      invoiceNumber: 'INV-2026-08-0001',
      customerName: 'أحمد خالد',
      customerPhone: '0791000001',
      date: '2026-08-20T10:00:00',
      currency: 'JOD',
      totalAmount: 850.5,
      amountPaid: 850.5,
      remainingBalance: 0,
      paymentMethod: 'Cash',
      status: 'Completed',
      statusLabel: 'مكتملة',
      userName: 'عمر',
      notes: null,
      createdAt: '2026-08-20T10:00:00',
      items: [],
    },
  ],
  totalCount: 1,
  pageNumber: 1,
  pageSize: 15,
  totalPages: 1,
};

const INVOICE_DETAIL: SalesInvoiceResponse = {
  ...INVOICES_PAGE.items![0]!,
  items: [
    {
      id: 'line-1',
      categoryName: null,
      karat: 21,
      weightInGrams: 25,
      equivalent21KWeightInGrams: 25,
      pricePerGram: 34.02,
      goldAmount: 850.5,
    },
  ],
};

const KPIS: SalesInvoiceKpiResponse = {
  todayCount: 4,
  totalSales: 1234.5,
  totalSalesDisplay: '1234.500',
  totalPaid: 900,
  totalPaidDisplay: '900.000',
  totalRemaining: 334.5,
  totalRemainingDisplay: '334.500',
};

const BASE = apiUrl('/api/v1');
const INVOICES = `${BASE}/sales-invoices`;
const EMPLOYEES = `${BASE}/employees`;
const CATEGORIES = `${BASE}/categories`;
const ACCOUNTS = `${BASE}/finance/accounts`;

describe('SalesStore', () => {
  let store: SalesStore;
  let toasts: ToastStore;

  const server = setupServer(
    http.get(`${INVOICES}/kpis`, () => HttpResponse.json(KPIS)),
    http.get(`${INVOICES}/next-number`, () => HttpResponse.json('INV-2026-08-0005')),
    http.get(`${INVOICES}/:id`, () => HttpResponse.json(INVOICE_DETAIL)),
    http.get(INVOICES, () => HttpResponse.json(INVOICES_PAGE)),
    http.post(INVOICES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'accountId',
        'amountPaid',
        'buyerAccountNumber',
        'currency',
        'customerName',
        'customerPhone',
        'date',
        'employeeId',
        'items',
        'notes',
        'paymentLegs',
        'paymentMethod',
        'totalAmount',
      ]);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('inv-new', { status: 201 });
    }),
    http.get(EMPLOYEES, () =>
      HttpResponse.json([{ id: 'emp-1', fullName: 'عمر حسن', isActive: true }]),
    ),
    http.get(CATEGORIES, () =>
      HttpResponse.json([{ id: 'cat-1', name: 'خواتم', isActive: true }]),
    ),
    http.get(ACCOUNTS, () =>
      HttpResponse.json([
        { id: 'acc-1', name: 'صندوق النقدية', currency: 'JOD', accountType: 'Cash', isActive: true },
        { id: 'acc-2', name: 'الحساب البنكي', currency: 'JOD', accountType: 'Bank', isActive: true },
      ]),
    ),
  );

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    store = TestBed.inject(SalesStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads KPIs, the next number, the paged invoices and the option lists', async () => {
    await store.loadKpis();
    await store.loadNextNumber();
    await store.loadInvoices({ page: 1, pageSize: 15 });
    await store.ensureEmployees();
    await store.ensureCategories();
    await store.ensureAccounts();

    expect(store.kpis()?.totalSalesDisplay).toBe('1234.500');
    expect(store.kpis()?.todayCount).toBe(4);
    expect(store.nextNumber()).toBe('INV-2026-08-0005');
    expect((store.page() as any)?.items?.[0]?.invoiceNumber).toBe('INV-2026-08-0001');
    expect(store.employees()[0]?.fullName).toBe('عمر حسن');
    expect(store.categories()[0]?.name).toBe('خواتم');
    expect(store.accounts()).toHaveLength(2);
  });

  it('creates an invoice with the exact request keys (no tenantId) and toasts', async () => {
    const ok = await store.createInvoice({
      customerName: 'مريم سليم',
      customerPhone: '0791000002',
      date: '2026-08-20T00:00:00.000Z',
      currency: 'JOD',
      items: [{ categoryId: 'cat-1', karat: 21, weightInGrams: 20, pricePerGram: 30 }],
      totalAmount: 600,
      amountPaid: 250,
      paymentMethod: 2,
      accountId: 'acc-2',
      buyerAccountNumber: 'JO1234',
      employeeId: 'emp-1',
      paymentLegs: null,
      notes: null,
    });

    expect(ok).toBe(true);
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('creates an invoice with multi-currency payment legs', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(INVOICES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('inv-new', { status: 201 });
      }),
    );

    const ok = await store.createInvoice({
      customerName: 'سامي',
      customerPhone: null,
      date: '2026-08-20T00:00:00.000Z',
      currency: 'JOD',
      items: [{ categoryId: null, karat: 18, weightInGrams: 10, pricePerGram: 25 }],
      totalAmount: 250,
      amountPaid: 250,
      paymentMethod: 1,
      accountId: 'acc-1',
      buyerAccountNumber: null,
      employeeId: 'emp-1',
      paymentLegs: [
        { accountId: 'acc-1', currency: 'JOD', amount: 100, exchangeRate: 1 },
        { accountId: 'acc-2', currency: 'USD', amount: 211.86, exchangeRate: 0.708 },
      ],
      notes: 'دفعة على دفعتين',
    });

    expect(ok).toBe(true);
    expect(captured?.['paymentLegs']).toEqual([
      { accountId: 'acc-1', currency: 'JOD', amount: 100, exchangeRate: 1 },
      { accountId: 'acc-2', currency: 'USD', amount: 211.86, exchangeRate: 0.708 },
    ]);
    expect(captured?.['amountPaid']).toBe(250);
  });

  it('passes filter query params (search + status + dates)', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(INVOICES, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(INVOICES_PAGE);
      }),
    );

    await store.loadInvoices({
      page: 1,
      pageSize: 15,
      search: 'مريم',
      status: 'PartiallyPaid',
      fromDate: '2026-08-01',
      toDate: '2026-08-31',
    });

    expect((captured as any)?.get('search')).toBe('مريم');
    expect((captured as any)?.get('status')).toBe('PartiallyPaid');
    expect((captured as any)?.get('fromDate')).toBe('2026-08-01');
    expect((captured as any)?.get('toDate')).toBe('2026-08-31');
  });

  it('loads a single invoice detail', async () => {
    await store.loadDetail('inv-1');

    expect(store.detail()?.invoiceNumber).toBe('INV-2026-08-0001');
    expect((store.detail() as any)?.items?.[0]?.equivalent21KWeightInGrams).toBe(25);
  });

  it('surfaces a list error without toasting it', async () => {
    server.use(http.get(INVOICES, () => HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 })));

    await store.loadInvoices({ page: 1, pageSize: 15 });

    expect(store.error()?.detail).toBe('خطأ خادم');
    expect(store.page()).toBeNull();
    expect(toasts.toasts().length).toBe(0);
  });

  it('surfaces a create failure with the server validation message, no success toast', async () => {
    server.use(
      http.post(INVOICES, () =>
        HttpResponse.json(
          { title: 'طلب غير صالح', errors: { totalAmount: ['المبلغ المستحق أكبر من صفر.'] } },
          { status: 400 },
        ),
      ),
    );

    const ok = await store.createInvoice({
      customerName: 'مريم',
      customerPhone: null,
      date: '2026-08-20T00:00:00.000Z',
      currency: 'JOD',
      items: [{ categoryId: null, karat: 21, weightInGrams: 5, pricePerGram: 30 }],
      totalAmount: 0,
      amountPaid: 0,
      paymentMethod: 1,
      accountId: null,
      buyerAccountNumber: null,
      employeeId: 'emp-1',
      paymentLegs: null,
      notes: null,
    });

    expect(ok).toBe(false);
    expect(store.saveError()?.validation?.['totalAmount']?.[0]).toBe('المبلغ المستحق أكبر من صفر.');
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(false);
  });

  it('degrades gracefully when the employees endpoint is gated behind another feature', async () => {
    server.use(http.get(EMPLOYEES, () => HttpResponse.json({ detail: 'غير مصرح' }, { status: 403 })));

    await store.ensureEmployees();

    expect(store.employees()).toHaveLength(0);
    expect(store.employeesError()).toContain('الموظفون');
    expect(toasts.toasts().length).toBe(0);
  });
});