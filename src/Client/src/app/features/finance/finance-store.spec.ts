import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type {
  CreateAccountInput,
  CreateDebtInput,
  DebtPaymentInput,
  SetBalanceInput,
} from './finance-api.service';
import { FinanceStore } from './finance-store';

const BASE = apiUrl('/api/v1');
const ACCOUNTS = `${BASE}/finance/accounts`;
const WITH_BALANCES = `${ACCOUNTS}/with-balances`;
const DEBTS = `${BASE}/finance/debts`;
const TRANSACTIONS = `${BASE}/finance/transactions`;

function accountInput(): CreateAccountInput {
  return {
    name: 'صندوق النقدية',
    currency: 'JOD',
    accountNumber: null,
    notes: null,
    openingBalance: 500,
  };
}

function debtInput(): CreateDebtInput {
  return {
    name: 'أحمد خليل',
    phone: null,
    direction: 1,
    currency: 'JOD',
    accountId: 'acc-1',
    amount: 300,
    notes: null,
    date: '2026-08-21T00:00:00.000Z',
  };
}

function paymentInput(): DebtPaymentInput {
  return {
    accountId: 'acc-1',
    amount: 150,
    date: '2026-08-21T00:00:00.000Z',
    notes: null,
  };
}

describe('FinanceStore', () => {
  let store: FinanceStore;
  let toasts: ToastStore;

  const server = setupServer(
    http.get(WITH_BALANCES, ({ request }) => {
      const url = new URL(request.url);
      expect(url.searchParams.get('activeOnly')).toBe('false');
      if (url.searchParams.get('currency') === 'USD') {
        return HttpResponse.json([]);
      }
      return HttpResponse.json([
        {
          id: 'acc-1',
          name: 'صندوق النقدية',
          currency: 'JOD',
          accountType: 'Cash',
          accountNumber: 'JOD-1234',
          isActive: true,
          balance: 1200,
          lastChangeAmount: 300,
          lastChangeDirection: 'Inflow',
        },
        {
          id: 'acc-2',
          name: 'الحساب البنكي',
          currency: 'JOD',
          accountType: 'Bank',
          accountNumber: null,
          isActive: false,
          balance: -50,
          lastChangeAmount: 50,
          lastChangeDirection: 'Outflow',
        },
      ]);
    }),
    http.post(ACCOUNTS, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'accountNumber',
        'currency',
        'name',
        'notes',
        'openingBalance',
      ]);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('acc-new', { status: 201 });
    }),
    http.put(`${ACCOUNTS}/acc-1/balance`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual(['notes', 'targetBalance']);
      expect(body['targetBalance']).toBe(900);
      return HttpResponse.json(null, { status: 200 });
    }),
    http.get(`${DEBTS}/kpis`, () =>
      HttpResponse.json({
        totalReceivables: 800,
        totalPayables: 300,
        receivableCount: 2,
        payableCount: 1,
        netBalance: 500,
        totalReceivablesDisplay: '800.000',
        totalPayablesDisplay: '300.000',
        netBalanceDisplay: '500.000',
        isNetPositive: true,
        byCurrency: [],
      }),
    ),
    http.get(DEBTS, ({ request }) => {
      const url = new URL(request.url);
      const direction = url.searchParams.get('direction');
      const search = url.searchParams.get('search');
      const items =
        direction === 'Receivable' || search === 'أحمد'
          ? [
              {
                id: 'debt-1',
                name: 'أحمد خليل',
                phone: null,
                direction: 'Receivable',
                directionLabel: 'ذمة مدينة',
                currency: 'JOD',
                accountId: 'acc-1',
                notes: null,
                createdAt: '2026-08-21T10:00:00.000Z',
                outstandingBalance: 300,
                outstandingBalanceDisplay: '300.000',
              },
            ]
          : [];
      const page = Number(url.searchParams.get('page') ?? 1);
      const pageSize = Number(url.searchParams.get('pageSize') ?? 15);
      return HttpResponse.json({
        pageNumber: page,
        pageSize,
        totalCount: items.length,
        totalPages: Math.ceil(items.length / pageSize),
        items,
        hasPreviousPage: false,
        hasNextPage: false,
      });
    }),
    http.post(DEBTS, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'accountId',
        'amount',
        'currency',
        'date',
        'direction',
        'name',
        'notes',
        'phone',
      ]);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('debt-new', { status: 201 });
    }),
    http.post(`${DEBTS}/debt-1/payments`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual(['accountId', 'amount', 'date', 'notes']);
      expect(body['amount']).toBe(150);
      return HttpResponse.json(null, { status: 200 });
    }),
    http.get(TRANSACTIONS, ({ request }) => {
      const url = new URL(request.url);
      const currency = url.searchParams.get('currency');
      const items =
        currency === 'JOD'
          ? [
              {
                id: 'tx-1',
                date: '2026-08-21T09:30:00.000Z',
                description: 'إنشاء ذمة مدينة: أحمد خليل',
                accountName: 'صندوق النقدية',
                amount: 300,
                currency: 'JOD',
                transactionType: 'Outflow',
                referenceType: 'DebtCreation',
              },
            ]
          : [];
      const page = Number(url.searchParams.get('page') ?? 1);
      const pageSize = Number(url.searchParams.get('pageSize') ?? 15);
      return HttpResponse.json({
        pageNumber: page,
        pageSize,
        totalCount: items.length,
        totalPages: Math.ceil(items.length / pageSize),
        items,
        hasPreviousPage: false,
        hasNextPage: false,
      });
    }),
  );

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    store = TestBed.inject(FinanceStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads the accounts with ledger-derived balances and filters by currency', async () => {
    await store.loadAccounts();

    expect(store.accountsLoading()).toBe(false);
    expect(store.accountsError()).toBeNull();
    expect(store.accounts()).toHaveLength(2);
    expect(store.accounts()?.[0]?.balance).toBe(1200);

    await store.loadAccounts({ currency: 'USD' });
    expect(store.accounts()).toHaveLength(0);
  });

  it('surfaces an accounts load failure without a toast', async () => {
    server.use(
      http.get(WITH_BALANCES, () => HttpResponse.json({ detail: 'خطأ داخلي' }, { status: 500 })),
    );

    await store.loadAccounts();

    expect(store.accounts()).toBeNull();
    expect(store.accountsError()?.detail).toBe('خطأ داخلي');
    expect(toasts.toasts().length).toBe(0);
  });

  it('creates an account with the exact request keys (no tenantId) and toasts', async () => {
    const ok = await store.createAccount(accountInput());

    expect(ok).toBe(true);
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('sets a balance with the exact request keys', async () => {
    const ok = await store.setBalance('acc-1', { targetBalance: 900, notes: null } satisfies SetBalanceInput);

    expect(ok).toBe(true);
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('loads the debt KPIs', async () => {
    await store.loadDebtKpis();

    expect(store.debtKpisLoading()).toBe(false);
    expect(store.debtKpis()?.netBalanceDisplay).toBe('500.000');
    expect(store.debtKpis()?.isNetPositive).toBe(true);
  });

  it('loads the paged debts with direction and search filters', async () => {
    await store.loadDebts({ page: 1, pageSize: 15, direction: 'Receivable' });

    expect(store.debtsError()).toBeNull();
    expect(store.debtsPage()?.items?.[0]?.directionLabel).toBe('ذمة مدينة');
    expect(store.debtsPage()?.totalCount).toBe(1);

    await store.loadDebts({ page: 1, pageSize: 15, search: 'أحمد' });
    expect(store.debtsPage()?.items?.[0]?.name).toBe('أحمد خليل');

    await store.loadDebts({ page: 1, pageSize: 15 });
    expect(store.debtsPage()?.items).toHaveLength(0);
  });

  it('creates a debt with the exact request keys (no tenantId)', async () => {
    const ok = await store.createDebt(debtInput());

    expect(ok).toBe(true);
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('pays a debt with the exact request keys', async () => {
    const ok = await store.payDebt('debt-1', paymentInput());

    expect(ok).toBe(true);
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('surfaces a create failure with the server validation message, no success toast', async () => {
    server.use(
      http.post(DEBTS, () =>
        HttpResponse.json(
          { title: 'طلب غير صالح', errors: { Amount: ['المبلغ يجب أن يكون أكبر من صفر.'] } },
          { status: 400 },
        ),
      ),
    );

    const ok = await store.createDebt({ ...debtInput(), amount: 0 });

    expect(ok).toBe(false);
    expect(store.saveError()?.validation?.['Amount']?.[0]).toBe('المبلغ يجب أن يكون أكبر من صفر.');
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(false);
  });

  it('surfaces a currency-mismatch conflict when paying with a foreign-currency account', async () => {
    server.use(
      http.post(`${DEBTS}/debt-1/payments`, () =>
        HttpResponse.json(
          { title: 'تعارض', detail: 'عملة الحساب لا تطابق عملة الذمة' },
          { status: 409 },
        ),
      ),
    );

    const ok = await store.payDebt('debt-1', paymentInput());

    expect(ok).toBe(false);
    expect(store.saveError()?.detail).toBe('عملة الحساب لا تطابق عملة الذمة');
  });

  it('loads the paged transactions with filters', async () => {
    await store.loadTransactions({ page: 1, pageSize: 15, currency: 'JOD' });

    expect(store.txError()).toBeNull();
    expect(store.txPage()?.items?.[0]?.transactionType).toBe('Outflow');
    expect(store.txPage()?.totalCount).toBe(1);

    await store.loadTransactions({ page: 1, pageSize: 15, currency: 'USD' });
    expect(store.txPage()?.items).toHaveLength(0);
  });
});
