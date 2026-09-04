import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { firstValueFrom } from 'rxjs';

import type { MeResponse } from '../../shared/api/api-types';
import { apiUrl } from '../../testing/api-url';
import { AuthStore } from '../auth/auth-store';
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
  http.post(apiUrl('/api/v1/auth/login'), () => new HttpResponse(null, { status: 200 })),
  http.get(apiUrl('/api/v1/auth/me'), () => HttpResponse.json(ME)),
  http.post(apiUrl('/api/v1/auth/refresh'), () => {
    refreshCalls += 1;
    return new HttpResponse(null, { status: 200 });
  }),
);

describe('refreshInterceptor', () => {
  let client: HttpClient;
  let auth: AuthStore;

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    refreshCalls = 0;
  });
  afterAll(() => server.close());

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([refreshInterceptor]))],
    });
    client = TestBed.inject(HttpClient);
    auth = TestBed.inject(AuthStore);
    await auth.login('cashier@goldstore.app', 'secret');
  });

  it('rotates the refresh cookie once on a 401 and retries the original request', async () => {
    let served = 0;
    server.use(
      http.get(apiUrl('/api/v1/reference/karats'), () => {
        served += 1;
        return served === 1
          ? HttpResponse.json({ message: 'expired' }, { status: 401 })
          : HttpResponse.json([{ id: 1, name: '21K' }]);
      }),
    );

    const result = await firstValueFrom(client.get<unknown[]>(apiUrl('/api/v1/reference/karats')));

    expect(refreshCalls).toBe(1);
    expect(served).toBe(2);
    expect(result).toEqual([{ id: 1, name: '21K' }]);
    expect(auth.isAuthenticated()).toBe(true);
  });

  it('passes the 401 through and clears the session when rotation fails', async () => {
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
  });
});
