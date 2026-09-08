import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { vi } from 'vitest';

import { apiUrl } from '../../testing/api-url';
import { GoldPriceStore, GOLD_PRICE_REFRESH_MS } from './gold-price-store';

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

const server = setupServer(
  http.get(apiUrl('/api/v1/gold-prices/current'), () => {
    priceCalls += 1;
    return HttpResponse.json(PRICES);
  }),
);

describe('GoldPriceStore', () => {
  let store: GoldPriceStore;

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    priceCalls = 0;
    store.stopAutoRefresh();
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient()],
    });
    store = TestBed.inject(GoldPriceStore);
  });

  it('loads the current feed and records lastUpdated', async () => {
    await store.ensureLoaded();
    expect(store.current()).toEqual(PRICES);
    expect(store.lastUpdated()).not.toBeNull();
    expect(priceCalls).toBe(1);
  });

  it('prefers the 21K per-gram price for the chip', async () => {
    await store.ensureLoaded();
    expect(store.chip()?.label).toBe('21');
    expect(store.chip()?.info.displayPrice).toBe('45.750 د.أ');
  });

  it('falls back to the spot price when no per-gram 21K price exists', async () => {
    server.use(
      http.get(apiUrl('/api/v1/gold-prices/current'), () =>
        HttpResponse.json({ spotPrice: PRICES.spotPrice }),
      ),
    );
    await store.ensureLoaded();
    expect(store.chip()?.label).toBe('العالمي');
    expect(store.chip()?.info.displayPrice).toBe('2,400.50 د.أ');
  });

  it('refresh replaces the feed', async () => {
    await store.ensureLoaded();
    server.use(
      http.get(apiUrl('/api/v1/gold-prices/current'), () =>
        HttpResponse.json({ spotPrice: { ...PRICES.spotPrice, price: 2500 } }),
      ),
    );
    await store.refresh();
    expect(store.current()?.spotPrice?.price).toBe(2500);
  });

  it('keeps the previous feed when a refresh fails', async () => {
    await store.ensureLoaded();
    server.use(http.get(apiUrl('/api/v1/gold-prices/current'), () => HttpResponse.error()));
    await expect(store.refresh()).rejects.toThrow();
    expect(store.current()).toEqual(PRICES);
  });

  it('auto-refreshes on the daily interval', async () => {
    vi.useFakeTimers();
    try {
      await store.ensureLoaded();
      store.startAutoRefresh();
      await vi.advanceTimersByTimeAsync(GOLD_PRICE_REFRESH_MS);
      expect(priceCalls).toBe(2);
    } finally {
      vi.useRealTimers();
    }
  });
});
