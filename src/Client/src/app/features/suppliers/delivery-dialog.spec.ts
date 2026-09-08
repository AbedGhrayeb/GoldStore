import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { DeliveryDialog } from './delivery-dialog';
import { SuppliersStore } from './suppliers-store';

const BASE = apiUrl('/api/v1');
const DELIVERIES = `${BASE}/supplier-deliveries`;

describe('DeliveryDialog', () => {
  let lastBody: Record<string, unknown> | null;

  const server = setupServer(
    http.get(`${BASE}/suppliers`, () =>
      HttpResponse.json([{ id: 'supplier-1', name: 'مورد', isActive: true }]),
    ),
    http.get(`${BASE}/categories`, () => HttpResponse.json([])),
    http.get(`${BASE}/finance/accounts`, () =>
      HttpResponse.json([
        { id: 'acc-jod', name: 'صندوق', currency: 'JOD', accountType: 'Cash', isActive: true },
        { id: 'acc-usd', name: 'بنك دولار', currency: 'USD', accountType: 'Bank', isActive: true },
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
    http.post(DELIVERIES, async ({ request }) => {
      lastBody = (await request.json()) as Record<string, unknown>;
      return HttpResponse.json('delivery-1', { status: 201 });
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

  async function openFixture(): Promise<ComponentFixture<DeliveryDialog>> {
    const store = TestBed.inject(SuppliersStore);
    await store.ensureLoaded();
    await store.ensureAccounts();
    const fixture = TestBed.createComponent(DeliveryDialog);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  function setInput(
    fixture: ComponentFixture<DeliveryDialog>,
    selector: string,
    value: string,
  ): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
  }

  function setSelect(
    fixture: ComponentFixture<DeliveryDialog>,
    selector: string,
    value: string,
  ): void {
    const select = fixture.nativeElement.querySelector(selector) as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('input', { bubbles: true }));
    select.dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();
  }

  async function fillLine(
    fixture: ComponentFixture<DeliveryDialog>,
    karat: string,
    weight: string,
  ): Promise<void> {
    await vi.waitFor(() => {
      expect(fixture.nativeElement.querySelectorAll('#line-karat-0 option').length).toBeGreaterThan(
        1,
      );
    });
    fixture.detectChanges();
    setSelect(fixture, '#line-karat-0', karat);
    setInput(fixture, '#line-weight-0', weight);
  }

  it('auto-suggests the due amount from fee and total weight', async () => {
    const fixture = await openFixture();
    await vi.waitFor(() => {
      expect(
        fixture.nativeElement.querySelectorAll('#delivery-supplier option').length,
      ).toBeGreaterThan(1);
    });
    fixture.detectChanges();
    await fillLine(fixture, '21', '10');
    setInput(fixture, '#delivery-fee', '2');

    await vi.waitFor(() => {
      const due = fixture.nativeElement.querySelector('#delivery-due') as HTMLInputElement;
      expect(due.value).toBe('20');
    });
  });

  it('submits the delivery with paymentLegs null (pay-later via payment forms)', async () => {
    const fixture = await openFixture();
    await vi.waitFor(() => {
      expect(
        fixture.nativeElement.querySelectorAll('#delivery-supplier option').length,
      ).toBeGreaterThan(1);
    });
    fixture.detectChanges();
    setSelect(fixture, '#delivery-supplier', 'supplier-1');
    await fillLine(fixture, '21', '10');
    setInput(fixture, '#delivery-fee', '2');
    setSelect(fixture, '#delivery-due-currency', 'USD');
    setInput(fixture, '#delivery-due', '15');

    const save = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حفظ التسليم',
    ) as HTMLButtonElement;
    save.click();
    fixture.detectChanges();

    await vi.waitFor(() => expect(lastBody).not.toBeNull());
    expect(lastBody?.['amountDue']).toBe(15);
    expect(lastBody?.['amountDueCurrency']).toBe('USD');
    expect(lastBody?.['manufacturingFeePerGram']).toBe(2);
    expect(lastBody?.['manufacturingFeeCurrency']).toBe('JOD');
    expect(lastBody?.['paymentLegs']).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('دفع المستحق الآن');
  });
});
