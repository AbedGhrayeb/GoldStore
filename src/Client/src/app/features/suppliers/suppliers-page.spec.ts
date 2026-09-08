import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { DeliveryDialog } from './delivery-dialog';
import type { SupplierResponse } from './suppliers-api.service';
import { SuppliersPage } from './suppliers-page';
import { SuppliersStore } from './suppliers-store';

const SUPPLIERS: SupplierResponse[] = [
  {
    id: 'supplier-1',
    name: 'مؤسسة الذهب',
    primaryPhone: '0791111111',
    secondaryPhone: null,
    bankAccountNumber: null,
    notes: null,
    isActive: true,
    goldBalance: 125.5,
    manufacturingBalance: 40.25,
  },
  {
    id: 'supplier-2',
    name: 'ورشة المجوهرات',
    primaryPhone: '0792222222',
    secondaryPhone: null,
    bankAccountNumber: null,
    notes: null,
    isActive: false,
    goldBalance: 0,
    manufacturingBalance: 0,
  },
];

const PAGED_SUPPLIERS = {
  items: [
    {
      id: 'supplier-1',
      name: 'مؤسسة الذهب',
      primaryPhone: '0791111111',
      secondaryPhone: null,
      bankAccountNumber: null,
      notes: null,
      isActive: true,
      goldBalance: 125.5,
      manufacturingBalance: 40.25,
    },
    {
      id: 'supplier-2',
      name: 'ورشة المجوهرات',
      primaryPhone: '0792222222',
      secondaryPhone: null,
      bankAccountNumber: null,
      notes: null,
      isActive: false,
      goldBalance: 0,
      manufacturingBalance: 0,
    },
  ],
  pageNumber: 1,
  pageSize: 15,
  totalCount: 2,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

const PAGED = {
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

const KPIS = {
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

const ACCOUNTS = [{ id: 'account-1', name: 'صندوق النقد', currency: 'JOD', isActive: true }];

const DETAIL = {
  id: 'supplier-1',
  name: 'مؤسسة الذهب',
  primaryPhone: '0791111111',
  secondaryPhone: null,
  bankAccountNumber: null,
  notes: null,
  isActive: true,
  createdAt: '2026-08-01T10:00:00',
  goldBalance: 125.5,
  manufacturingBalance: 40.25,
  financialBalancesByCurrency: [{ currency: 'JOD', balance: 400 }],
  recentTransactions: [],
};

const DETAIL_TX_GOLD = {
  id: 'tx-gold',
  description: 'توريد ذهب',
  date: '2026-09-01T10:00:00',
  type: 'ذهب',
  amount: 10,
  unit: 'جم',
  direction: '+',
};

const DETAIL_TX_MFG = {
  id: 'tx-mfg',
  description: 'أجور تصنيع',
  date: '2026-09-02T10:00:00',
  type: 'تصنيع',
  amount: 20,
  unit: 'JOD',
  direction: '+',
};

const BASE = apiUrl('/api/v1');
const SUPPLIERS_ENDPOINT = `${BASE}/suppliers`;
const DELIVERIES = `${BASE}/supplier-deliveries`;
const TRANSACTIONS = `${BASE}/supplier-financial-transactions`;
const ACCOUNTS_ENDPOINT = `${BASE}/finance/accounts`;

describe('SuppliersPage', () => {
  let suppliers: SupplierResponse[];
  let lastDetailTxQuery: { id: string; type: string | null; page: string | null };

  const server = setupServer(
    http.get(SUPPLIERS_ENDPOINT, () => HttpResponse.json(suppliers)),
    http.get(`${SUPPLIERS_ENDPOINT}/paged`, () => HttpResponse.json(PAGED_SUPPLIERS)),
    http.get(`${SUPPLIERS_ENDPOINT}/:id/transactions`, ({ params, request }) => {
      const url = new URL(request.url);
      lastDetailTxQuery = {
        id: params['id'] as string,
        type: url.searchParams.get('type'),
        page: url.searchParams.get('page'),
      };
      const type = url.searchParams.get('type') ?? '';
      const page = url.searchParams.get('page') ?? '1';
      if (type === 'gold') {
        return HttpResponse.json({
          items: [DETAIL_TX_GOLD],
          pageNumber: 1,
          pageSize: 15,
          totalCount: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
        });
      }
      if (type === 'manufacturing') {
        return HttpResponse.json({
          items: [DETAIL_TX_MFG],
          pageNumber: 1,
          pageSize: 15,
          totalCount: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
        });
      }
      return HttpResponse.json({
        items: page === '2' ? [DETAIL_TX_MFG] : [DETAIL_TX_GOLD],
        pageNumber: Number(page),
        pageSize: 15,
        totalCount: 2,
        totalPages: 2,
        hasPreviousPage: page === '2',
        hasNextPage: page !== '2',
      });
    }),
    http.get(`${SUPPLIERS_ENDPOINT}/:id`, () => HttpResponse.json(DETAIL)),
    http.post(SUPPLIERS_ENDPOINT, async ({ request }) => {
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
    http.post(DELIVERIES, async ({ request }) => {
      const body = (await request.json()) as {
        lines: {
          karat: number;
          weightInGrams: number;
          categoryId: string | null;
        }[];
      };
      expect(Object.keys(body)).not.toContain('tenantId');
      expect(Object.keys(body).sort()).toEqual([
        'amountDue',
        'amountDueCurrency',
        'lines',
        'manufacturingFeeCurrency',
        'manufacturingFeePerGram',
        'notes',
        'paymentLegs',
        'supplierId',
      ]);
      for (const line of body.lines) {
        expect(Object.keys(line).sort()).toEqual(['categoryId', 'karat', 'weightInGrams']);
      }
      return HttpResponse.json('delivery-1', { status: 201 });
    }),
    http.get(`${TRANSACTIONS}/kpis`, () => HttpResponse.json(KPIS)),
    http.get(TRANSACTIONS, () => HttpResponse.json(PAGED)),
    http.get(ACCOUNTS_ENDPOINT, () => HttpResponse.json(ACCOUNTS)),
    http.get(`${BASE}/categories`, () =>
      HttpResponse.json([{ id: 'cat-1', name: 'خواتم', isActive: true }]),
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
  afterEach(() => {
    server.resetHandlers();
    suppliers = [SUPPLIERS[0] as SupplierResponse, SUPPLIERS[1] as SupplierResponse];
    lastDetailTxQuery = { id: '', type: null, page: null };
  });
  afterAll(() => server.close());

  beforeEach(() => {
    suppliers = [SUPPLIERS[0] as SupplierResponse, SUPPLIERS[1] as SupplierResponse];
    lastDetailTxQuery = { id: '', type: null, page: null };
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function createLoadedFixture(): Promise<ComponentFixture<SuppliersPage>> {
    const store = TestBed.inject(SuppliersStore);
    await store.ensureLoaded();
    await store.loadKpis();
    await store.loadSuppliersPage({ page: 1, pageSize: 15 });
    await store.loadTransactions({ page: 1, pageSize: 15 });
    const fixture = TestBed.createComponent(SuppliersPage);
    fixture.detectChanges();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<SuppliersPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  function buttonByText(
    fixture: ComponentFixture<SuppliersPage>,
    label: string,
  ): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')].find((button: HTMLButtonElement) =>
      button.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  async function openDetailTab(): Promise<ComponentFixture<SuppliersPage>> {
    const fixture = await createLoadedFixture();
    const eye = fixture.nativeElement.querySelector(
      'button[aria-label="عرض تفاصيل مؤسسة الذهب"]',
    ) as HTMLButtonElement;
    eye.click();
    fixture.detectChanges();
    await vi.waitFor(() => {
      expect(text(fixture)).toContain('توريد ذهب');
    });
    return fixture;
  }

  it('renders suppliers, KPI cards and the financial transactions table', async () => {
    const fixture = await createLoadedFixture();
    const content = text(fixture);

    expect(content).toContain('مؤسسة الذهب');
    expect(content).toContain('ورشة المجوهرات');
    expect(content).toContain('نشط');
    expect(content).toContain('متوقف');
    expect(content).toContain('سلف الموردين');
    expect(content).toContain('400.000');

    const operationsTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('العمليات المالية'),
    ) as HTMLButtonElement;
    operationsTab.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(text(fixture)).toContain('معاملة مالية');
  });

  it('creates a supplier through the dialog and refreshes the table', async () => {
    const fixture = await createLoadedFixture();
    buttonByText(fixture, 'إضافة مورد').click();
    fixture.detectChanges();

    const name = fixture.nativeElement.querySelector('#supplier-name') as HTMLInputElement;
    name.value = 'مورد جديد';
    name.dispatchEvent(new Event('input'));
    const phone = fixture.nativeElement.querySelector('#supplier-phone') as HTMLInputElement;
    phone.value = '0793333333';
    phone.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(fixture, 'حفظ').click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).toContain('مورد جديد'));
  });

  it('records a delivery with header-level due amount for the total weight', async () => {
    const fixture = await createLoadedFixture();
    buttonByText(fixture, 'تسليم ذهب').click();
    fixture.detectChanges();
    await fixture.whenStable();

    const dialog = fixture.debugElement.query(By.directive(DeliveryDialog))
      .componentInstance as DeliveryDialog;
    dialog.draft.set({ supplierId: 'supplier-1', notes: '' });

    const karat = fixture.nativeElement.querySelector('#line-karat-0') as HTMLSelectElement;
    karat.value = '21';
    karat.dispatchEvent(new Event('change'));
    const weight = fixture.nativeElement.querySelector('#line-weight-0') as HTMLInputElement;
    weight.value = '50.5';
    weight.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(fixture, 'حفظ التسليم').click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).not.toContain('حفظ التسليم'));
  });

  it('opens the supplier detail as a full-page tab instead of a modal', async () => {
    const fixture = await openDetailTab();

    expect(text(fixture)).toContain('تفاصيل المورد');
    expect(text(fixture)).toContain('رصيد الذهب (21ك)');
    expect(lastDetailTxQuery.id).toBe('supplier-1');
    expect(fixture.nativeElement.querySelector('app-supplier-detail-dialog')).toBeNull();
    const detailTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('تفاصيل المورد'),
    ) as HTMLButtonElement;
    expect(detailTab.getAttribute('aria-selected')).toBe('true');
  });

  it('filters detail transactions by type', async () => {
    const fixture = await openDetailTab();

    const typeSelect = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    typeSelect.value = 'gold';
    typeSelect.dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();
    buttonByText(fixture, 'تصفية').click();
    fixture.detectChanges();

    await vi.waitFor(() => {
      expect(lastDetailTxQuery.type).toBe('gold');
    });
    await vi.waitFor(() => {
      expect(text(fixture)).toContain('توريد ذهب');
      expect(text(fixture)).not.toContain('أجور تصنيع');
    });
  });

  it('pages detail transactions', async () => {
    const fixture = await openDetailTab();
    expect(text(fixture)).toContain('توريد ذهب');

    buttonByText(fixture, 'التالي').click();
    fixture.detectChanges();

    await vi.waitFor(() => {
      expect(lastDetailTxQuery.page).toBe('2');
    });
    await vi.waitFor(() => {
      expect(text(fixture)).toContain('أجور تصنيع');
    });
  });

  it('shows an inline retry state when the supplier list fetch fails', async () => {
    const store = TestBed.inject(SuppliersStore);
    server.use(
      http.get(`${SUPPLIERS_ENDPOINT}/paged`, () =>
        HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 }),
      ),
    );
    await store.loadSuppliersPage({ page: 1, pageSize: 15 });

    const fixture = TestBed.createComponent(SuppliersPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.suppliersPageLoading()).toBe(false));
    fixture.detectChanges();

    expect(text(fixture)).toContain('تعذّر تحميل الموردين');
    expect(text(fixture)).toContain('إعادة المحاولة');
  });
});
