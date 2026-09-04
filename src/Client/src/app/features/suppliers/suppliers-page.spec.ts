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

const BASE = apiUrl('/api/v1');
const SUPPLIERS_ENDPOINT = `${BASE}/suppliers`;
const DELIVERIES = `${BASE}/supplier-deliveries`;
const TRANSACTIONS = `${BASE}/supplier-financial-transactions`;
const ACCOUNTS_ENDPOINT = `${BASE}/finance/accounts`;

describe('SuppliersPage', () => {
  let suppliers: SupplierResponse[];

  const server = setupServer(
    http.get(SUPPLIERS_ENDPOINT, () => HttpResponse.json(suppliers)),
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
      const body = (await request.json()) as { lines: { karat: number; weightInGrams: number }[] };
      expect(Object.keys(body)).not.toContain('tenantId');
      for (const line of body.lines) {
        expect(Object.keys(line).sort()).toEqual(['karat', 'weightInGrams']);
      }
      return HttpResponse.json('delivery-1', { status: 201 });
    }),
    http.get(`${TRANSACTIONS}/kpis`, () => HttpResponse.json(KPIS)),
    http.get(TRANSACTIONS, () => HttpResponse.json(PAGED)),
    http.get(ACCOUNTS_ENDPOINT, () => HttpResponse.json(ACCOUNTS)),
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
  });
  afterAll(() => server.close());

  beforeEach(() => {
    suppliers = [SUPPLIERS[0] as SupplierResponse, SUPPLIERS[1] as SupplierResponse];
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function createLoadedFixture(): Promise<ComponentFixture<SuppliersPage>> {
    const store = TestBed.inject(SuppliersStore);
    await store.ensureLoaded();
    await store.loadKpis();
    await store.loadTransactions({ page: 1, pageSize: 15 });
    const fixture = TestBed.createComponent(SuppliersPage);
    fixture.detectChanges();
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
    return [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes(label),
    ) as HTMLButtonElement;
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
    expect(content).toContain('معاملة مالية');
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

  it('records a delivery whose lines carry only karat and weightInGrams', async () => {
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

  it('shows an inline retry state when the supplier list fetch fails', async () => {
    const store = TestBed.inject(SuppliersStore);
    server.use(
      http.get(SUPPLIERS_ENDPOINT, () =>
        HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 }),
      ),
    );
    await store.load();

    const fixture = TestBed.createComponent(SuppliersPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.loading()).toBe(false));
    fixture.detectChanges();

    expect(text(fixture)).toContain('تعذّر تحميل الموردين');
    expect(text(fixture)).toContain('إعادة المحاولة');
  });
});