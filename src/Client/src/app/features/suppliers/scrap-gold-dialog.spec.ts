import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { ScrapGoldDialog } from './scrap-gold-dialog';
import { SuppliersStore } from './suppliers-store';

const BASE = apiUrl('/api/v1');
const SCRAP_GOLD = `${BASE}/supplier-payments/scrap-gold`;

describe('ScrapGoldDialog', () => {
  let lastBody: Record<string, unknown> | null;

  const server = setupServer(
    http.get(`${BASE}/suppliers`, () =>
      HttpResponse.json([{ id: 'supplier-1', name: 'مورد', isActive: true, goldBalance: 20 }]),
    ),
    http.get(`${BASE}/suppliers/:id/balances`, () =>
      HttpResponse.json({
        supplierId: 'supplier-1',
        goldByKarat: [{ karat: 21, netWeight: 12.5 }],
        manufacturingByCurrency: [],
      }),
    ),
    http.get(`${BASE}/reference/karats`, () =>
      HttpResponse.json([
        { value: 18, label: 'عيار 18' },
        { value: 21, label: 'عيار 21' },
      ]),
    ),
    http.get(`${BASE}/reference/currencies`, () => HttpResponse.json([])),
    http.get(`${BASE}/categories`, () =>
      HttpResponse.json([{ id: 'cat-scrap', name: 'كسر', isActive: true }]),
    ),
    http.post(SCRAP_GOLD, async ({ request }) => {
      lastBody = (await request.json()) as Record<string, unknown>;
      return HttpResponse.json('payment-1', { status: 201 });
    }),
  );

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    lastBody = null;
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function openFixture(): Promise<ComponentFixture<ScrapGoldDialog>> {
    const store = TestBed.inject(SuppliersStore);
    await store.ensureLoaded();
    const fixture = TestBed.createComponent(ScrapGoldDialog);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  function setSelect(
    fixture: ComponentFixture<ScrapGoldDialog>,
    selector: string,
    value: string,
  ): void {
    const select = fixture.nativeElement.querySelector(selector) as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('input', { bubbles: true }));
    select.dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();
  }

  function setWeight(fixture: ComponentFixture<ScrapGoldDialog>, value: string): void {
    const input = fixture.nativeElement.querySelector('#scrap-weight') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
  }

  async function selectSupplierAndKarat(fixture: ComponentFixture<ScrapGoldDialog>): Promise<void> {
    await vi.waitFor(() => {
      expect(
        fixture.nativeElement.querySelectorAll('#scrap-supplier option').length,
      ).toBeGreaterThan(1);
    });
    fixture.detectChanges();
    setSelect(fixture, '#scrap-supplier', 'supplier-1');
    await vi.waitFor(() => {
      expect(fixture.nativeElement.querySelectorAll('#scrap-karat option').length).toBeGreaterThan(
        1,
      );
    });
    fixture.detectChanges();
    setSelect(fixture, '#scrap-karat', '21');
  }

  it('shows the due gold weight for the selected karat', async () => {
    const fixture = await openFixture();
    await selectSupplierAndKarat(fixture);

    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('المستحق للمورد عيار 21: 12.500 غ');
    });
  });

  it('auto-computes the remaining due weight as the weight is typed', async () => {
    const fixture = await openFixture();
    await selectSupplierAndKarat(fixture);
    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('المستحق للمورد عيار 21');
    });

    setWeight(fixture, '5');

    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('المتبقي بعد هذه الدفعة');
      expect(fixture.nativeElement.textContent).toContain('7.500');
    });
  });

  it('blocks a weight above the due gold weight', async () => {
    const fixture = await openFixture();
    await selectSupplierAndKarat(fixture);
    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('المستحق للمورد عيار 21');
    });

    setWeight(fixture, '20');

    const save = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حفظ الدفعة',
    ) as HTMLButtonElement;
    save.click();
    fixture.detectChanges();

    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('يتجاوز المستحق للمورد');
    });
    expect(lastBody).toBeNull();
  });
});
