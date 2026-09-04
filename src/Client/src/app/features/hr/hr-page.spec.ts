import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { HrPage } from './hr-page';
import { HrStore } from './hr-store';

const BASE = apiUrl('/api/v1');
const EMPLOYEES = `${BASE}/employees`;
const PAYMENTS = `${BASE}/employees/salary-payments`;
const SUMMARY = `${BASE}/employees/salary-period-summary`;
const UNLINKED = `${BASE}/employees/unlinked-users`;
const ACCOUNTS = `${BASE}/finance/accounts`;

const EMPLOYEES_PAYLOAD = [
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
    lastPaymentDate: null,
    lastPaymentNet: null,
    isActive: true,
    createdAt: '2026-08-01T10:00:00',
  },
];

const PAYMENTS_PAGE = {
  items: [
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
      notes: 'راتب أغسطس',
      isOnSchedule: true,
    },
  ],
  totalCount: 1,
  pageNumber: 1,
  pageSize: 15,
  totalPages: 1,
};

describe('HrPage', () => {
  const server = setupServer(
    http.get(EMPLOYEES, () => HttpResponse.json(EMPLOYEES_PAYLOAD)),
    http.get(PAYMENTS, () => HttpResponse.json(PAYMENTS_PAGE)),
    http.get(SUMMARY, ({ request }) => {
      const url = new URL(request.url);
      return HttpResponse.json({
        employeeId: url.searchParams.get('employeeId'),
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
    http.get(UNLINKED, () => HttpResponse.json([{ id: 'user-1', fullName: 'سارة علي', email: 'sara@example.com' }])),
    http.get(ACCOUNTS, () =>
      HttpResponse.json([{ id: 'acc-1', name: 'الصندوق الرئيسي', currency: 'JOD', accountType: 'Cash', isActive: true }]),
    ),
    http.post(EMPLOYEES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('emp-new', { status: 201 });
    }),
    http.put(`${EMPLOYEES}/emp-1`, () => HttpResponse.json(null, { status: 200 })),
    http.post(`${EMPLOYEES}/emp-1/toggle-active`, () => HttpResponse.json(null, { status: 200 })),
    http.post(`${EMPLOYEES}/emp-1/pay-salary`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('pay-new', { status: 201 });
    }),
  );

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function createLoadedFixture(): Promise<ComponentFixture<HrPage>> {
    const store = TestBed.inject(HrStore);
    await store.loadEmployees();
    await store.loadSalaryPayments({ page: 1, pageSize: 15 });
    const fixture = TestBed.createComponent(HrPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<HrPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  function buttonByText(fixture: ComponentFixture<HrPage>, label: string): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')].find(
      (b: HTMLButtonElement) => b.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  it('renders the employees tab first with rows and salary', async () => {
    const fixture = await createLoadedFixture();

    expect(text(fixture)).toContain('الموظفون');
    expect(text(fixture)).toContain('أحمد خليل');
    expect(text(fixture)).toContain('موظف مبيعات');
    expect(text(fixture)).toContain('800');
    expect(text(fixture)).toContain('نشط');
  });

  it('switches to the payments tab and shows paged rows', async () => {
    const fixture = await createLoadedFixture();

    const paymentsTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (b: HTMLButtonElement) => b.textContent?.includes('سجل الرواتب'),
    ) as HTMLButtonElement;
    paymentsTab.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(text(fixture)).toContain('سجل دفع الرواتب');
    expect(text(fixture)).toContain('راتب أغسطس');
    expect(text(fixture)).toContain('الصندوق الرئيسي');
  });

  it('creates an employee through the dialog with the exact payload', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(EMPLOYEES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('emp-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    buttonByText(fixture, 'موظف جديد').click();
    fixture.detectChanges();
    await fixture.whenStable();

    const firstName = fixture.nativeElement.querySelector('#emp-firstName') as HTMLInputElement;
    firstName.value = 'أحمد';
    firstName.dispatchEvent(new Event('input'));
    const lastName = fixture.nativeElement.querySelector('#emp-lastName') as HTMLInputElement;
    lastName.value = 'خليل';
    lastName.dispatchEvent(new Event('input'));
    const salary = fixture.nativeElement.querySelector('#emp-salary') as HTMLInputElement;
    salary.value = '800';
    salary.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(fixture, 'إنشاء الموظف').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['firstName']).toBe('أحمد');
    expect(captured?.['lastName']).toBe('خليل');
    expect(captured?.['role']).toBe(4);
    expect(captured?.['salary']).toBe(800);
    expect(captured?.['currency']).toBe(1);
    expect(captured?.['salaryCycle']).toBe(3);
    expect(Object.keys(captured!)).not.toContain('tenantId');
  });

  it('toggles an employee active state', async () => {
    let toggled = false;
    server.use(
      http.post(`${EMPLOYEES}/emp-1/toggle-active`, () => {
        toggled = true;
        return HttpResponse.json(null, { status: 200 });
      }),
    );

    const fixture = await createLoadedFixture();
    const toggleButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (b: HTMLButtonElement) => b.title === 'تفعيل/إيقاف',
    ) as HTMLButtonElement;
    toggleButton.click();
    await vi.waitFor(() => expect(toggled).toBe(true));
  });

  it('pays salary through the dialog with the exact payload and period summary', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(`${EMPLOYEES}/emp-1/pay-salary`, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('pay-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    const payButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (b: HTMLButtonElement) => b.title === 'دفع راتب',
    ) as HTMLButtonElement;
    payButton.click();
    fixture.detectChanges();
    await fixture.whenStable();

    // Summary should be fetched automatically
    await vi.waitFor(() => expect(text(fixture)).toContain('المتبقي'));

    const amountInput = fixture.nativeElement.querySelector('#pay-amount') as HTMLInputElement;
    amountInput.value = '800';
    amountInput.dispatchEvent(new Event('input'));
    const accountSelect = fixture.nativeElement.querySelector('#pay-account') as HTMLSelectElement;
    accountSelect.value = 'acc-1';
    accountSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'تأكيد الدفع').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['accountId']).toBe('acc-1');
    expect(captured?.['amount']).toBe(800);
    expect(captured?.['paymentDate']).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(Object.keys(captured!)).not.toContain('tenantId');
  });

  it('applies the salary payments employeeName filter', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(PAYMENTS, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(PAYMENTS_PAGE);
      }),
    );

    const fixture = await createLoadedFixture();
    // Switch to payments tab to expose filter
    const paymentsTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (b: HTMLButtonElement) => b.textContent?.includes('سجل الرواتب'),
    ) as HTMLButtonElement;
    paymentsTab.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const input = fixture.nativeElement.querySelector('#pay-employee-filter') as HTMLInputElement;
    input.value = 'أحمد';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(fixture, 'تصفية').click();
    await vi.waitFor(() => expect((captured as any)?.get('employeeName')).toBe('أحمد'));
  });
});
