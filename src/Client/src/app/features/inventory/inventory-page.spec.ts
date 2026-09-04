import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import type {
  GoldTrendPoint,
  InventoryAdjustmentKpiResponse,
  InventoryKpiResponse,
  PaginatedGoldLedger,
  PaginatedInventoryAdjustments,
} from './inventory-api.service';
import { InventoryPage } from './inventory-page';
import { InventoryStore } from './inventory-store';

const ADJUSTMENTS_PAGE: PaginatedInventoryAdjustments = {
  items: [
    {
      id: 'adj-1',
      date: '2026-08-19T10:00:00',
      type: 'Increase',
      typeLabel: 'زيادة',
      typeColor: 'text-on-primary-container',
      typeBg: 'bg-primary-container/20',
      typeIcon: 'arrow_upward',
      karat: 'عيار 21',
      weightInGrams: 50.5,
      signedWeight: 50.5,
      equivalent21KWeightInGrams: 50.5,
      reason: 'زيادة رصيد',
      notes: null,
      userName: 'أحمد',
    },
    {
      id: 'adj-2',
      date: '2026-08-18T09:00:00',
      type: 'Loss',
      typeLabel: 'خسارة',
      typeColor: 'text-error',
      typeBg: 'bg-error-container/30',
      typeIcon: 'warning',
      karat: 'عيار 18',
      weightInGrams: 10,
      signedWeight: -10,
      equivalent21KWeightInGrams: 8,
      reason: 'فقدان قطعة',
      notes: 'تحت التحقيق',
      userName: 'ليلى',
    },
  ],
  totalCount: 2,
  pageNumber: 1,
  pageSize: 15,
  totalPages: 1,
};

const ADJUSTMENT_KPIS: InventoryAdjustmentKpiResponse = {
  todayCount: 3,
  netWeightChange: 40.5,
  netWeightDisplay: '+40.500',
  isNegative: false,
};

const LEDGER_PAGE: PaginatedGoldLedger = {
  items: [
    {
      id: 'entry-1',
      date: '2026-08-19T10:00:00',
      karat: '21',
      karatLabel: 'عيار 21',
      weightInGrams: 25,
      equivalent21KWeightInGrams: 25,
      movementType: 'Increase',
      movementLabel: 'زيادة',
      movementColor: 'text-tertiary-container',
      movementIcon: 'arrow_downward',
      referenceType: 'Sale',
      referenceLabel: 'بيع',
      referenceId: 'inv-1',
      notes: null,
      userId: 'u1',
      userName: 'أحمد',
    },
    {
      id: 'entry-2',
      date: '2026-08-18T14:30:00',
      karat: '18',
      karatLabel: 'عيار 18',
      weightInGrams: 15,
      equivalent21KWeightInGrams: 12,
      movementType: 'Decrease',
      movementLabel: 'نقصان',
      movementColor: 'text-error',
      movementIcon: 'arrow_upward',
      referenceType: 'InventoryAdjustment',
      referenceLabel: 'تسوية جردية',
      referenceId: 'adj-2',
      notes: null,
      userId: 'u1',
      userName: 'أحمد',
    },
  ],
  totalCount: 2,
  pageNumber: 1,
  pageSize: 15,
  totalPages: 1,
};

const INVENTORY_KPIS: InventoryKpiResponse = {
  totalEquivalent21KGrams: 175,
  totalEquivalent21KDisplay: '175.000',
  totalEquivalent21KUnit: 'جم',
  estimatedValueJod: 0,
  estimatedValueDisplay: '—',
  karatBreakdowns: [
    {
      karat: 24,
      karatLabel: 'عيار 24',
      description: 'عيار 24',
      totalWeightGrams: 50,
      totalWeightDisplay: '50.000',
      unit: 'جم',
      isPrimary: false,
    },
    {
      karat: 21,
      karatLabel: 'عيار 21',
      description: 'عيار 21',
      totalWeightGrams: 125,
      totalWeightDisplay: '125.000',
      unit: 'جم',
      isPrimary: true,
    },
  ],
};

const TREND: GoldTrendPoint[] = [
  { date: '2026-08-19T00:00:00', label: 'الثلاثاء', in21K: 100, out21K: 40, net21K: 60 },
  { date: '2026-08-20T00:00:00', label: 'الأربعاء', in21K: 50, out21K: 10, net21K: 40 },
];

const BASE = apiUrl('/api/v1');
const ADJUSTMENTS = `${BASE}/inventory/adjustments`;
const LEDGER = `${BASE}/inventory/gold-ledger`;

describe('InventoryPage', () => {
  const server = setupServer(
    http.get(`${ADJUSTMENTS}/kpis`, () => HttpResponse.json(ADJUSTMENT_KPIS)),
    http.get(ADJUSTMENTS, () => HttpResponse.json(ADJUSTMENTS_PAGE)),
    http.post(ADJUSTMENTS, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'adjustmentType',
        'date',
        'karat',
        'notes',
        'reason',
        'weightInGrams',
      ]);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('adj-new', { status: 201 });
    }),
    http.get(`${LEDGER}/trend`, () => HttpResponse.json(TREND)),
    http.get(`${LEDGER}/kpis`, () => HttpResponse.json(INVENTORY_KPIS)),
    http.get(LEDGER, () => HttpResponse.json(LEDGER_PAGE)),
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

  async function createLoadedFixture(): Promise<ComponentFixture<InventoryPage>> {
    const store = TestBed.inject(InventoryStore);
    await store.loadInventoryKpis();
    await store.loadAdjustmentKpis();
    await store.loadAdjustments({ page: 1, pageSize: 15 });
    await store.loadLedger({ page: 1, pageSize: 15 });
    await store.loadTrend(7);
    const fixture = TestBed.createComponent(InventoryPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<InventoryPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  function buttonByText(
    fixture: ComponentFixture<InventoryPage>,
    label: string,
  ): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  it('renders inventory KPIs, adjustment KPIs, the trend chart and both tables', async () => {
    const fixture = await createLoadedFixture();
    const content = text(fixture);

    expect(content).toContain('إجمالي المخزون');
    expect(content).toContain('175.000');
    expect(content).toContain('50.000');
    expect(content).toContain('الرئيسي');
    expect(content).toContain('تسويات اليوم');
    expect(content).toContain('+40.500');
    expect(content).toContain('دخول');
    expect(content).toContain('خروج');
    expect(content).toContain('زيادة رصيد');
    expect(content).toContain('فقدان قطعة');
    expect(content).toContain('بيع');
    expect(content).toContain('تسوية جردية');
  });

  it('creates an adjustment through the dialog and closes it', async () => {
    const fixture = await createLoadedFixture();
    buttonByText(fixture, 'تسوية جردية').click();
    fixture.detectChanges();
    await fixture.whenStable();

    const weight = fixture.nativeElement.querySelector('#adjustment-weight') as HTMLInputElement;
    weight.value = '20';
    weight.dispatchEvent(new Event('input'));
    const reason = fixture.nativeElement.querySelector('#adjustment-reason') as HTMLInputElement;
    reason.value = 'إعادة وزن';
    reason.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(fixture, 'حفظ التسوية').click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).not.toContain('حفظ التسوية'));
  });

  it('applies gold-ledger filters (karat + referenceType)', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(LEDGER, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(LEDGER_PAGE);
      }),
    );

    const fixture = await createLoadedFixture();
    const karat = [...fixture.nativeElement.querySelectorAll('select')].find(
      (select: HTMLSelectElement) => [...select.options].some((option) => option.value === '24'),
    ) as HTMLSelectElement;
    karat.value = '21';
    karat.dispatchEvent(new Event('change'));
    const referenceType = [...fixture.nativeElement.querySelectorAll('select')].find(
      (select: HTMLSelectElement) =>
        [...select.options].some((option) => option.value === 'Sale'),
    ) as HTMLSelectElement;
    referenceType.value = 'Sale';
    referenceType.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'تصفية').click();
    await vi.waitFor(() => expect((captured as any)?.get('karat')).toBe('21'));

    expect((captured as any)?.get('referenceType')).toBe('Sale');
  });

  it('shows an inline retry state when the adjustments fetch fails', async () => {
    const store = TestBed.inject(InventoryStore);
    server.use(
      http.get(ADJUSTMENTS, () =>
        HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 }),
      ),
    );
    await store.loadAdjustments({ page: 1, pageSize: 15 });

    const fixture = TestBed.createComponent(InventoryPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.adjustmentsError()).not.toBeNull());
    fixture.detectChanges();

    expect(text(fixture)).toContain('تعذّر تحميل التسويات');
    expect(text(fixture)).toContain('إعادة المحاولة');
  });
});