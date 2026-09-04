import { provideHttpClient, withFetch } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { GoldPricesPage } from './gold-prices-page';

const PRICES = {
  spotPrice: {
    price: 2400.5,
    displayPrice: '2,400.50 د.أ',
    changePercent24H: 1.25,
    changeDirection: 'up',
    currency: 'JOD',
    currencySymbol: 'د.أ',
    unit: 'أونصة',
  },
  pricePerGram24K: {
    price: 52.1,
    displayPrice: '52.100 د.أ',
    changePercent24H: 1.25,
    changeDirection: 'up',
    currency: 'JOD',
    currencySymbol: 'د.أ',
    unit: 'جم',
  },
  pricePerGram21K: {
    price: 45.75,
    displayPrice: '45.750 د.أ',
    changePercent24H: -0.4,
    changeDirection: 'down',
    currency: 'JOD',
    currencySymbol: 'د.أ',
    unit: 'جم',
  },
};

let priceCalls = 0;

function respond(body: typeof PRICES = PRICES) {
  priceCalls += 1;
  return HttpResponse.json(body);
}

const server = setupServer(http.get(apiUrl('/api/v1/gold-prices/current'), () => respond()));

describe('GoldPricesPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    priceCalls = 0;
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(withFetch())] });
  });

  function createFixture(): ComponentFixture<GoldPricesPage> {
    const fixture = TestBed.createComponent(GoldPricesPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<GoldPricesPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  it('renders the spot, 24K and 21K tiles from the current feed', async () => {
    const fixture = createFixture();
    await vi.waitFor(() => expect(text(fixture)).toContain('2,400.50 د.أ'));
    fixture.detectChanges();

    expect(text(fixture)).toContain('أسعار الذهب');
    expect(text(fixture)).toContain('سعر الأونصة');
    expect(text(fixture)).toContain('غرام عيار 24');
    expect(text(fixture)).toContain('غرام عيار 21');
    expect(text(fixture)).toContain('52.100 د.أ');
    expect(text(fixture)).toContain('45.750 د.أ');
    expect(text(fixture)).toContain('العالمي');
  });

  it('refetches the feed on refresh and updates the tiles without a reload', async () => {
    const fixture = createFixture();
    await vi.waitFor(() => expect(text(fixture)).toContain('2,400.50 د.أ'));

    server.use(
      http.get(apiUrl('/api/v1/gold-prices/current'), () =>
        respond({
          ...PRICES,
          spotPrice: { ...PRICES.spotPrice, displayPrice: '2,500.00 د.أ' },
        }),
      ),
    );

    const refreshButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'تحديث',
    ) as HTMLButtonElement;
    refreshButton.click();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(text(fixture)).toContain('2,500.00 د.أ');
    });
    expect(priceCalls).toBe(2);
  });

  it('recovers from a failed refresh via retry', async () => {
    const fixture = createFixture();
    await vi.waitFor(() => expect(text(fixture)).toContain('2,400.50 د.أ'));

    server.use(
      http.get(apiUrl('/api/v1/gold-prices/current'), () => {
        priceCalls += 1;
        return HttpResponse.error();
      }),
    );

    const refreshButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'تحديث',
    ) as HTMLButtonElement;
    refreshButton.click();
    fixture.detectChanges();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(text(fixture)).toContain('تعذّر تحميل الأسعار');
    });

    server.use(
      http.get(apiUrl('/api/v1/gold-prices/current'), () =>
        respond({
          ...PRICES,
          pricePerGram21K: { ...PRICES.pricePerGram21K, displayPrice: '46.000 د.أ' },
        }),
      ),
    );
    refreshButton.click();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(text(fixture)).toContain('46.000 د.أ');
    });
  });

  it('shows a retry state when the first fetch fails', async () => {
    server.use(
      http.get(apiUrl('/api/v1/gold-prices/current'), () => {
        priceCalls += 1;
        return HttpResponse.error();
      }),
    );

    const fixture = createFixture();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(text(fixture)).toContain('تعذّر تحميل الأسعار');
    });

    expect(text(fixture)).toContain('إعادة المحاولة');
  });
});
