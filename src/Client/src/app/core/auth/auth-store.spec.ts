import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import type { MeResponse } from '../../shared/api/api-types';
import { apiUrl } from '../../testing/api-url';
import { AuthStore } from './auth-store';

const ME: MeResponse = {
  userId: 'user-1',
  email: 'cashier@goldstore.app',
  tenantId: 'tenant-1',
  tenantKey: 'goldstore',
  roles: ['Cashier'],
  permissions: ['catalog', 'sales', 'inventory'],
};

let refreshCalls = 0;

const server = setupServer(
  http.post(apiUrl('/api/v1/auth/login'), async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>;
    if (Object.keys(body).includes('tenantId')) {
      return HttpResponse.json({ message: 'tenantId must never be sent' }, { status: 400 });
    }
    // The server issues the JWT and stores it in an HttpOnly cookie; the body carries no tokens.
    return new HttpResponse(null, { status: 200 });
  }),
  http.post(apiUrl('/api/v1/auth/refresh'), () => {
    refreshCalls += 1;
    return new HttpResponse(null, { status: 200 });
  }),
  http.post(apiUrl('/api/v1/auth/logout'), () => new HttpResponse(null, { status: 204 })),
  http.get(apiUrl('/api/v1/auth/me'), () => HttpResponse.json(ME)),
  http.get(apiUrl('/host/api/v1/auth/me'), () =>
    HttpResponse.json({ email: 'platform@goldstore.app' }),
  ),
);

describe('AuthStore', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    refreshCalls = 0;
    localStorage.clear();
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  it('logs in through the API (JWT set as an HttpOnly cookie) and loads claims from /me', async () => {
    const store = TestBed.inject(AuthStore);

    const me = (await store.login('cashier@goldstore.app', 'secret')) as MeResponse;

    expect(me.permissions).toEqual(['catalog', 'sales', 'inventory']);
    expect(store.isAuthenticated()).toBe(true);
    expect(store.tenantKey()).toBe('goldstore');
    expect(store.permissions()).toContain('sales');
  });

  it('never sends tenantId in the login payload', async () => {
    const store = TestBed.inject(AuthStore);
    await expect(store.login('cashier@goldstore.app', 'secret')).resolves.toBeTruthy();
    expect(refreshCalls).toBe(0);
  });

  it('rotates the HttpOnly refresh cookie on silentRefresh, single-flight for concurrent callers', async () => {
    const store = TestBed.inject(AuthStore);
    await store.login('cashier@goldstore.app', 'secret');

    const results = await Promise.all([
      store.silentRefresh(),
      store.silentRefresh(),
      store.silentRefresh(),
    ]);

    expect(refreshCalls).toBe(1);
    expect(results).toEqual([true, true, true]);
    expect(store.isAuthenticated()).toBe(true);
  });

  it('returns false and clears the session when rotation fails', async () => {
    server.use(
      http.post(apiUrl('/api/v1/auth/refresh'), () =>
        HttpResponse.json({ message: 'expired' }, { status: 401 }),
      ),
    );
    const store = TestBed.inject(AuthStore);
    await store.login('cashier@goldstore.app', 'secret');

    await expect(store.silentRefresh()).resolves.toBe(false);
    expect(store.isAuthenticated()).toBe(false);
  });

  it('revokes the session on logout and clears state', async () => {
    const store = TestBed.inject(AuthStore);
    await store.login('cashier@goldstore.app', 'secret');

    await store.logout();

    expect(store.isAuthenticated()).toBe(false);
  });

  it('restores both cookie sessions from /me and /host/me on reload', async () => {
    localStorage.setItem('isTenantAuthenticated', 'true');
    localStorage.setItem('isHostAdmin', 'true');
    const store = TestBed.inject(AuthStore);

    await store.restoreSessions();

    expect(store.isAuthenticated()).toBe(true);
    expect(store.isHostAdmin()).toBe(true);
    expect(store.tenantKey()).toBe('goldstore');
  });
});
