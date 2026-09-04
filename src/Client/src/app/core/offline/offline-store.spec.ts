import { TestBed } from '@angular/core/testing';

import { OfflineStore } from './offline-store';

describe('OfflineStore', () => {
  let store: OfflineStore;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    store = TestBed.inject(OfflineStore);
  });

  afterEach(() => {
    window.dispatchEvent(new Event('online'));
  });

  it('starts online when the browser reports a connection', () => {
    expect(store.online()).toBe(true);
    expect(store.offline()).toBe(false);
  });

  it('flips to offline on the window offline event', () => {
    window.dispatchEvent(new Event('offline'));
    expect(store.offline()).toBe(true);
  });

  it('recovers on the window online event', () => {
    window.dispatchEvent(new Event('offline'));
    window.dispatchEvent(new Event('online'));
    expect(store.online()).toBe(true);
    expect(store.offline()).toBe(false);
  });

  it('checkNow reports offline when navigator is offline', async () => {
    const descriptor = Object.getOwnPropertyDescriptor(Navigator.prototype, 'onLine');
    Object.defineProperty(Navigator.prototype, 'onLine', { configurable: true, value: false });
    try {
      const result = await store.checkNow();
      expect(result).toBe(false);
      expect(store.offline()).toBe(true);
    } finally {
      if (descriptor) {
        Object.defineProperty(Navigator.prototype, 'onLine', descriptor);
      }
    }
  });

  it('checkNow confirms online when the connection is back', async () => {
    window.dispatchEvent(new Event('offline'));
    const result = await store.checkNow();
    expect(result).toBe(true);
    expect(store.online()).toBe(true);
    expect(store.checking()).toBe(false);
  });
});
