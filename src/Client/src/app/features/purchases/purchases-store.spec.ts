import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type { CreateCustomerPurchaseInput } from './purchases-api.service';
import { PurchasesStore } from './purchases-store';

const BASE = apiUrl('/api/v1');
const INVOICES = `${BASE}/customer-purchases/invoices`;
const EMPLOYEES = `${BASE}/employees`;
const CATEGORIES = `${BASE}/categories`;
const ACCOUNTS = `${BASE}/finance/accounts`;

function validInput(): CreateCustomerPurchaseInput {
  return {
    sellerName: 'مريم سليم',
    sellerPhone: '0791000002',
    sellerIdNumber: '123456789',
    sellerYearOfBirth: 1990,
    sellerAddress: 'عمان',
    employeeId: 'emp-1',
    currency: 'JOD',
    date: '2026-08-20T00:00:00.000Z',
    totalAmount: 600,
    amountPaid: 250,
    paymentMethod: 2,
    accountId: 'acc-2',
    sellerAccountNumber: 'JO1234',
    notes: null,
    items: [{ categoryId: 'cat-1', karat: 21, weightInGrams: 20, pricePerGram: 30 }],
    paymentLegs: null,
  };
}

describe('PurchasesStore', () => {
  let store: PurchasesStore;
  let toasts: ToastStore;

  const server = setupServer(
    http.get(`${INVOICES}/next-number`, () => HttpResponse.json('PUR-2026-08-0005')),
    http.get(`${INVOICES}/kpis`, () =>
      HttpResponse.json({
        todayCount: 3,
        totalPurchases: 1800,
        totalPurchasesDisplay: '1,800.000',
        totalPaid: 900,
        totalPaidDisplay: '900.000',
        totalRemaining: 900,
        totalRemainingDisplay: '900.000',
      }),
    ),
    http.get(`${INVOICES}/pur-1`, () =>
      HttpResponse.json({
        id: 'pur-1',
        invoiceNumber: 'PUR-2026-08-0001',
        sellerName: 'مريم سليم',
        sellerPhone: '0791000002',
        sellerIdNumber: '123456789',
        sellerYearOfBirth: 1990,
        sellerAddress: 'عمان',
        employeeName: 'عمر حسن',
        date: '2026-08-20T00:00:00.000Z',
        currency: 'JOD',
        totalAmount: 600,
        amountPaid: 250,
        remainingBalance: 350,
        paymentMethod: 'Bank',
        sellerAccountNumber: 'JO1234',
        notes: null,
        createdAt: '2026-08-20T10:00:00.000Z',
        items: [
          {
            id: 'item-1',
            categoryId: 'cat-1',
            karat: 21,
            weightInGrams: 20,
            equivalent21KWeightInGrams: 20,
            pricePerGram: 30,
            goldAmount: 600,
          },
        ],
      }),
    ),
    http.get(INVOICES, ({ request }) => {
      const url = new URL(request.url);
      const search = url.searchParams.get('search');
      const page = url.searchParams.get('page');
      const pageSize = url.searchParams.get('pageSize');
      const items =
        search === 'مريم'
          ? [
              {
                id: 'pur-1',
                invoiceNumber: 'PUR-2026-08-0001',
                sellerName: 'مريم سليم',
                sellerPhone: '0791000002',
                sellerIdNumber: '123456789',
                sellerYearOfBirth: 1990,
                sellerAddress: 'عمان',
                employeeName: 'عمر حسن',
                date: '2026-08-20T00:00:00.000Z',
                currency: 'JOD',
                totalAmount: 600,
                amountPaid: 250,
                remainingBalance: 350,
                paymentMethod: 'Bank',
                sellerAccountNumber: 'JO1234',
                notes: null,
                createdAt: '2026-08-20T10:00:00.000Z',
                items: [],
              },
            ]
          : [];
      return HttpResponse.json({
        pageNumber: Number(page ?? 1),
        pageSize: Number(pageSize ?? 15),
        totalCount: items.length,
        totalPages: Math.ceil(items.length / Number(pageSize ?? 15)),
        items,
        hasPreviousPage: false,
        hasNextPage: false,
      });
    }),
    http.post(INVOICES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'accountId',
        'amountPaid',
        'currency',
        'date',
        'employeeId',
        'items',
        'notes',
        'paymentLegs',
        'paymentMethod',
        'sellerAccountNumber',
        'sellerAddress',
        'sellerIdNumber',
        'sellerName',
        'sellerPhone',
        'sellerYearOfBirth',
        'totalAmount',
      ]);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('pur-new', { status: 201 });
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
    store = TestBed.inject(PurchasesStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads the next number and the option lists', async () => {
    await store.loadNextNumber();
    await store.ensureEmployees();
    await store.ensureCategories();
    await store.ensureAccounts();

    expect(store.nextNumber()).toBe('PUR-2026-08-0005');
    expect(store.employees()[0]?.fullName).toBe('عمر حسن');
    expect(store.categories()[0]?.name).toBe('خواتم');
    expect(store.accounts()).toHaveLength(2);
  });

  it('loads the paged invoice list with filters', async () => {
    await store.loadInvoices({ page: 1, pageSize: 15, search: 'مريم' });

    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
    expect((store.page() as any)?.items[0]?.invoiceNumber).toBe('PUR-2026-08-0001');
    expect(store.page()?.totalCount).toBe(1);
  });

  it('surfaces a list load failure without a toast', async () => {
    server.use(http.get(INVOICES, () => HttpResponse.json({ detail: 'خطأ داخلي' }, { status: 500 })));

    await store.loadInvoices({ page: 1 });

    expect(store.page()).toBeNull();
    expect(store.error()?.detail).toBe('خطأ داخلي');
    expect(toasts.toasts().length).toBe(0);
  });

  it('loads the KPIs', async () => {
    await store.loadKpis();

    expect(store.kpisLoading()).toBe(false);
    expect(store.kpis()?.todayCount).toBe(3);
    expect(store.kpis()?.totalPurchasesDisplay).toBe('1,800.000');
  });

  it('loads and clears the invoice detail', async () => {
    await store.loadDetail('pur-1');

    expect(store.detailLoading()).toBe(false);
    expect(store.detail()?.invoiceNumber).toBe('PUR-2026-08-0001');
    expect((store.detail() as any)?.items[0]?.goldAmount).toBe(600);

    store.clearDetail();
    expect(store.detail()).toBeNull();
  });

  it('surfaces a detail load failure', async () => {
    server.use(http.get(`${INVOICES}/pur-1`, () => HttpResponse.json({ detail: 'غير موجود' }, { status: 404 })));

    await store.loadDetail('pur-1');

    expect(store.detail()).toBeNull();
    expect(store.detailError()?.detail).toBe('غير موجود');
  });

  it('creates an invoice with the exact request keys (no tenantId) and toasts', async () => {
    const ok = await store.createInvoice(validInput());

    expect(ok).toBe(true);
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('creates an invoice with multi-currency payment legs', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(INVOICES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('pur-new', { status: 201 });
      }),
    );

    const ok = await store.createInvoice({
      ...validInput(),
      sellerPhone: null,
      sellerAddress: null,
      sellerYearOfBirth: null,
      amountPaid: 250,
      paymentMethod: 1,
      accountId: 'acc-1',
      sellerAccountNumber: null,
      paymentLegs: [
        { accountId: 'acc-1', currency: 'JOD', amount: 100, exchangeRate: 1 },
        { accountId: 'acc-2', currency: 'USD', amount: 211.86, exchangeRate: 0.708 },
      ],
    });

    expect(ok).toBe(true);
    expect(captured?.['paymentLegs']).toEqual([
      { accountId: 'acc-1', currency: 'JOD', amount: 100, exchangeRate: 1 },
      { accountId: 'acc-2', currency: 'USD', amount: 211.86, exchangeRate: 0.708 },
    ]);
    expect(captured?.['amountPaid']).toBe(250);
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

    const ok = await store.createInvoice({ ...validInput(), totalAmount: 0 });

    expect(ok).toBe(false);
    expect(store.saveError()?.validation?.['totalAmount']?.[0]).toBe('المبلغ المستحق أكبر من صفر.');
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(false);
  });

  it('degrades gracefully when the option endpoints are gated behind other features', async () => {
    server.use(
      http.get(EMPLOYEES, () => HttpResponse.json({ detail: 'غير مصرح' }, { status: 403 })),
      http.get(CATEGORIES, () => HttpResponse.json({ detail: 'غير مصرح' }, { status: 403 })),
      http.get(ACCOUNTS, () => HttpResponse.json({ detail: 'غير مصرح' }, { status: 403 })),
    );

    await store.ensureEmployees();
    await store.ensureCategories();
    await store.ensureAccounts();

    expect(store.employees()).toHaveLength(0);
    expect(store.employeesError()).toContain('الموظفون');
    expect(store.categories()).toHaveLength(0);
    expect(store.categoriesError()).toContain('الكتالوج');
    expect(store.accounts()).toHaveLength(0);
    expect(store.accountsError()).toContain('المالية');
    expect(toasts.toasts().length).toBe(0);
  });
});