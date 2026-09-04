import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import type {
  CustomerPurchaseInvoiceKpiResponse,
  CustomerPurchaseInvoiceResponse,
  PaginatedCustomerPurchaseInvoices,
} from './purchases-api.service';
import { PurchasesPage } from './purchases-page';
import { PurchasesStore } from './purchases-store';

const PURCHASES_PAGE: PaginatedCustomerPurchaseInvoices = {
  items: [
    {
      id: 'pur-1',
      invoiceNumber: 'PUR-2026-08-0001',
      sellerName: 'مريم سليم',
      sellerPhone: '0791000002',
      sellerIdNumber: '123456789',
      sellerYearOfBirth: 1990,
      sellerAddress: 'عمان',
      employeeName: 'عمر حسن',
      date: '2026-08-20T10:00:00',
      currency: 'JOD',
      totalAmount: 850.5,
      amountPaid: 850.5,
      remainingBalance: 0,
      paymentMethod: 'Cash',
      sellerAccountNumber: null,
      notes: null,
      createdAt: '2026-08-20T10:00:00',
      items: [],
    },
    {
      id: 'pur-2',
      invoiceNumber: 'PUR-2026-08-0002',
      sellerName: 'سامي ناصر',
      sellerPhone: null,
      sellerIdNumber: '987654321',
      sellerYearOfBirth: null,
      sellerAddress: null,
      employeeName: 'ليلى',
      date: '2026-08-19T15:30:00',
      currency: 'JOD',
      totalAmount: 1200,
      amountPaid: 500,
      remainingBalance: 700,
      paymentMethod: 'Bank',
      sellerAccountNumber: 'JO9876',
      notes: 'دفعة أولى',
      createdAt: '2026-08-19T15:30:00',
      items: [],
    },
  ],
  totalCount: 2,
  pageNumber: 1,
  pageSize: 15,
  totalPages: 1,
};

const PURCHASE_DETAIL: CustomerPurchaseInvoiceResponse = {
  ...PURCHASES_PAGE.items![0]!,
  items: [
    {
      id: 'line-1',
      categoryId: null,
      karat: 21,
      weightInGrams: 25,
      equivalent21KWeightInGrams: 25,
      pricePerGram: 34.02,
      goldAmount: 850.5,
    },
  ],
};

const KPIS: CustomerPurchaseInvoiceKpiResponse = {
  todayCount: 4,
  totalPurchases: 1234.5,
  totalPurchasesDisplay: '1234.500',
  totalPaid: 900,
  totalPaidDisplay: '900.000',
  totalRemaining: 334.5,
  totalRemainingDisplay: '334.500',
};

const BASE = apiUrl('/api/v1');
const INVOICES = `${BASE}/customer-purchases/invoices`;
const EMPLOYEES = `${BASE}/employees`;
const CATEGORIES = `${BASE}/categories`;
const ACCOUNTS = `${BASE}/finance/accounts`;

describe('PurchasesPage', () => {
  const server = setupServer(
    http.get(`${INVOICES}/kpis`, () => HttpResponse.json(KPIS)),
    http.get(`${INVOICES}/next-number`, () => HttpResponse.json('PUR-2026-08-0005')),
    http.get(`${INVOICES}/:id`, () => HttpResponse.json(PURCHASE_DETAIL)),
    http.get(INVOICES, () => HttpResponse.json(PURCHASES_PAGE)),
    http.post(INVOICES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
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
    http.get(`${BASE}/reference/karats`, () =>
      HttpResponse.json([
        { value: 18, label: 'عيار 18' },
        { value: 21, label: 'عيار 21' },
        { value: 24, label: 'عيار 24' },
      ]),
    ),
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

  async function createLoadedFixture(): Promise<ComponentFixture<PurchasesPage>> {
    const store = TestBed.inject(PurchasesStore);
    await store.loadKpis();
    await store.loadInvoices({ page: 1, pageSize: 15 });
    const fixture = TestBed.createComponent(PurchasesPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<PurchasesPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  function buttonByText(
    fixture: ComponentFixture<PurchasesPage>,
    label: string,
  ): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  function setInput(fixture: ComponentFixture<PurchasesPage>, selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function setSelect(fixture: ComponentFixture<PurchasesPage>, selector: string, value: string): void {
    const select = fixture.nativeElement.querySelector(selector) as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('change'));
  }

  async function openDialog(fixture: ComponentFixture<PurchasesPage>): Promise<void> {
    await TestBed.inject(PurchasesStore).ensureAccounts();
    buttonByText(fixture, 'فاتورة شراء جديدة').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders the purchase KPIs and the paged invoices table', async () => {
    const fixture = await createLoadedFixture();
    const content = text(fixture);

    expect(content).toContain('مشتريات اليوم');
    expect(content).toContain('4');
    expect(content).toContain('1234.500');
    expect(content).toContain('900.000');
    expect(content).toContain('334.500');
    expect(content).toContain('PUR-2026-08-0001');
    expect(content).toContain('PUR-2026-08-0002');
    expect(content).toContain('مريم سليم');
    expect(content).toContain('سامي ناصر');
    expect(content).toContain('850.500 JOD');
    expect(content).toContain('700.000 JOD');
  });

  it('auto-selects the matching cash account when the dialog opens', async () => {
    const fixture = await createLoadedFixture();
    await openDialog(fixture);

    const account = fixture.nativeElement.querySelector('#purchase-account') as HTMLSelectElement;
    await vi.waitFor(() => expect(account.value).toBe('acc-1'));
  });

  it('creates a cash invoice through the dialog with the exact payload and closes it', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(INVOICES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('pur-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    await openDialog(fixture);

    setInput(fixture, '#seller-name', 'مريم سليم');
    setInput(fixture, '#seller-id-number', '123456789');
    setInput(fixture, '[data-idx="0"][data-field="weight"]', '20');
    setInput(fixture, '[data-idx="0"][data-field="price"]', '30');
    setInput(fixture, '#purchase-total', '600');
    setSelect(fixture, '#purchase-employee', 'emp-1');
    fixture.detectChanges();

    buttonByText(fixture, 'إصدار فاتورة الشراء').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['sellerName']).toBe('مريم سليم');
    expect(captured?.['sellerPhone']).toBeNull();
    expect(captured?.['sellerIdNumber']).toBe('123456789');
    expect(captured?.['sellerYearOfBirth']).toBeNull();
    expect(captured?.['sellerAddress']).toBeNull();
    expect(captured?.['employeeId']).toBe('emp-1');
    expect(captured?.['currency']).toBe('JOD');
    expect(captured?.['date']).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
    expect(captured?.['totalAmount']).toBe(600);
    expect(captured?.['amountPaid']).toBe(0);
    expect(captured?.['paymentMethod']).toBe(1);
    expect(captured?.['accountId']).toBe('acc-1');
    expect(captured?.['sellerAccountNumber']).toBeNull();
    expect(captured?.['notes']).toBeNull();
    expect(captured?.['paymentLegs']).toBeNull();
    expect(captured?.['items']).toEqual([
      { categoryId: null, karat: 21, weightInGrams: 20, pricePerGram: 30 },
    ]);
    expect(Object.keys(captured!)).not.toContain('tenantId');

    await vi.waitFor(() => expect(text(fixture)).not.toContain('إصدار فاتورة الشراء'));
  });

  it('creates a bank-transfer invoice with the seller account number', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(INVOICES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('pur-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    await openDialog(fixture);

    setInput(fixture, '#seller-name', 'سامي');
    setInput(fixture, '#seller-id-number', '987654321');
    setInput(fixture, '[data-idx="0"][data-field="weight"]', '10');
    setInput(fixture, '[data-idx="0"][data-field="price"]', '25');
    setInput(fixture, '#purchase-total', '250');
    setSelect(fixture, '#purchase-employee', 'emp-1');
    const bankRadio = [...fixture.nativeElement.querySelectorAll('input')].find(
      (input: HTMLInputElement) =>
        input.name === 'purchase-payment-method' && input.value === '2',
    ) as HTMLInputElement;
    bankRadio.click();
    fixture.detectChanges();
    setInput(fixture, '#seller-account-number', 'JO9876');
    fixture.detectChanges();

    buttonByText(fixture, 'إصدار فاتورة الشراء').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['paymentMethod']).toBe(2);
    expect(captured?.['accountId']).toBe('acc-2');
    expect(captured?.['sellerAccountNumber']).toBe('JO9876');
    expect(captured?.['amountPaid']).toBe(0);
  });

  it('creates an invoice with multi-currency payment legs', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(INVOICES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('pur-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    await openDialog(fixture);

    setInput(fixture, '#seller-name', 'ليلى');
    setInput(fixture, '#seller-id-number', '555666777');
    setInput(fixture, '[data-idx="0"][data-field="weight"]', '15');
    setInput(fixture, '[data-idx="0"][data-field="price"]', '20');
    setInput(fixture, '#purchase-total', '300');
    setSelect(fixture, '#purchase-employee', 'emp-1');
    const legsToggle = fixture.nativeElement.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    legsToggle.click();
    fixture.detectChanges();
    setSelect(fixture, '[data-idx="0"][data-field="accountId"]', 'acc-2');
    setInput(fixture, '[data-idx="0"][data-field="amount"]', '300');
    fixture.detectChanges();

    buttonByText(fixture, 'إصدار فاتورة الشراء').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['paymentMethod']).toBe(2);
    expect(captured?.['accountId']).toBe('acc-2');
    expect(captured?.['amountPaid']).toBe(300);
    expect(captured?.['sellerAccountNumber']).toBeNull();
    expect(captured?.['paymentLegs']).toEqual([
      { accountId: 'acc-2', currency: 'JOD', amount: 300, exchangeRate: 1 },
    ]);
  });

  it('shows inline validation errors instead of submitting', async () => {
    let posted = false;
    server.use(
      http.post(INVOICES, async () => {
        posted = true;
        return HttpResponse.json('pur-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    await openDialog(fixture);

    buttonByText(fixture, 'إصدار فاتورة الشراء').click();
    fixture.detectChanges();

    expect(text(fixture)).toContain('اسم البائع مطلوب.');
    expect(text(fixture)).toContain('رقم الهوية مطلوب.');
    expect(posted).toBe(false);
  });

  it('shows an inline error when the missing employee blocks the submit', async () => {
    const fixture = await createLoadedFixture();
    await openDialog(fixture);

    setInput(fixture, '#seller-name', 'مريم');
    setInput(fixture, '#seller-id-number', '123456789');
    setInput(fixture, '[data-idx="0"][data-field="weight"]', '10');
    setInput(fixture, '[data-idx="0"][data-field="price"]', '20');
    setInput(fixture, '#purchase-total', '200');
    fixture.detectChanges();

    buttonByText(fixture, 'إصدار فاتورة الشراء').click();
    fixture.detectChanges();

    expect(text(fixture)).toContain('اختر الموظف المشتري للفاتورة.');
  });

  it('blocks the submit when no account matches the currency and payment method', async () => {
    server.use(
      http.get(ACCOUNTS, () =>
        HttpResponse.json([
          { id: 'acc-1', name: 'صندوق النقدية', currency: 'USD', accountType: 'Cash', isActive: true },
        ]),
      ),
    );

    let posted = false;
    server.use(
      http.post(INVOICES, async () => {
        posted = true;
        return HttpResponse.json('pur-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    await openDialog(fixture);

    setInput(fixture, '#seller-name', 'مريم');
    setInput(fixture, '#seller-id-number', '123456789');
    setInput(fixture, '[data-idx="0"][data-field="weight"]', '10');
    setInput(fixture, '[data-idx="0"][data-field="price"]', '20');
    setInput(fixture, '#purchase-total', '200');
    setSelect(fixture, '#purchase-employee', 'emp-1');
    fixture.detectChanges();

    buttonByText(fixture, 'إصدار فاتورة الشراء').click();
    fixture.detectChanges();

    expect(text(fixture)).toContain('لا يوجد حساب مطابق');
    expect(posted).toBe(false);
  });

  it('applies search + date + page-size filters', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(INVOICES, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(PURCHASES_PAGE);
      }),
    );

    const fixture = await createLoadedFixture();
    const search = [...fixture.nativeElement.querySelectorAll('input')].find(
      (input: HTMLInputElement) => input.placeholder.includes('رقم الفاتورة'),
    ) as HTMLInputElement;
    search.value = 'مريم';
    search.dispatchEvent(new Event('input'));
    const dateInputs = [...fixture.nativeElement.querySelectorAll('input[type="date"]')] as HTMLInputElement[];
    dateInputs[0]!.value = '2026-08-01';
    dateInputs[0]!.dispatchEvent(new Event('change'));
    dateInputs[1]!.value = '2026-08-31';
    dateInputs[1]!.dispatchEvent(new Event('change'));
    const pageSize = [...fixture.nativeElement.querySelectorAll('select')].find(
      (select: HTMLSelectElement) => [...select.options].some((option) => option.value === '25'),
    ) as HTMLSelectElement;
    pageSize.value = '25';
    pageSize.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'تصفية').click();
    await vi.waitFor(() => expect((captured as any)?.get('search')).toBe('مريم'));

    expect((captured as any)?.get('fromDate')).toBe('2026-08-01');
    expect((captured as any)?.get('toDate')).toBe('2026-08-31');
    expect((captured as any)?.get('pageSize')).toBe('25');
  });

  it('opens the detail dialog from a row and shows the refreshed invoice', async () => {
    const fixture = await createLoadedFixture();
    const store = TestBed.inject(PurchasesStore);
    const numberButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('PUR-2026-08-0001'),
    ) as HTMLButtonElement;
    numberButton.click();
    await vi.waitFor(() => expect(store.detail()).not.toBeNull());
    fixture.detectChanges();

    const content = text(fixture);
    expect(content).toContain('عيار 21');
    expect(content).toContain('25.000');
    expect(content).toContain('عمر حسن');
  });

  it('shows an inline retry state when the invoices fetch fails', async () => {
    const store = TestBed.inject(PurchasesStore);
    server.use(http.get(INVOICES, () => HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 })));
    await store.loadInvoices({ page: 1, pageSize: 15 });

    const fixture = TestBed.createComponent(PurchasesPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.error()).not.toBeNull());
    fixture.detectChanges();

    expect(text(fixture)).toContain('تعذّر تحميل الفواتير');
    expect(text(fixture)).toContain('إعادة المحاولة');
  });
});