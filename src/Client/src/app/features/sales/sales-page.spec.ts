import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import type {
  PaginatedSalesInvoices,
  SalesInvoiceKpiResponse,
  SalesInvoiceResponse,
} from './sales-api.service';
import { SalesPage } from './sales-page';
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
    {
      id: 'inv-2',
      invoiceNumber: 'INV-2026-08-0002',
      customerName: 'مريم سليم',
      customerPhone: null,
      date: '2026-08-19T15:30:00',
      currency: 'JOD',
      totalAmount: 1200,
      amountPaid: 500,
      remainingBalance: 700,
      paymentMethod: null,
      status: 'PartiallyPaid',
      statusLabel: 'مدفوعة جزئياً',
      userName: 'ليلى',
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

describe('SalesPage', () => {
  const server = setupServer(
    http.get(`${INVOICES}/kpis`, () => HttpResponse.json(KPIS)),
    http.get(`${INVOICES}/next-number`, () => HttpResponse.json('INV-2026-08-0005')),
    http.get(`${INVOICES}/:id`, () => HttpResponse.json(INVOICE_DETAIL)),
    http.get(INVOICES, () => HttpResponse.json(INVOICES_PAGE)),
    http.post(INVOICES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
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

  async function createLoadedFixture(): Promise<ComponentFixture<SalesPage>> {
    const store = TestBed.inject(SalesStore);
    await store.loadKpis();
    await store.loadInvoices({ page: 1, pageSize: 15 });
    const fixture = TestBed.createComponent(SalesPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<SalesPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  function buttonByText(
    fixture: ComponentFixture<SalesPage>,
    label: string,
  ): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  function setInput(fixture: ComponentFixture<SalesPage>, selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('renders the sales KPIs and the paged invoices table', async () => {
    const fixture = await createLoadedFixture();
    const content = text(fixture);

    expect(content).toContain('مبيعات اليوم');
    expect(content).toContain('4');
    expect(content).toContain('1234.500');
    expect(content).toContain('900.000');
    expect(content).toContain('334.500');
    expect(content).toContain('INV-2026-08-0001');
    expect(content).toContain('INV-2026-08-0002');
    expect(content).toContain('أحمد خالد');
    expect(content).toContain('مريم سليم');
    expect(content).toContain('مكتملة');
    expect(content).toContain('مدفوعة جزئياً');
    expect(content).toContain('850.500 JOD');
    expect(content).toContain('700.000 JOD');
  });

  it('creates an invoice through the dialog and closes it', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(INVOICES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('inv-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    buttonByText(fixture, 'فاتورة جديدة').click();
    fixture.detectChanges();
    await fixture.whenStable();

    setInput(fixture, '#invoice-customer-name', 'مريم سليم');
    setInput(fixture, '[data-idx="0"][data-field="weight"]', '20');
    setInput(fixture, '[data-idx="0"][data-field="price"]', '30');
    setInput(fixture, '#invoice-total', '600');
    const employee = fixture.nativeElement.querySelector('#invoice-employee') as HTMLSelectElement;
    employee.value = 'emp-1';
    employee.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'إصدار الفاتورة').click();
    await vi.waitFor(() => expect(text(fixture)).not.toContain('إصدار الفاتورة'));

    expect(captured?.['customerName']).toBe('مريم سليم');
    expect(captured?.['customerPhone']).toBeNull();
    expect(captured?.['currency']).toBe('JOD');
    expect(captured?.['employeeId']).toBe('emp-1');
    expect(captured?.['totalAmount']).toBe(600);
    expect(captured?.['amountPaid']).toBe(0);
    expect(captured?.['paymentMethod']).toBe(1);
    expect(captured?.['accountId']).toBeNull();
    expect(captured?.['buyerAccountNumber']).toBeNull();
    expect(captured?.['paymentLegs']).toBeNull();
    expect(captured?.['notes']).toBeNull();
    expect(captured?.['items']).toEqual([
      { categoryId: null, karat: 21, weightInGrams: 20, pricePerGram: 30 },
    ]);
  });

  it('applies search + status + date filters', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(INVOICES, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(INVOICES_PAGE);
      }),
    );

    const fixture = await createLoadedFixture();
    const search = [...fixture.nativeElement.querySelectorAll('input')].find(
      (input: HTMLInputElement) => input.placeholder.includes('رقم الفاتورة'),
    ) as HTMLInputElement;
    search.value = 'مريم';
    search.dispatchEvent(new Event('input'));
    const status = [...fixture.nativeElement.querySelectorAll('select')].find(
      (select: HTMLSelectElement) => [...select.options].some((option) => option.value === 'PartiallyPaid'),
    ) as HTMLSelectElement;
    status.value = 'PartiallyPaid';
    status.dispatchEvent(new Event('change'));
    const dateInputs = [...fixture.nativeElement.querySelectorAll('input[type="date"]')] as HTMLInputElement[];
    dateInputs[0]!.value = '2026-08-01';
    dateInputs[0]!.dispatchEvent(new Event('change'));
    dateInputs[1]!.value = '2026-08-31';
    dateInputs[1]!.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'تصفية').click();
    await vi.waitFor(() => expect((captured as any)?.get('search')).toBe('مريم'));

    expect((captured as any)?.get('status')).toBe('PartiallyPaid');
    expect((captured as any)?.get('fromDate')).toBe('2026-08-01');
    expect((captured as any)?.get('toDate')).toBe('2026-08-31');
  });

  it('opens the detail dialog from a row and shows the refreshed invoice', async () => {
    const fixture = await createLoadedFixture();
    const store = TestBed.inject(SalesStore);
    const numberButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('INV-2026-08-0001'),
    ) as HTMLButtonElement;
    numberButton.click();
    await vi.waitFor(() => expect(store.detail()).not.toBeNull());
    fixture.detectChanges();

    const content = text(fixture);
    expect(content).toContain('مكتملة');
    expect(content).toContain('عيار 21');
    expect(content).toContain('25.000');
  });

  it('shows an inline retry state when the invoices fetch fails', async () => {
    const store = TestBed.inject(SalesStore);
    server.use(http.get(INVOICES, () => HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 })));
    await store.loadInvoices({ page: 1, pageSize: 15 });

    const fixture = TestBed.createComponent(SalesPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.error()).not.toBeNull());
    fixture.detectChanges();

    expect(text(fixture)).toContain('تعذّر تحميل الفواتير');
    expect(text(fixture)).toContain('إعادة المحاولة');
  });
});