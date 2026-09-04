import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { FinancePage } from './finance-page';
import { FinanceStore } from './finance-store';

const BASE = apiUrl('/api/v1');
const ACCOUNTS = `${BASE}/finance/accounts`;
const WITH_BALANCES = `${ACCOUNTS}/with-balances`;
const DEBTS = `${BASE}/finance/debts`;
const TRANSACTIONS = `${BASE}/finance/transactions`;

const ACCOUNTS_PAGE = [
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
];

const DEBTS_PAGE = {
  items: [
    {
      id: 'debt-1',
      name: 'أحمد خليل',
      phone: null,
      direction: 'Receivable',
      directionLabel: 'ذمة مدينة',
      currency: 'JOD',
      accountId: 'acc-1',
      notes: null,
      createdAt: '2026-08-21T10:00:00',
      outstandingBalance: 300,
      outstandingBalanceDisplay: '300.000',
    },
  ],
  totalCount: 1,
  pageNumber: 1,
  pageSize: 15,
  totalPages: 1,
};

const KPIS = {
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
};

const TRANSACTIONS_PAGE = {
  items: [
    {
      id: 'tx-1',
      date: '2026-08-21T09:30:00',
      description: 'إنشاء ذمة مدينة: أحمد خليل',
      accountName: 'صندوق النقدية',
      amount: 300,
      currency: 'JOD',
      transactionType: 'Outflow',
      referenceType: 'DebtCreation',
    },
  ],
  totalCount: 1,
  pageNumber: 1,
  pageSize: 15,
  totalPages: 1,
};

describe('FinancePage', () => {
  const server = setupServer(
    http.get(WITH_BALANCES, () => HttpResponse.json(ACCOUNTS_PAGE)),
    http.get(`${DEBTS}/kpis`, () => HttpResponse.json(KPIS)),
    http.get(DEBTS, () => HttpResponse.json(DEBTS_PAGE)),
    http.get(TRANSACTIONS, () => HttpResponse.json(TRANSACTIONS_PAGE)),
    http.post(ACCOUNTS, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('acc-new', { status: 201 });
    }),
    http.put(`${ACCOUNTS}/acc-1/balance`, () => HttpResponse.json(null, { status: 200 })),
    http.post(DEBTS, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('debt-new', { status: 201 });
    }),
    http.post(`${DEBTS}/debt-1/payments`, () => HttpResponse.json(null, { status: 200 })),
    http.get(`${BASE}/reference/currencies`, () =>
      HttpResponse.json([
        { value: 0, code: 'JOD', symbol: 'د.أ' },
        { value: 1, code: 'USD', symbol: '$' },
        { value: 2, code: 'ILS', symbol: '₪' },
      ]),
    ),
  );

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function createLoadedFixture(): Promise<ComponentFixture<FinancePage>> {
    const store = TestBed.inject(FinanceStore);
    await store.loadAccounts();
    await store.loadDebtKpis();
    await store.loadDebts({ page: 1, pageSize: 15 });
    await store.loadTransactions({ page: 1, pageSize: 15 });
    const fixture = TestBed.createComponent(FinancePage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<FinancePage>): string {
    return fixture.nativeElement.textContent as string;
  }

  function buttonByText(
    fixture: ComponentFixture<FinancePage>,
    label: string,
  ): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  function setInput(fixture: ComponentFixture<FinancePage>, selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function setSelect(fixture: ComponentFixture<FinancePage>, selector: string, value: string): void {
    const select = fixture.nativeElement.querySelector(selector) as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('change'));
  }

  async function switchTab(fixture: ComponentFixture<FinancePage>, label: string): Promise<void> {
    const tabButton = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (button: HTMLButtonElement) => button.textContent?.includes(label),
    ) as HTMLButtonElement;
    tabButton.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders the transactions tab first, then debts and accounts on demand', async () => {
    const fixture = await createLoadedFixture();

    // Default tab: operations (transactions ledger)
    expect(text(fixture)).toContain('الحركات المالية');
    expect(text(fixture)).toContain('إنشاء ذمة مدينة: أحمد خليل');
    expect(text(fixture)).toContain('−300.000');
    expect(text(fixture)).not.toContain('صندوق النقدية JOD-1234');

    // Debts tab
    await switchTab(fixture, 'الذمم');
    let content = text(fixture);
    expect(content).toContain('ذمم لنا (مدينة)');
    expect(content).toContain('800.000');
    expect(content).toContain('300.000');
    expect(content).toContain('500.000');
    expect(content).toContain('أحمد خليل');
    expect(content).toContain('ذمة مدينة');
    expect(content).not.toContain('إنشاء ذمة مدينة: أحمد خليل');

    // Accounts tab
    await switchTab(fixture, 'الحسابات');
    content = text(fixture);
    expect(content).toContain('صندوق النقدية');
    expect(content).toContain('1200.000 JOD');
    expect(content).toContain('إجمالي JOD:');
    expect(content).not.toContain('أحمد خليل');
  });

  it('creates an account through the dialog with the exact payload', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(ACCOUNTS, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('acc-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    await switchTab(fixture, 'الحسابات');
    await TestBed.inject(FinanceStore).loadAccounts();
    buttonByText(fixture, 'حساب جديد').click();
    fixture.detectChanges();
    await fixture.whenStable();

    setInput(fixture, '#account-name', 'خزينة الفرع');
    setInput(fixture, '#account-opening', '250');
    fixture.detectChanges();

    buttonByText(fixture, 'إنشاء الحساب').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['name']).toBe('خزينة الفرع');
    expect(captured?.['currency']).toBe('JOD');
    expect(captured?.['accountNumber']).toBeNull();
    expect(captured?.['notes']).toBeNull();
    expect(captured?.['openingBalance']).toBe(250);
    expect(Object.keys(captured!)).not.toContain('tenantId');

    await vi.waitFor(() => expect(text(fixture)).not.toContain('إنشاء الحساب'));
  });

  it('sets an account balance through the dialog', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.put(`${ACCOUNTS}/acc-1/balance`, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(null, { status: 200 });
      }),
    );

    const fixture = await createLoadedFixture();
    await switchTab(fixture, 'الحسابات');
    await TestBed.inject(FinanceStore).loadAccounts();
    const editButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.title === 'تعديل الرصيد',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();
    await fixture.whenStable();

    setInput(fixture, '#target-balance', '900');
    setInput(fixture, '#balance-notes', 'تسوية جرد');
    fixture.detectChanges();

    buttonByText(fixture, 'حفظ الرصيد').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['targetBalance']).toBe(900);
    expect(captured?.['notes']).toBe('تسوية جرد');
  });

  it('creates a receivable debt through the dialog with the exact payload', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(DEBTS, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('debt-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    buttonByText(fixture, 'ذمة جديدة').click();
    fixture.detectChanges();
    await fixture.whenStable();

    setInput(fixture, '#debt-name', 'أحمد خليل');
    setInput(fixture, '#debt-amount', '300');
    setSelect(fixture, '#debt-account', 'acc-1');
    fixture.detectChanges();

    buttonByText(fixture, 'إنشاء الذمة').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['name']).toBe('أحمد خليل');
    expect(captured?.['phone']).toBeNull();
    expect(captured?.['direction']).toBe(1);
    expect(captured?.['currency']).toBe('JOD');
    expect(captured?.['accountId']).toBe('acc-1');
    expect(captured?.['amount']).toBe(300);
    expect(captured?.['notes']).toBeNull();
    expect(captured?.['date']).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
    expect(Object.keys(captured!)).not.toContain('tenantId');
  });

  it('pays a debt through the row action with the exact payload', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(`${DEBTS}/debt-1/payments`, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(null, { status: 200 });
      }),
    );

    const fixture = await createLoadedFixture();
    await switchTab(fixture, 'الذمم');
    await TestBed.inject(FinanceStore).loadDebts({ page: 1, pageSize: 15 });
    const payButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.title === 'سداد',
    ) as HTMLButtonElement;
    payButton.click();
    fixture.detectChanges();
    await fixture.whenStable();

    setInput(fixture, '#payment-amount', '150');
    setSelect(fixture, '#payment-account', 'acc-1');
    fixture.detectChanges();

    buttonByText(fixture, 'تسجيل السداد').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['accountId']).toBe('acc-1');
    expect(captured?.['amount']).toBe(150);
    expect(captured?.['notes']).toBeNull();
    expect(captured?.['date']).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
  });

  it('applies the debts direction filter', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(DEBTS, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(DEBTS_PAGE);
      }),
    );

    const fixture = await createLoadedFixture();
    await switchTab(fixture, 'الذمم');
    setSelect(fixture, '#debt-direction-filter', 'Receivable');
    fixture.detectChanges();

    buttonByText(fixture, 'تصفية').click();
    await vi.waitFor(() => expect((captured as any)?.get('direction')).toBe('Receivable'));
  });

  it('applies the transactions currency filter', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(TRANSACTIONS, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(TRANSACTIONS_PAGE);
      }),
    );

    const fixture = await createLoadedFixture();
    setSelect(fixture, '#tx-currency-filter', 'JOD');
    fixture.detectChanges();

    buttonByText(fixture, 'تصفية').click();
    await vi.waitFor(() => expect((captured as any)?.get('currency')).toBe('JOD'));
  });
});
