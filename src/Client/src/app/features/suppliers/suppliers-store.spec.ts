import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type {
  PagedSupplierFinancialTransactionResponse,
  SupplierFinancialKpiResponse,
  SupplierFinancialPaymentResponse,
  SupplierResponse,
} from './suppliers-api.service';
import { SuppliersStore } from './suppliers-store';

const SUPPLIER_1: SupplierResponse = {
  id: 'supplier-1',
  name: 'مؤسسة الذهب',
  primaryPhone: '0791111111',
  secondaryPhone: null,
  bankAccountNumber: null,
  notes: null,
  isActive: true,
  goldBalance: 125.5,
  manufacturingBalance: 40.25,
};

const SUPPLIER_2: SupplierResponse = {
  id: 'supplier-2',
  name: 'ورشة المجوهرات',
  primaryPhone: '0792222222',
  secondaryPhone: null,
  bankAccountNumber: null,
  notes: null,
  isActive: false,
  goldBalance: 0,
  manufacturingBalance: 0,
};

const PAGED: PagedSupplierFinancialTransactionResponse = {
  items: [
    {
      id: 'tx-1',
      supplierId: 'supplier-1',
      supplierName: 'مؤسسة الذهب',
      direction: 'FromSupplier',
      directionLabel: 'له',
      amount: 1000,
      currency: 'JOD',
      accountId: 'account-1',
      createdAt: '2026-08-01T10:00:00',
      outstandingBalance: 400,
      outstandingBalanceDisplay: '400.000',
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 15,
  totalPages: 1,
};

const KPIS: SupplierFinancialKpiResponse = {
  byCurrency: [
    {
      currency: 'JOD',
      symbol: 'د.أ',
      totalFromSupplier: 1500,
      totalToSupplier: 1100,
      netBalance: 400,
      transactionCount: 2,
      totalFromSupplierDisplay: '1,500.000',
      totalToSupplierDisplay: '1,100.000',
      netBalanceDisplay: '400.000',
    },
  ],
};

const PAYMENTS: SupplierFinancialPaymentResponse[] = [
  {
    id: 'payment-1',
    amount: 300,
    accountName: 'صندوق النقد',
    date: '2026-08-05T09:00:00',
    notes: 'دفعة أولى',
  },
];

const BASE = apiUrl('/api/v1');
const SUPPLIERS = `${BASE}/suppliers`;
const PAGED_SUPPLIERS = {
  items: [SUPPLIER_1, SUPPLIER_2],
  pageNumber: 1,
  pageSize: 15,
  totalCount: 2,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};
const DELIVERIES = `${BASE}/supplier-deliveries`;
const PAYMENTS_ENDPOINT = `${BASE}/supplier-payments`;
const TRANSACTIONS = `${BASE}/supplier-financial-transactions`;
const ACCOUNTS = `${BASE}/finance/accounts`;

describe('SuppliersStore', () => {
  let store: SuppliersStore;
  let toasts: ToastStore;
  let suppliers: SupplierResponse[];

  const server = setupServer(
    http.get(SUPPLIERS, () => HttpResponse.json(suppliers)),
    http.get(`${SUPPLIERS}/paged`, () => HttpResponse.json(PAGED_SUPPLIERS)),
    http.post(SUPPLIERS, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      const newSupplier: SupplierResponse = {
        id: 'supplier-new',
        name: body['name'] as string,
        primaryPhone: body['primaryPhone'] as string,
        isActive: true,
        goldBalance: 0,
        manufacturingBalance: 0,
      };
      suppliers = [...suppliers, newSupplier];
      return HttpResponse.json('supplier-new', { status: 201 });
    }),
    http.put(`${SUPPLIERS}/:id`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json(true);
    }),
    http.post(`${SUPPLIERS}/:id/toggle-active`, () => HttpResponse.json(true)),
    http.get(`${SUPPLIERS}/:id`, () =>
      HttpResponse.json({ id: 'supplier-1', name: 'مؤسسة الذهب', goldBalance: 125.5 }),
    ),
    http.post(DELIVERIES, async ({ request }) => {
      const body = (await request.json()) as {
        lines: {
          karat: number;
          weightInGrams: number;
          categoryId: string | null;
        }[];
      };
      expect(Object.keys(body)).not.toContain('tenantId');
      expect(Object.keys(body).sort()).toEqual(
        [
          'amountDue',
          'amountDueCurrency',
          'lines',
          'manufacturingFeeCurrency',
          'manufacturingFeePerGram',
          'notes',
          'paymentLegs',
          'supplierId',
        ].sort(),
      );
      for (const line of body.lines) {
        expect(Object.keys(line).sort()).toEqual(['categoryId', 'karat', 'weightInGrams']);
      }
      return HttpResponse.json('delivery-1', { status: 201 });
    }),
    http.post(`${PAYMENTS_ENDPOINT}/scrap-gold`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      for (const key of ['karat', 'notes', 'supplierId', 'weightInGrams']) {
        expect(key in body).toBe(true);
      }
      for (const key of Object.keys(body)) {
        expect(['karat', 'notes', 'supplierId', 'weightInGrams', 'categoryId']).toContain(key);
      }
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('payment-1', { status: 201 });
    }),
    http.post(`${PAYMENTS_ENDPOINT}/manufacturing`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'accountId',
        'amount',
        'currency',
        'notes',
        'paymentLegs',
        'supplierId',
      ]);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('payment-2', { status: 201 });
    }),
    http.get(`${TRANSACTIONS}/kpis`, () => HttpResponse.json(KPIS)),
    http.get(TRANSACTIONS, () => HttpResponse.json(PAGED)),
    http.post(TRANSACTIONS, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('tx-new', { status: 201 });
    }),
    http.get(`${TRANSACTIONS}/:transactionId/payments`, () => HttpResponse.json(PAYMENTS)),
    http.post(`${TRANSACTIONS}/:transactionId/payments`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('payment-new', { status: 201 });
    }),
    http.get(ACCOUNTS, () =>
      HttpResponse.json([
        { id: 'account-1', name: 'صندوق النقد', currency: 'JOD', isActive: true },
      ]),
    ),
  );

  beforeEach(() => {
    suppliers = [SUPPLIER_1, SUPPLIER_2];
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    store = TestBed.inject(SuppliersStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads the supplier list once for concurrent ensureLoaded calls', async () => {
    await Promise.all([store.ensureLoaded(), store.ensureLoaded(), store.ensureLoaded()]);

    expect(store.suppliers()).toHaveLength(2);
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('loads the paged supplier list with search and status filter', async () => {
    await store.loadSuppliersPage({ page: 1, pageSize: 15, search: 'ذهب', activeOnly: true });

    expect(store.suppliersPage()?.totalCount).toBe(2);
    expect(store.suppliersPage()?.items).toHaveLength(2);
    expect(store.suppliersPageLoading()).toBe(false);
    expect(store.suppliersPageError()).toBeNull();
  });

  it('creates a supplier with a tenantId-free payload and reloads', async () => {
    const ok = await store.createSupplier({
      name: 'مورد جديد',
      primaryPhone: '0793333333',
      secondaryPhone: null,
      bankAccountNumber: null,
      notes: null,
    });

    expect(ok).toBe(true);
    expect(store.suppliers()?.some((supplier) => supplier.id === 'supplier-new')).toBe(true);
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('updates a supplier and toggles its active state', async () => {
    const ok = await store.updateSupplier('supplier-1', {
      name: 'مؤسسة الذهب المحدّثة',
      primaryPhone: '0791111111',
      secondaryPhone: null,
      bankAccountNumber: null,
      notes: null,
    });

    expect(ok).toBe(true);

    await store.toggleActive('supplier-1');
    expect(store.mutatingId()).toBeNull();
    expect(store.suppliers()).toHaveLength(2);
  });

  it('posts a delivery with header-level due amount and payment legs', async () => {
    const ok = await store.createDelivery({
      supplierId: 'supplier-1',
      lines: [
        { karat: 21, weightInGrams: 50.5, categoryId: 'cat-1' },
        { karat: 18, weightInGrams: 10, categoryId: null },
      ],
      manufacturingFeePerGram: 0.5,
      manufacturingFeeCurrency: 'JOD',
      amountDue: 100,
      amountDueCurrency: 'JOD',
      paymentLegs: [{ accountId: 'account-1', currency: 'JOD', amount: 40, exchangeRate: 1 }],
      notes: 'تسليم أول',
    });

    expect(ok).toBe(true);
    expect(store.suppliers()).toHaveLength(2);
  });

  it('posts a scrap-gold payment with only karat + weightInGrams (no tenantId)', async () => {
    const ok = await store.createScrapGoldPayment({
      supplierId: 'supplier-1',
      karat: 21,
      weightInGrams: 25,
      notes: null,
    });

    expect(ok).toBe(true);
  });

  it('posts a manufacturing payment with a tenantId-free payload', async () => {
    const ok = await store.createManufacturingPayment({
      supplierId: 'supplier-1',
      accountId: 'account-1',
      amount: 120,
      currency: 'JOD',
      paymentLegs: null,
      notes: null,
    });

    expect(ok).toBe(true);
  });

  it('loads KPIs, the paged transactions and the best-effort accounts', async () => {
    await store.loadKpis();
    await store.loadTransactions({ page: 1, pageSize: 15 });
    await store.ensureAccounts();

    expect(store.kpis()?.byCurrency?.[0]?.netBalanceDisplay).toBe('400.000');
    expect(store.page()?.items?.[0]?.supplierName).toBe('مؤسسة الذهب');
    expect(store.accounts()).toHaveLength(1);
    expect(store.accountsError()).toBeNull();
  });

  it('degrades gracefully when the finance-gated accounts endpoint is forbidden', async () => {
    server.use(
      http.get(ACCOUNTS, () =>
        HttpResponse.json({ title: 'Forbidden', status: 403 }, { status: 403 }),
      ),
    );

    await store.ensureAccounts();

    expect(store.accounts()).toHaveLength(0);
    expect(store.accountsError()).not.toBeNull();
  });

  it('creates a transaction and a payment without a tenantId in either payload', async () => {
    const okTx = await store.createTransaction({
      supplierId: 'supplier-1',
      direction: 1,
      amount: 500,
      currency: 'JOD',
      accountId: 'account-1',
      date: '2026-08-10T00:00:00.000Z',
      notes: null,
    });
    expect(okTx).toBe(true);

    const okPayment = await store.createPayment('tx-1', {
      accountId: 'account-1',
      amount: 200,
      date: '2026-08-11T00:00:00.000Z',
      notes: null,
    });
    expect(okPayment).toBe(true);
  });

  it('loads and clears the payments of one transaction', async () => {
    await store.loadPayments('tx-1');

    expect(store.payments()).toHaveLength(1);
    expect(store.payments()[0]?.accountName).toBe('صندوق النقد');

    store.clearPayments();
    expect(store.payments()).toHaveLength(0);
  });
});
