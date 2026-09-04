import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { AuthStore } from '../../core/auth/auth-store';
import type { UserResponse } from './users-api.service';
import { UsersPage } from './users-page';
import { UsersStore } from './users-store';

const USERS: UserResponse[] = [
  {
    id: 'user-1',
    email: 'ali@goldstore.app',
    firstName: 'علي',
    lastName: 'أحمد',
  },
  {
    id: 'user-2',
    email: 'sara@goldstore.app',
    firstName: 'سارة',
    lastName: 'محمد',
  },
];

const ME: UserResponse = USERS[0] as UserResponse;

const BASE = apiUrl('/api/v1/users');

describe('UsersPage', () => {
  let users: UserResponse[];

  const server = setupServer(
    http.get(BASE, () => HttpResponse.json(users)),
    http.get(`${BASE}/me`, () => HttpResponse.json(ME)),
    http.post(BASE, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      users = [
        ...users,
        {
          id: 'user-new',
          email: body['email'] as string,
          firstName: body['firstName'] as string,
          lastName: body['lastName'] as string,
        },
      ];
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

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    users = [USERS[0] as UserResponse, USERS[1] as UserResponse];
  });
  afterAll(() => server.close());

  beforeEach(() => {
    users = [USERS[0] as UserResponse, USERS[1] as UserResponse];
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    const auth = TestBed.inject(AuthStore);
    (auth as unknown as { userSignal: { set: (v: unknown) => void } }).userSignal.set({
      userId: 'user-1',
      email: 'ali@goldstore.app',
      tenantId: 'tenant-1',
      tenantKey: 'test',
      roles: ['store_admin'],
      permissions: ['settings'],
    });
  });

  async function createLoadedFixture(): Promise<ComponentFixture<UsersPage>> {
    const auth = TestBed.inject(AuthStore);
    (auth as unknown as { userSignal: { set: (v: unknown) => void } }).userSignal.set({
      userId: 'user-1',
      email: 'ali@goldstore.app',
      tenantId: 'tenant-1',
      tenantKey: 'test',
      roles: ['store_admin'],
      permissions: ['settings'],
    });
    const store = TestBed.inject(UsersStore);
    await store.load();
    const fixture = TestBed.createComponent(UsersPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<UsersPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  it('renders the profile card, the users table and the self badge', async () => {
    const fixture = await createLoadedFixture();
    const content = text(fixture);

    expect(content).toContain('علي أحمد');
    expect(content).toContain('ali@goldstore.app');
    expect(content).toContain('سارة محمد');
    expect(content).toContain('sara@goldstore.app');
    expect(content).toContain('أنت');
  });

  it('creates a user through the form dialog and refreshes the table', async () => {
    const fixture = await createLoadedFixture();
    const addButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('إضافة مستخدم'),
    ) as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    const email = fixture.nativeElement.querySelector('#user-email') as HTMLInputElement;
    email.value = 'omar@goldstore.app';
    email.dispatchEvent(new Event('input'));
    const firstName = fixture.nativeElement.querySelector('#user-first-name') as HTMLInputElement;
    firstName.value = 'عمر';
    firstName.dispatchEvent(new Event('input'));
    const lastName = fixture.nativeElement.querySelector('#user-last-name') as HTMLInputElement;
    lastName.value = 'خالد';
    lastName.dispatchEvent(new Event('input'));
    const password = fixture.nativeElement.querySelector('#user-password') as HTMLInputElement;
    password.value = 'secret123';
    password.dispatchEvent(new Event('input'));
    const confirmPassword = fixture.nativeElement.querySelector('#user-confirm-password') as HTMLInputElement;
    confirmPassword.value = 'secret123';
    confirmPassword.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حفظ',
    ) as HTMLButtonElement;
    saveButton.click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).toContain('عمر خالد'));
  });

  it('opens the edit dialog prefilled for the current user and saves through PUT', async () => {
    const fixture = await createLoadedFixture();
    const editButton = fixture.nativeElement.querySelector(
      'button[aria-label="تعديل بياناتي"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    const firstName = fixture.nativeElement.querySelector('#user-first-name') as HTMLInputElement;
    expect(firstName.value).toBe('علي');
    expect(fixture.nativeElement.querySelector('#user-email')).toBeNull();

    firstName.value = 'علي الجديد';
    firstName.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حفظ',
    ) as HTMLButtonElement;
    saveButton.click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).toContain('علي الجديد'));
  });

  it('does not render an edit action for other users', async () => {
    const fixture = await createLoadedFixture();
    const editButtons = [...fixture.nativeElement.querySelectorAll('button')].filter(
      (button: HTMLButtonElement) => button.getAttribute('aria-label')?.startsWith('تعديل'),
    );
    expect(editButtons).toHaveLength(1);
  });

  it('does not render a delete action for the current user', async () => {
    const fixture = await createLoadedFixture();
    expect(fixture.nativeElement.querySelector('button[aria-label="حذف علي أحمد"]')).toBeNull();
    expect(
      fixture.nativeElement.querySelector('button[aria-label="حذف سارة محمد"]'),
    ).not.toBeNull();
  });

  it('deletes another user after confirming the dialog', async () => {
    const fixture = await createLoadedFixture();
    const deleteButton = fixture.nativeElement.querySelector(
      'button[aria-label="حذف سارة محمد"]',
    ) as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();

    expect(text(fixture)).toContain('هل أنت متأكد');

    const confirmButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حذف',
    ) as HTMLButtonElement;
    confirmButton.click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).not.toContain('سارة محمد'));
  });

  it('shows an inline retry state when the list fetch fails', async () => {
    const auth = TestBed.inject(AuthStore);
    (auth as unknown as { userSignal: { set: (v: unknown) => void } }).userSignal.set({
      userId: 'user-1',
      email: 'ali@goldstore.app',
      tenantId: 'tenant-1',
      tenantKey: 'test',
      roles: ['store_admin'],
      permissions: ['settings'],
    });
    const store = TestBed.inject(UsersStore);
    server.use(http.get(BASE, () => HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 })));
    await store.load();

    const fixture = TestBed.createComponent(UsersPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.loading()).toBe(false));
    fixture.detectChanges();

    expect(text(fixture)).toContain('تعذّر تحميل المستخدمين');
    expect(text(fixture)).toContain('إعادة المحاولة');
  });
});
