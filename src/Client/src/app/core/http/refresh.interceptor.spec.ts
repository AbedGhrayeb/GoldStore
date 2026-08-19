import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { firstValueFrom } from 'rxjs';

import { AuthStore } from '../auth/auth-store';
import { TokenStorage } from '../auth/token-storage';
import type { MeResponse } from '../../shared/api/api-types';
import { apiUrl } from '../../testing/api-url';
import { bearerInterceptor } from './bearer.interceptor';
import { refreshInterceptor } from './refresh.interceptor';

const ME: MeResponse = {
  userId: 'user-1',
  email: 'cashier@goldstore.app',
  tenantId: 'tenant-1',
  tenantKey: 'goldstore',
  roles: [],
  permissions: [],
};

let refreshCalls = 0;

const server = setupServer(
  http.post(apiUrl('/api/v1/auth/login'), () =>
    HttpResponse.json({
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
      refreshExpiresAt: '2026-09-01T00:00:00Z',
    }),
  ),
  http.get(apiUrl('/api/v1/auth/me'), () => HttpResponse.json(ME)),
  http.post(apiUrl('/api/v1/auth/refresh'), () => {
    refreshCalls += 1;
    return HttpResponse.json({
      accessToken: 'access-2',
      refreshToken: 'refresh-2',
      refreshExpiresAt: '2026-09-01T00:00:00Z',
    });
  }),
);

describe('refreshInterceptor', () => {
  let client: HttpClient;
  let tokens: TokenStorage;
  let auth: AuthStore;

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    refreshCalls = 0;
  });
  afterAll(() => server.close());

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([bearerInterceptor, refreshInterceptor]))],
    });
    client = TestBed.inject(HttpClient);
    tokens = TestBed.inject(TokenStorage);
    auth = TestBed.inject(AuthStore);
    await auth.login('cashier@goldstore.app', 'secret');
  });

  it('rotates the token once on a 401 and retries the original request', async () => {
    let servedWith = '';
    server.use(
      http.get(apiUrl('/api/v1/reference/karats'), ({ request }) => {
        servedWith = request.headers.get('Authorization') ?? '';
        if (servedWith === 'Bearer access-1') {
          return HttpResponse.json({ message: 'expired' }, { status: 401 });
        }
        return HttpResponse.json([{ id: 1, name: '21K' }]);
      }),
    );

    const result = await firstValueFrom(client.get<unknown[]>(apiUrl('/api/v1/reference/karats')));

    expect(refreshCalls).toBe(1);
    expect(servedWith).toBe('Bearer access-2');
    expect(result).toEqual([{ id: 1, name: '21K' }]);
    expect(tokens.access).toBe('access-2');
  });

  it('passes the 401 through when rotation fails and the session is cleared', async () => {
    server.use(
      http.post(apiUrl('/api/v1/auth/refresh'), () => {
        refreshCalls += 1;
        return HttpResponse.json({ message: 'expired' }, { status: 401 });
      }),
      http.get(apiUrl('/api/v1/reference/karats'), () =>
        HttpResponse.json({ message: 'expired' }, { status: 401 }),
      ),
    );

    await expect(
      firstValueFrom(client.get(apiUrl('/api/v1/reference/karats'))),
    ).rejects.toMatchObject({ status: 401 });

    expect(refreshCalls).toBe(1);
    expect(tokens.access).toBeNull();
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('does not rotate for host (cookie-auth) requests', async () => {
    server.use(
      http.get(apiUrl('/host/api/v1/tenants'), () =>
        HttpResponse.json({ message: 'unauthorized' }, { status: 401 }),
      ),
    );

    await expect(
      firstValueFrom(client.get<unknown>(apiUrl('/host/api/v1/tenants'))),
    ).rejects.toMatchObject({ status: 401 });

    expect(refreshCalls).toBe(0);
    expect(tokens.access).toBe('access-1');
  });
});
