import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type { CreateEmployeeInput, PaySalaryInput } from './hr-api.service';
import { HrStore } from './hr-store';

const BASE = apiUrl('/api/v1');
const EMPLOYEES = `${BASE}/employees`;
const PAYMENTS = `${BASE}/employees/salary-payments`;
const SUMMARY = `${BASE}/employees/salary-period-summary`;
const UNLINKED = `${BASE}/employees/unlinked-users`;
const ACCOUNTS = `${BASE}/finance/accounts`;

function createInput(): CreateEmployeeInput {
  return {
    firstName: 'أحمد',
    lastName: 'خليل',
    role: 4,
    salary: 800,
    currency: 1,
    salaryCycle: 3,
    connectToUser: false,
    existingUserId: null,
    newUserEmail: null,
    newUserPassword: null,
  };
}

describe('HrStore', () => {
  let store: HrStore;
  let toasts: ToastStore;

  const server = setupServer(
    http.get(EMPLOYEES, () =>
      HttpResponse.json([
        {
          id: 'emp-1',
          firstName: 'أحمد',
          lastName: 'خليل',
          fullName: 'أحمد خليل',
          role: 4,
          roleName: 'موظف مبيعات',
          salary: 800,
          currency: 1,
          currencySymbol: 'د.أ',
          salaryCycle: 3,
          salaryCycleName: 'شهري',
          userId: null,
          userEmail: null,
          lastPaymentDate: '2026-08-10',
          lastPaymentNet: 750,
          isActive: true,
          createdAt: '2026-08-01T10:00:00',
        },
      ]),
    ),
    http.get(`${EMPLOYEES}/emp-1`, () =>
      HttpResponse.json({
        id: 'emp-1',
        firstName: 'أحمد',
        lastName: 'خليل',
        fullName: 'أحمد خليل',
        role: 4,
        roleName: 'موظف مبيعات',
        salary: 800,
        currency: 1,
        currencySymbol: 'د.أ',
        salaryCycle: 3,
        salaryCycleName: 'شهري',
        userId: null,
        userEmail: null,
        lastPaymentDate: null,
        lastPaymentNet: null,
        isActive: true,
        createdAt: '2026-08-01T10:00:00',
      }),
    ),
    http.post(EMPLOYEES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'connectToUser',
        'currency',
        'existingUserId',
        'firstName',
        'lastName',
        'newUserEmail',
        'newUserPassword',
        'role',
        'salary',
        'salaryCycle',
      ]);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('emp-new', { status: 201 });
    }),
    http.put(`${EMPLOYEES}/emp-1`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'currency',
        'firstName',
        'isActive',
        'lastName',
        'role',
        'salary',
        'salaryCycle',
      ]);
      return HttpResponse.json(null, { status: 200 });
    }),
    http.post(`${EMPLOYEES}/emp-1/toggle-active`, () => HttpResponse.json(null, { status: 200 })),
    http.post(`${EMPLOYEES}/emp-1/pay-salary`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual(['accountId', 'amount', 'notes', 'paymentDate']);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('pay-new', { status: 201 });
    }),
    http.get(PAYMENTS, ({ request }) => {
      const url = new URL(request.url);
      const employeeName = url.searchParams.get('employeeName');
      const items: unknown[] =
        employeeName === 'أحمد'
          ? [
              {
                id: 'pay-1',
                employeeId: 'emp-1',
                employeeName: 'أحمد خليل',
                paymentDate: '2026-08-15',
                scheduledDate: '2026-08-15',
                salaryAmount: 800,
                discountAmount: 0,
                amount: 800,
                accountName: 'الصندوق الرئيسي',
                notes: null,
                isOnSchedule: true,
              },
            ]
          : [];
      const page = Number(url.searchParams.get('page') ?? 1);
      const pageSize = Number(url.searchParams.get('pageSize') ?? 15);
      return HttpResponse.json({
        pageNumber: page,
        pageSize,
        totalCount: items.length,
        totalPages: Math.ceil(items.length / pageSize),
        items,
        hasPreviousPage: false,
        hasNextPage: false,
      });
    }),
    http.get(SUMMARY, ({ request }) => {
      const url = new URL(request.url);
      expect(url.searchParams.get('employeeId')).toBe('emp-1');
      return HttpResponse.json({
        employeeId: 'emp-1',
        employeeName: 'أحمد خليل',
        salary: 800,
        salaryCycle: 3,
        salaryCycleName: 'شهري',
        paymentDate: url.searchParams.get('paymentDate'),
        scheduledDate: url.searchParams.get('paymentDate'),
        dayOff: 0,
        discountAmount: 0,
        netAmount: 800,
        alreadyPaid: 0,
        remaining: 800,
        isFullyPaid: false,
      });
    }),
    http.get(UNLINKED, () =>
      HttpResponse.json([{ id: 'user-1', fullName: 'سارة علي', email: 'sara@example.com' }]),
    ),
    http.get(ACCOUNTS, () =>
      HttpResponse.json([
        { id: 'acc-1', name: 'الصندوق الرئيسي', currency: 'JOD', accountType: 'Cash', isActive: true },
      ]),
    ),
  );

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    store = TestBed.inject(HrStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads the employees list', async () => {
    await store.loadEmployees();

    expect(store.employeesLoading()).toBe(false);
    expect(store.employeesError()).toBeNull();
    expect(store.employees()).toHaveLength(1);
    expect(store.employees()?.[0]?.fullName).toBe('أحمد خليل');
  });

  it('loads an employee by id', async () => {
    await store.loadEmployee('emp-1');

    expect(store.employeeLoading()).toBe(false);
    expect(store.employee()?.id).toBe('emp-1');
    expect(store.employee()?.roleName).toBe('موظف مبيعات');
  });

  it('creates an employee with the exact request keys (no tenantId) and toasts', async () => {
    const ok = await store.createEmployee(createInput());

    expect(ok).toBe(true);
    expect(toasts.toasts().some((t) => t.type === 'success')).toBe(true);
  });

  it('creates an employee linked to an existing user', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(EMPLOYEES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('emp-new', { status: 201 });
      }),
    );

    const ok = await store.createEmployee({
      ...createInput(),
      connectToUser: true,
      existingUserId: 'user-1',
    });

    expect(ok).toBe(true);
    expect(captured?.['connectToUser']).toBe(true);
    expect(captured?.['existingUserId']).toBe('user-1');
  });

  it('updates an employee with the exact request keys', async () => {
    const ok = await store.updateEmployee('emp-1', {
      firstName: 'أحمد',
      lastName: 'خليل',
      role: 4,
      salary: 900,
      currency: 1,
      salaryCycle: 3,
      isActive: true,
    });

    expect(ok).toBe(true);
  });

  it('toggles an employee active state', async () => {
    const ok = await store.toggleActive('emp-1');

    expect(ok).toBe(true);
    expect(toasts.toasts().some((t) => t.type === 'success')).toBe(true);
  });

  it('pays salary with the exact request keys (no tenantId) and ledger hit', async () => {
    const input: PaySalaryInput = {
      accountId: 'acc-1',
      amount: 800,
      paymentDate: '2026-08-15',
      notes: null,
    };
    const ok = await store.paySalary('emp-1', input);

    expect(ok).toBe(true);
  });

  it('loads paged salary payments with employeeName filter', async () => {
    await store.loadSalaryPayments({ page: 1, pageSize: 15, employeeName: 'أحمد' });

    expect(store.salaryPaymentsError()).toBeNull();
    expect(store.salaryPayments()?.items?.[0]?.employeeName).toBe('أحمد خليل');
    expect(store.salaryPayments()?.totalCount).toBe(1);

    await store.loadSalaryPayments({ page: 1, pageSize: 15, employeeName: 'غير موجود' });
    expect(store.salaryPayments()?.items).toHaveLength(0);
  });

  it('loads the salary period summary', async () => {
    await store.loadPeriodSummary('emp-1', '2026-08-15');

    expect(store.periodSummaryLoading()).toBe(false);
    expect(store.periodSummary()?.netAmount).toBe(800);
    expect(store.periodSummary()?.remaining).toBe(800);
    expect(store.periodSummary()?.isFullyPaid).toBe(false);
  });

  it('loads unlinked users', async () => {
    await store.ensureUnlinkedUsers();

    expect(store.unlinkedUsers()[0]?.email).toBe('sara@example.com');
  });

  it('surfaces a create validation failure, no success toast', async () => {
    server.use(
      http.post(EMPLOYEES, () =>
        HttpResponse.json(
          { title: 'طلب غير صالح', errors: { salary: ['الراتب يجب أن يكون أكبر من صفر.'] } },
          { status: 400 },
        ),
      ),
    );

    const ok = await store.createEmployee({ ...createInput(), salary: 0 });

    expect(ok).toBe(false);
    expect(store.saveError()?.validation?.['salary']?.[0]).toBe('الراتب يجب أن يكون أكبر من صفر.');
    expect(toasts.toasts().some((t) => t.type === 'success')).toBe(false);
  });

  it('surfaces insufficient-balance conflict when paying salary', async () => {
    server.use(
      http.post(`${EMPLOYEES}/emp-1/pay-salary`, () =>
        HttpResponse.json({ detail: 'الرصيد غير كاف' }, { status: 409 }),
      ),
    );

    const ok = await store.paySalary('emp-1', {
      accountId: 'acc-1',
      amount: 5000,
      paymentDate: '2026-08-15',
      notes: null,
    });

    expect(ok).toBe(false);
    expect(store.saveError()?.detail).toBe('الرصيد غير كاف');
  });

  it('degrades gracefully when accounts endpoint is gated behind finance', async () => {
    server.use(http.get(ACCOUNTS, () => HttpResponse.json({ detail: 'غير مصرح' }, { status: 403 })));

    await store.ensureAccounts();

    expect(store.accounts()).toHaveLength(0);
    expect(store.accountsError()).toContain('المالية');
  });
});
