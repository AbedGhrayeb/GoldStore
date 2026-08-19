import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import type { MeResponse } from '../../shared/api/api-types';
import { apiUrl } from '../../testing/api-url';
import { AuthStore } from './auth-store';
import { TokenStorage } from './token-storage';

const ME: MeResponse = {
  userId: 'user-1',
  email: 'cashier@goldstore.app',
  tenantId: 'tenant-1',
  tenantKey: 'goldstore',
  roles: ['Cashier'],
  permissions: ['catalog', 'sales', 'inventory'],
};

const TOKENS = {
  accessToken: 'access-1',
  refreshToken: 'refresh-1',
  refreshExpiresAt: '2026-09-01T00:00:00Z',
};

let refreshCalls = 0;

const server = setupServer(
  http.post(apiUrl('/api/v1/auth/login'), async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>;
    if (Object.keys(body).includes('tenantId')) {
      return HttpResponse.json({ message: 'tenantId must never be sent' }, { status: 400 });
    }
    return HttpResponse.json(TOKENS);
  }),
  http.post(apiUrl('/api/v1/auth/refresh'), () => {
    refreshCalls += 1;
    return HttpResponse.json({ ...TOKENS, accessToken: 'access-2', refreshToken: 'refresh-2' });
  }),
  http.post(apiUrl('/api/v1/auth/logout'), () => new HttpResponse(null, { status: 204 })),
  http.get(apiUrl('/api/v1/auth/me'), () => HttpResponse.json(ME)),
);

describe('AuthStore', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    refreshCalls = 0;
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  it('logs in, stores tokens in memory only, and loads claims from /me', async () => {
    const store = TestBed.inject(AuthStore);
    const tokens = TestBed.inject(TokenStorage);

    const me = await store.login('cashier@goldstore.app', 'secret');

    expect(me.permissions).toEqual(['catalog', 'sales', 'inventory']);
    expect(tokens.access).toBe('access-1');
    expect(tokens.refresh).toBe('refresh-1');
    expect(store.isAuthenticated()).toBe(true);
    expect(store.tenantKey()).toBe('goldstore');
    expect(store.permissions()).toContain('sales');
  });

  it('never sends tenantId in the login payload', async () => {
    const store = TestBed.inject(AuthStore);
    await expect(store.login('cashier@goldstore.app', 'secret')).resolves.toBeTruthy();
    expect(refreshCalls).toBe(0);
  });

  it('rotates tokens on silentRefresh and is single-flight for concurrent callers', async () => {
    const store = TestBed.inject(AuthStore);
    const tokens = TestBed.inject(TokenStorage);
    await store.login('cashier@goldstore.app', 'secret');

    const results = await Promise.all([
      store.silentRefresh(),
      store.silentRefresh(),
      store.silentRefresh(),
    ]);

    expect(refreshCalls).toBe(1);
    expect(results).toEqual([true, true, true]);
    expect(tokens.access).toBe('access-2');
    expect(tokens.refresh).toBe('refresh-2');
    expect(store.isAuthenticated()).toBe(true);
  });

  it('returns false without calling the API when no refresh token exists', async () => {
    const store = TestBed.inject(AuthStore);

    await expect(store.silentRefresh()).resolves.toBe(false);
    expect(refreshCalls).toBe(0);
  });

  it('clears the session when token rotation fails', async () => {
    server.use(
      http.post(apiUrl('/api/v1/auth/refresh'), () =>
        HttpResponse.json({ message: 'expired' }, { status: 401 }),
      ),
    );
    const store = TestBed.inject(AuthStore);
    const tokens = TestBed.inject(TokenStorage);
    await store.login('cashier@goldstore.app', 'secret');

    await expect(store.silentRefresh()).resolves.toBe(false);
    expect(tokens.access).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
  });

  it('revokes the refresh token on logout and clears all state', async () => {
    let revokedBody: unknown = null;
    server.use(
      http.post(apiUrl('/api/v1/auth/logout'), async ({ request }) => {
        revokedBody = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const store = TestBed.inject(AuthStore);
    const tokens = TestBed.inject(TokenStorage);
    await store.login('cashier@goldstore.app', 'secret');

    await store.logout();

    expect(revokedBody).toEqual({ refreshToken: 'refresh-1' });
    expect(tokens.hasTokens()).toBe(false);
    expect(store.isAuthenticated()).toBe(false);
  });
});
