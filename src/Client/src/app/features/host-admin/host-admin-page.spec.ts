import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { HostAdminPage } from './host-admin-page';
import { HostStore } from './host-store';

const TENANTS = apiUrl('/host/api/v1/tenants');
const RECON = apiUrl('/host/api/v1/reconciliation');

const TENANTS_PAYLOAD = [
  { id: 'tenant-1', key: 'goldstore-1', name: 'متجر الذهب الأول', status: 'Active' },
  { id: 'tenant-2', key: 'goldstore-2', name: 'متجر الذهب الثاني', status: 'Trial' },
];

const RECON_PAYLOAD = {
  generatedAtUtc: '2026-08-21T10:00:00Z',
  anomalies: [],
  tenants: [
    {
      tenantId: 'tenant-1',
      key: 'goldstore-1',
      name: 'متجر الذهب الأول',
      status: 'Active',
      rowCounts: [{ entity: 'Users', count: 3 }],
      goldStock: [{ karat: 21, weightGrams: 50, equivalent21K: 50 }],
      totalEquivalent21K: 50,
      financialBalances: [],
      financialTotals: [{ currency: 'JOD', totalInflow: 1000, totalOutflow: 200, net: 800 }],
      debtTotals: [],
      supplierGoldBalances: [],
      supplierManufacturingBalances: [],
    },
  ],
};

describe('HostAdminPage', () => {
  const server = setupServer(
    http.get(TENANTS, () => HttpResponse.json(TENANTS_PAYLOAD)),
    http.get(RECON, () => HttpResponse.json(RECON_PAYLOAD)),
    http.patch(apiUrl('/host/api/v1/tenants/tenant-1/status'), async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
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

  async function createFixture(): Promise<ComponentFixture<HostAdminPage>> {
    const store = TestBed.inject(HostStore);
    await store.loadTenants();
    const fixture = TestBed.createComponent(HostAdminPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<HostAdminPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  function buttonByText(fixture: ComponentFixture<HostAdminPage>, label: string): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')].find(
      (b: HTMLButtonElement) => b.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  it('renders tenants tab first with status badges', async () => {
    const fixture = await createFixture();
    expect(text(fixture)).toContain('إدارة المنصة');
    expect(text(fixture)).toContain('متجر الذهب الأول');
    expect(text(fixture)).toContain('نشط');
    expect(text(fixture)).toContain('تجريبي');
  });

  it('switches to reconciliation tab and runs reconciliation', async () => {
    const fixture = await createFixture();
    const reconTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (b: HTMLButtonElement) => b.textContent?.includes('المطابقة'),
    ) as HTMLButtonElement;
    reconTab.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(text(fixture)).toContain('تشغيل المطابقة');

    buttonByText(fixture, 'تشغيل المطابقة').click();
    await vi.waitFor(() => expect(text(fixture)).toContain('إجمالي مكافئ 21K'));
    expect(text(fixture)).toContain('متجر الذهب الأول');
  });

  it('opens status dialog and patches with exact payload', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.patch(apiUrl('/host/api/v1/tenants/tenant-1/status'), async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({});
      }),
    );
    const fixture = await createFixture();
    const editButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (b: HTMLButtonElement) => b.title === 'تغيير الحالة',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(text(fixture)).toContain('تغيير حالة المستأجر');

    const select = fixture.nativeElement.querySelector('#host-status') as HTMLSelectElement;
    select.value = '3';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'تأكيد التغيير').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());
    expect(captured!['newStatus']).toBe(3);
    expect(Object.keys(captured!)).not.toContain('tenantId');
  });

  it('filters reconciliation by selected tenant', async () => {
    let capturedUrl: string | null = null;
    server.use(
      http.get(RECON, ({ request }) => {
        capturedUrl = request.url;
        return HttpResponse.json(RECON_PAYLOAD);
      }),
    );
    const fixture = await createFixture();
    const reconTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (b: HTMLButtonElement) => b.textContent?.includes('المطابقة'),
    ) as HTMLButtonElement;
    reconTab.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const select = fixture.nativeElement.querySelector('#recon-tenant') as HTMLSelectElement;
    select.value = 'tenant-1';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'تشغيل المطابقة').click();
    await vi.waitFor(() => expect(capturedUrl).toContain('tenantId=tenant-1'));
  });

  it('shows anomalies when present', async () => {
    const reconWithAnomalies = {
      ...RECON_PAYLOAD,
      anomalies: [{ table: 'GoldLedgerEntries', tenantId: null, count: 1, message: 'Missing TenantId' }],
    };
    server.use(http.get(RECON, () => HttpResponse.json(reconWithAnomalies)));

    const fixture = await createFixture();
    const reconTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (b: HTMLButtonElement) => b.textContent?.includes('المطابقة'),
    ) as HTMLButtonElement;
    reconTab.click();
    fixture.detectChanges();
    await fixture.whenStable();

    buttonByText(fixture, 'تشغيل المطابقة').click();
    await vi.waitFor(() => expect(text(fixture)).toContain('Missing TenantId'));
  });
});
