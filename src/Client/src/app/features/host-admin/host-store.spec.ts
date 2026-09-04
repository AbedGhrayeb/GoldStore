import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { HostStore } from './host-store';

const TENANTS = apiUrl('/host/api/v1/tenants');
const RECON = apiUrl('/host/api/v1/reconciliation');
const PATCH_TENANT = (id: string) => apiUrl(`/host/api/v1/tenants/${id}/status`);

const TENANT_FIXTURE = [
  { id: 'tenant-1', key: 'goldstore-1', name: 'متجر الذهب الأول', status: 'Active' },
  { id: 'tenant-2', key: 'goldstore-2', name: 'متجر الذهب الثاني', status: 'Trial' },
];

const RECON_FIXTURE = {
  generatedAtUtc: '2026-08-21T10:00:00Z',
  anomalies: [{ table: 'GoldLedgerEntries', tenantId: null, count: 2, message: 'Missing TenantId' }],
  tenants: [
    {
      tenantId: 'tenant-1',
      key: 'goldstore-1',
      name: 'متجر الذهب الأول',
      status: 'Active',
      rowCounts: [{ entity: 'Users', count: 5 }],
      goldStock: [{ karat: 21, weightGrams: 100, equivalent21K: 100 }],
      totalEquivalent21K: 100,
      financialBalances: [{ accountId: 'acc-1', accountName: 'الصندوق', currency: 'JOD', balance: 5000 }],
      financialTotals: [{ currency: 'JOD', totalInflow: 6000, totalOutflow: 1000, net: 5000 }],
      debtTotals: [{ currency: 'JOD', totalReceivable: 200, totalPayable: 0, net: 200 }],
      supplierGoldBalances: [],
      supplierManufacturingBalances: [],
    },
  ],
};

describe('HostStore', () => {
  const server = setupServer(
    http.get(TENANTS, () => HttpResponse.json(TENANT_FIXTURE)),
    http.get(RECON, () => HttpResponse.json(RECON_FIXTURE)),
    http.patch(PATCH_TENANT('tenant-1'), async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(body['newStatus']).toBe(2);
      expect(body['transitionAtUtc']).toBeDefined();
      // Must not contain tenantId in body
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json({});
    }),
  );

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  it('loads tenants from /host/api/v1/tenants', async () => {
    const store = TestBed.inject(HostStore);
    await store.loadTenants();
    expect(store.tenants()).toEqual(TENANT_FIXTURE);
    expect(store.tenantsError()).toBeNull();
  });

  it('loads reconciliation with optional tenantId query', async () => {
    let capturedUrl: string | null = null;
    server.use(
      http.get(RECON, ({ request }) => {
        capturedUrl = request.url;
        return HttpResponse.json(RECON_FIXTURE);
      }),
    );
    const store = TestBed.inject(HostStore);

    await store.loadReconciliation(null);
    expect(capturedUrl).not.toContain('tenantId=');

    await store.loadReconciliation('tenant-1');
    expect(capturedUrl).toContain('tenantId=tenant-1');
    expect(store.reconciliation()).toEqual(RECON_FIXTURE);
  });

  it('patches tenant status with exact UpdateTenantStatusRequest keys', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.patch(PATCH_TENANT('tenant-1'), async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({});
      }),
    );
    const store = TestBed.inject(HostStore);
    const ok = await store.updateTenantStatus('tenant-1', {
      newStatus: 2,
      transitionAtUtc: '2026-08-22T00:00:00.000Z',
    });
    expect(ok).toBe(true);
    expect(captured).toEqual({ newStatus: 2, transitionAtUtc: '2026-08-22T00:00:00.000Z' });
    expect(Object.keys(captured!)).not.toContain('tenantId');
  });

  it('surfaces saveError on 400 validation and does not toast', async () => {
    server.use(
      http.patch(PATCH_TENANT('tenant-1'), () =>
        HttpResponse.json(
          { type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1', title: 'Bad Request', status: 400, detail: 'Invalid transition' },
          { status: 400 },
        ),
      ),
    );
    const store = TestBed.inject(HostStore);
    const ok = await store.updateTenantStatus('tenant-1', { newStatus: 99, transitionAtUtc: null });
    expect(ok).toBe(false);
    expect(store.saveError()?.status).toBe(400);
  });

  it('surfaces tenantsError on failure', async () => {
    server.use(http.get(TENANTS, () => HttpResponse.json({ title: 'Unauthorized' }, { status: 401 })));
    const store = TestBed.inject(HostStore);
    await store.loadTenants();
    expect(store.tenantsError()?.status).toBe(401);
  });

  it('supports null transitionAtUtc for status moves without grace date', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.patch(PATCH_TENANT('tenant-1'), async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({});
      }),
    );
    const store = TestBed.inject(HostStore);
    await store.updateTenantStatus('tenant-1', { newStatus: 3, transitionAtUtc: null });
    expect(captured!['transitionAtUtc']).toBeNull();
  });
});
