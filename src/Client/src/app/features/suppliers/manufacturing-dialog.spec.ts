import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { ManufacturingDialog } from './manufacturing-dialog';
import { SuppliersStore } from './suppliers-store';

const BASE = apiUrl('/api/v1');
const MANUFACTURING = `${BASE}/supplier-payments/manufacturing`;

describe('ManufacturingDialog', () => {
  let lastBody: Record<string, unknown> | null;

  const server = setupServer(
    http.get(`${BASE}/suppliers`, () =>
      HttpResponse.json([{ id: 'supplier-1', name: 'مورد', isActive: true }]),
    ),
    http.get(`${BASE}/suppliers/:id/balances`, () =>
      HttpResponse.json({
        supplierId: 'supplier-1',
        goldByKarat: [{ karat: 21, netWeight: 12.5 }],
        manufacturingByCurrency: [
          { currency: 'JOD', netAmount: 500 },
          { currency: 'USD', netAmount: 120 },
        ],
      }),
    ),
    http.get(`${BASE}/finance/accounts`, () =>
      HttpResponse.json([
        { id: 'acc-jod', name: 'صندوق', currency: 'JOD', accountType: 'Cash', isActive: true },
        {
          id: 'acc-jod-bank',
          name: 'بنك دينار',
          currency: 'JOD',
          accountType: 'Bank',
          isActive: true,
        },
        { id: 'acc-usd', name: 'بنك دولار', currency: 'USD', accountType: 'Bank', isActive: true },
      ]),
    ),
    http.get(`${BASE}/reference/karats`, () => HttpResponse.json([])),
    http.get(`${BASE}/reference/currencies`, () =>
      HttpResponse.json([
        { value: 0, code: 'JOD', symbol: 'د.أ' },
        { value: 1, code: 'USD', symbol: '$' },
        { value: 2, code: 'ILS', symbol: '₪' },
      ]),
    ),
    http.post(MANUFACTURING, async ({ request }) => {
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

  async function openFixture(): Promise<ComponentFixture<ManufacturingDialog>> {
    const store = TestBed.inject(SuppliersStore);
    await store.ensureLoaded();
    await store.ensureAccounts();
    const fixture = TestBed.createComponent(ManufacturingDialog);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  function setSelect(element: HTMLSelectElement, value: string): void {
    element.value = value;
    element.dispatchEvent(new Event('input', { bubbles: true }));
    element.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function setDialogSelect(
    fixture: ComponentFixture<ManufacturingDialog>,
    selector: string,
    value: string,
  ): void {
    setSelect(fixture.nativeElement.querySelector(selector) as HTMLSelectElement, value);
    fixture.detectChanges();
  }

  function setInput(element: HTMLInputElement, value: string): void {
    element.value = value;
    element.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function legSelect(
    fixture: ComponentFixture<ManufacturingDialog>,
    index: number,
    field: string,
  ): HTMLSelectElement {
    return fixture.nativeElement.querySelector(
      `table select[data-idx="${index}"][data-field="${field}"]`,
    ) as HTMLSelectElement;
  }

  function legInput(
    fixture: ComponentFixture<ManufacturingDialog>,
    index: number,
    field: string,
  ): HTMLInputElement {
    return fixture.nativeElement.querySelector(
      `table input[data-idx="${index}"][data-field="${field}"]`,
    ) as HTMLInputElement;
  }

  function clickSave(fixture: ComponentFixture<ManufacturingDialog>): void {
    const save = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حفظ الدفعة',
    ) as HTMLButtonElement;
    save.click();
    fixture.detectChanges();
  }

  async function selectSupplier(fixture: ComponentFixture<ManufacturingDialog>): Promise<void> {
    await vi.waitFor(() => {
      expect(fixture.nativeElement.querySelectorAll('#mfg-supplier option').length).toBeGreaterThan(
        1,
      );
    });
    fixture.detectChanges();
    setDialogSelect(fixture, '#mfg-supplier', 'supplier-1');
  }

  function enableMulti(fixture: ComponentFixture<ManufacturingDialog>): void {
    const checkbox = fixture.nativeElement.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    checkbox.checked = true;
    checkbox.dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();
  }

  it('shows the supplier dues per currency as selectable cards', async () => {
    const fixture = await openFixture();
    await selectSupplier(fixture);

    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('المستحق حسب العملة');
      expect(fixture.nativeElement.textContent).toContain('500.000');
      expect(fixture.nativeElement.textContent).toContain('120.000');
    });
  });

  it('auto-fills the amount with the due in the payment currency', async () => {
    const fixture = await openFixture();
    await selectSupplier(fixture);

    await vi.waitFor(() => {
      const amount = fixture.nativeElement.querySelector('#mfg-amount') as HTMLInputElement;
      expect(amount.value).toBe('500');
    });
  });

  it('filters single-mode accounts by payment method', async () => {
    const fixture = await openFixture();
    await selectSupplier(fixture);

    await vi.waitFor(() => {
      const options = [...fixture.nativeElement.querySelectorAll('#mfg-account option')].map(
        (option: unknown) => (option as HTMLOptionElement).textContent,
      );
      expect(options.join(' ')).toContain('صندوق');
      expect(options.join(' ')).not.toContain('بنك دينار');
    });

    const bank = fixture.nativeElement.querySelector(
      'input[name="mfg-payment-method"][value="2"]',
    ) as HTMLInputElement;
    bank.click();
    fixture.detectChanges();

    await vi.waitFor(() => {
      const options = [...fixture.nativeElement.querySelectorAll('#mfg-account option')].map(
        (option: unknown) => (option as HTMLOptionElement).textContent,
      );
      expect(options.join(' ')).toContain('بنك دينار');
      expect(options.join(' ')).not.toContain('صندوق');
    });
  });

  it('converts multi-currency legs into the due currency for totals and remainder', async () => {
    const fixture = await openFixture();
    await selectSupplier(fixture);
    enableMulti(fixture);

    // Leg 1: 50 JOD cash.
    setSelect(legSelect(fixture, 0, 'accountId'), 'acc-jod');
    fixture.detectChanges();
    setInput(legInput(fixture, 0, 'amount'), '50');
    fixture.detectChanges();

    // Leg 2: 100 USD bank @ 0.71 → 71 JOD (currency follows the account, like sales).
    const add = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('إضافة دفعة'),
    ) as HTMLButtonElement;
    add.click();
    fixture.detectChanges();
    setSelect(legSelect(fixture, 1, 'accountId'), 'acc-usd');
    fixture.detectChanges();
    setInput(legInput(fixture, 1, 'amount'), '100');
    setInput(legInput(fixture, 1, 'rate'), '0.71');
    fixture.detectChanges();

    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('121.000');
      expect(fixture.nativeElement.textContent).toContain('379.000');
    });

    clickSave(fixture);

    await vi.waitFor(() => expect(lastBody).not.toBeNull());
    expect(lastBody?.['supplierId']).toBe('supplier-1');
    expect(lastBody?.['currency']).toBe('JOD');
    expect(lastBody?.['amount']).toBe(121);
    expect(lastBody?.['accountId']).toBe('acc-jod');
    expect(lastBody?.['paymentLegs']).toEqual([
      { accountId: 'acc-jod', currency: 'JOD', amount: 50, exchangeRate: 1 },
      { accountId: 'acc-usd', currency: 'USD', amount: 100, exchangeRate: 0.71 },
    ]);
  });

  it('ignores unrated cross-currency legs in the total until a rate is entered', async () => {
    const fixture = await openFixture();
    await selectSupplier(fixture);
    enableMulti(fixture);

    // Leg 1: 50 JOD cash.
    setSelect(legSelect(fixture, 0, 'accountId'), 'acc-jod');
    fixture.detectChanges();
    setInput(legInput(fixture, 0, 'amount'), '50');
    fixture.detectChanges();

    // Leg 2: 500 USD with no rate yet — must contribute 0, not 500.
    const add = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('إضافة دفعة'),
    ) as HTMLButtonElement;
    add.click();
    fixture.detectChanges();
    setSelect(legSelect(fixture, 1, 'accountId'), 'acc-usd');
    fixture.detectChanges();
    setInput(legInput(fixture, 1, 'amount'), '500');
    fixture.detectChanges();

    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('50.000');
      expect(fixture.nativeElement.textContent).toContain('450.000');
    });
    expect(fixture.nativeElement.textContent).not.toContain('550.000');
  });

  it('blocks a single payment above the supplier due', async () => {
    const fixture = await openFixture();
    await selectSupplier(fixture);
    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('المستحق حسب العملة');
    });

    const amount = fixture.nativeElement.querySelector('#mfg-amount') as HTMLInputElement;
    setInput(amount, '600');
    fixture.detectChanges();
    setDialogSelect(fixture, '#mfg-account', 'acc-jod');

    clickSave(fixture);

    await vi.waitFor(() => {
      expect(fixture.nativeElement.textContent).toContain('يتجاوز المستحق للمورد');
    });
    expect(lastBody).toBeNull();
  });
});
