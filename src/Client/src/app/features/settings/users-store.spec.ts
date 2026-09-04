import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type { UserResponse } from './users-api.service';
import { UsersStore } from './users-store';

const USER_1: UserResponse = {
  id: 'user-1',
  email: 'ali@goldstore.app',
  firstName: 'علي',
  lastName: 'أحمد',
};

const USER_2: UserResponse = {
  id: 'user-2',
  email: 'sara@goldstore.app',
  firstName: 'سارة',
  lastName: 'محمد',
};

const ME: UserResponse = {
  id: 'user-1',
  email: 'ali@goldstore.app',
  firstName: 'علي',
  lastName: 'أحمد',
};

const BASE = apiUrl('/api/v1/users');

describe('UsersStore', () => {
  let store: UsersStore;
  let toasts: ToastStore;
  let users: UserResponse[];

  const server = setupServer(
    http.get(BASE, () => HttpResponse.json(users)),
    http.get(`${BASE}/me`, () => HttpResponse.json(ME)),
    http.post(BASE, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      const newUser: UserResponse = {
        id: 'user-new',
        email: body['email'] as string,
        firstName: body['firstName'] as string,
        lastName: body['lastName'] as string,
      };
      users = [...users, newUser];
      return HttpResponse.json('user-new', { status: 201 });
    }),
    http.put(`${BASE}/:id`, async ({ request, params }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      users = users.map((user) =>
        user.id === params['id']
          ? {
              ...user,
              firstName: body['firstName'] as string,
              lastName: body['lastName'] as string,
            }
          : user,
      );
      return HttpResponse.json(true);
    }),
    http.delete(`${BASE}/:id`, ({ params }) => {
      users = users.filter((user) => user.id !== params['id']);
      return HttpResponse.json(true);
    }),
  );

  beforeEach(() => {
    users = [USER_1, USER_2];
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    store = TestBed.inject(UsersStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads the user list and the current profile into the store', async () => {
    await store.load();

    expect(store.users()).toEqual([USER_1, USER_2]);
    expect(store.me()).toEqual(ME);
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('keeps the previous list and reports the error when a reload fails', async () => {
    await store.load();
    server.use(http.get(BASE, () => HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 })));

    await store.load();

    expect(store.error()).not.toBeNull();
    expect(store.error()?.detail).toBe('خطأ خادم');
    expect(store.users()).toEqual([USER_1, USER_2]);
  });

  it('loads the list only once for concurrent ensureLoaded calls', async () => {
    await Promise.all([store.ensureLoaded(), store.ensureLoaded(), store.ensureLoaded()]);

    expect(store.users()).toHaveLength(2);
  });

  it('creates a user, posts a tenantId-free payload and reloads the list', async () => {
    const ok = await store.createUser({
      email: 'omar@goldstore.app',
      firstName: 'عمر',
      lastName: 'خالد',
      password: 'secret123',
    });

    expect(ok).toBe(true);
    expect(store.users()?.find((user) => user.id === 'user-new')?.email).toBe('omar@goldstore.app');
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('surfaces a duplicate-email conflict inline without reloading', async () => {
    await store.load();
    server.use(
      http.post(BASE, () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
            title: 'Conflict',
            status: 409,
            detail: 'البريد الإلكتروني مستخدم بالفعل',
            errorCode: 'Users.DuplicateEmail',
          },
          { status: 409 },
        ),
      ),
    );

    const ok = await store.createUser({
      email: 'ali@goldstore.app',
      firstName: 'علي',
      lastName: 'أحمد',
      password: 'secret123',
    });

    expect(ok).toBe(false);
    expect(store.saveError()?.detail).toBe('البريد الإلكتروني مستخدم بالفعل');
    expect(store.users()).toHaveLength(2);
  });

  it('updates the profile through PUT with a tenantId-free payload and reloads', async () => {
    const ok = await store.updateUser('user-1', {
      firstName: 'علي الجديد',
      lastName: 'أحمد',
      password: null,
    });

    expect(ok).toBe(true);
    const updated = store.users()?.find((user) => user.id === 'user-1');
    expect(updated?.firstName).toBe('علي الجديد');
    expect(updated?.lastName).toBe('أحمد');
  });

  it('deletes a user and reloads', async () => {
    await store.deleteUser('user-2');

    expect(store.users()?.some((user) => user.id === 'user-2')).toBe(false);
    expect(store.users()).toHaveLength(1);
    expect(store.mutatingId()).toBeNull();
  });
});
