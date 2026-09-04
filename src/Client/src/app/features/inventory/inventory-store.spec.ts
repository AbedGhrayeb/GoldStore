import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type {
  GoldTrendPoint,
  InventoryAdjustmentKpiResponse,
  InventoryKpiResponse,
  PaginatedGoldLedger,
  PaginatedInventoryAdjustments,
} from './inventory-api.service';
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
  ],
  totalCount: 1,
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
  ],
  totalCount: 1,
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

describe('InventoryStore', () => {
  let store: InventoryStore;
  let toasts: ToastStore;

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
  );

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    store = TestBed.inject(InventoryStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads inventory KPIs, adjustment KPIs, trend and both paged tables', async () => {
    await store.loadInventoryKpis();
    await store.loadAdjustmentKpis();
    await store.loadTrend(7);
    await store.loadAdjustments({ page: 1, pageSize: 15 });
    await store.loadLedger({ page: 1, pageSize: 15 });

    expect(store.inventoryKpis()?.totalEquivalent21KDisplay).toBe('175.000');
    expect(store.inventoryKpis()?.karatBreakdowns).toHaveLength(2);
    expect(store.adjustmentKpis()?.todayCount).toBe(3);
    expect(store.trend()).toHaveLength(2);
    expect(store.adjustments()?.items?.[0]?.reason).toBe('زيادة رصيد');
    expect(store.ledger()?.items?.[0]?.referenceLabel).toBe('بيع');
  });

  it('creates an adjustment with the exact request keys (no tenantId) and toasts', async () => {
    const ok = await store.createAdjustment({
      adjustmentType: 1,
      karat: 21,
      weightInGrams: 50.5,
      reason: 'زيادة رصيد',
      notes: null,
      date: '2026-08-20T00:00:00.000Z',
    });

    expect(ok).toBe(true);
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('passes adjustment filter query params (type + dates)', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(ADJUSTMENTS, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(ADJUSTMENTS_PAGE);
      }),
    );

    await store.loadAdjustments({
      page: 1,
      pageSize: 15,
      adjustmentType: '2',
      fromDate: '2026-08-01',
      toDate: '2026-08-31',
    });

    expect((captured as any)?.get('adjustmentType')).toBe('2');
    expect((captured as any)?.get('fromDate')).toBe('2026-08-01');
    expect((captured as any)?.get('toDate')).toBe('2026-08-31');
  });

  it('passes gold-ledger filter query params (karat + referenceType)', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(LEDGER, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(LEDGER_PAGE);
      }),
    );

    await store.loadLedger({
      page: 1,
      pageSize: 15,
      karat: 21,
      referenceType: 'Sale',
    });

    expect((captured as any)?.get('karat')).toBe('21');
    expect((captured as any)?.get('referenceType')).toBe('Sale');
  });

  it('passes the trend days param', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(`${LEDGER}/trend`, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(TREND);
      }),
    );

    await store.loadTrend(14);

    expect((captured as any)?.get('days')).toBe('14');
  });

  it('surfaces a list error without toasting it', async () => {
    server.use(
      http.get(ADJUSTMENTS, () =>
        HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 }),
      ),
    );

    await store.loadAdjustments({ page: 1, pageSize: 15 });

    expect(store.adjustmentsError()?.detail).toBe('خطأ خادم');
    expect(store.adjustments()).toBeNull();
    expect(toasts.toasts().length).toBe(0);
  });

  it('surfaces a ledger error signal', async () => {
    server.use(
      http.get(LEDGER, () => HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 })),
    );

    await store.loadLedger({ page: 1, pageSize: 15 });

    expect(store.ledgerError()?.detail).toBe('خطأ خادم');
    expect(store.ledger()).toBeNull();
  });
});