import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { ReferenceStore } from './reference-store';

const KARATS = [
  { value: 18, label: '18K' },
  { value: 21, label: '21K' },
  { value: 24, label: '24K' },
];
const CURRENCIES = [
  { value: 1, code: 'JOD', symbol: 'د.أ' },
  { value: 2, code: 'USD', symbol: 'US$' },
  { value: 3, code: 'ILS', symbol: '₪' },
];

let karatCalls = 0;
let currencyCalls = 0;

const server = setupServer(
  http.get(apiUrl('/api/v1/reference/karats'), () => {
    karatCalls += 1;
    return HttpResponse.json(KARATS);
  }),
  http.get(apiUrl('/api/v1/reference/currencies'), () => {
    currencyCalls += 1;
    return HttpResponse.json(CURRENCIES);
  }),
);

describe('ReferenceStore', () => {
  let store: ReferenceStore;

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    karatCalls = 0;
    currencyCalls = 0;
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient()],
    });
    store = TestBed.inject(ReferenceStore);
  });

  it('loads karats and currencies once', async () => {
    await store.ensureLoaded();
    expect(store.karats()).toEqual(KARATS);
    expect(store.currencies()).toEqual(CURRENCIES);
    expect(store.loaded()).toBe(true);
    expect(store.loading()).toBe(false);
    expect(karatCalls).toBe(1);
    expect(currencyCalls).toBe(1);
  });

  it('is single-flight and skips repeat loads once loaded', async () => {
    await store.ensureLoaded();
    await store.ensureLoaded();
    expect(karatCalls).toBe(1);
  });

  it('refresh refetches and replaces the data', async () => {
    await store.ensureLoaded();
    server.use(
      http.get(apiUrl('/api/v1/reference/karats'), () =>
        HttpResponse.json([{ value: 22, label: '22K' }]),
      ),
    );
    await store.refresh();
    expect(store.karats()).toEqual([{ value: 22, label: '22K' }]);
    expect(store.currencies()).toEqual(CURRENCIES);
    expect(store.loaded()).toBe(true);
  });

  it('keeps loaded false on failure so a later retry can succeed', async () => {
    server.use(http.get(apiUrl('/api/v1/reference/karats'), () => HttpResponse.error()));
    await expect(store.ensureLoaded()).rejects.toThrow();
    expect(store.loaded()).toBe(false);

    server.resetHandlers();
    await store.ensureLoaded();
    expect(store.loaded()).toBe(true);
    expect(store.karats()).toEqual(KARATS);
  });
});
