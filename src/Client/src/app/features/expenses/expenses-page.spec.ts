import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import { ExpensesPage } from './expenses-page';
import { ExpensesStore } from './expenses-store';

const BASE = apiUrl('/api/v1');
const EXPENSES = `${BASE}/expenses`;
const CATEGORIES = `${BASE}/expenses/categories`;
const KPIS = `${BASE}/expenses/kpis`;
const ACCOUNTS = `${BASE}/finance/accounts`;

const EXPENSES_PAGE = {
  items: [
    {
      id: 'exp-1',
      expenseDate: '2026-08-21',
      categoryId: 'cat-1',
      categoryName: 'إيجار',
      description: 'إيجار المحل',
      amount: 250,
      currency: 'JOD',
      currencySymbol: 'د.أ',
      accountId: 'acc-1',
      accountName: 'الصندوق الرئيسي',
    },
  ],
  totalCount: 1,
  pageNumber: 1,
  pageSize: 15,
  totalPages: 1,
};

const KPIS_PAYLOAD = {
  todayTotals: [{ currency: 'JOD', symbol: 'د.أ', amount: 250 }],
  monthTotals: [{ currency: 'JOD', symbol: 'د.أ', amount: 1200 }],
  topCategoryName: 'إيجار',
  topCategoryAmounts: [{ currency: 'JOD', symbol: 'د.أ', amount: 800 }],
};

const CATEGORIES_PAYLOAD = [
  { id: 'cat-1', name: 'إيجار', isActive: true },
  { id: 'cat-2', name: 'كهرباء', isActive: true },
];

describe('ExpensesPage', () => {
  const server = setupServer(
    http.get(EXPENSES, () => HttpResponse.json(EXPENSES_PAGE)),
    http.get(KPIS, () => HttpResponse.json(KPIS_PAYLOAD)),
    http.get(CATEGORIES, () => HttpResponse.json(CATEGORIES_PAYLOAD)),
    http.get(ACCOUNTS, () =>
      HttpResponse.json([
        { id: 'acc-1', name: 'الصندوق الرئيسي', currency: 'JOD', accountType: 'Cash', isActive: true },
      ]),
    ),
    http.post(EXPENSES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('exp-new', { status: 201 });
    }),
    http.put(`${EXPENSES}/exp-1`, () => HttpResponse.json(null, { status: 200 })),
    http.delete(`${EXPENSES}/exp-1`, () => HttpResponse.json(null, { status: 200 })),
    http.post(CATEGORIES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('cat-new', { status: 201 });
    }),
    http.delete(`${CATEGORIES}/cat-1`, () => HttpResponse.json(null, { status: 200 })),
  );

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function createLoadedFixture(): Promise<ComponentFixture<ExpensesPage>> {
    const store = TestBed.inject(ExpensesStore);
    await store.loadExpenses({ page: 1, pageSize: 15 });
    await store.loadKpis();
    await store.loadCategories();
    const fixture = TestBed.createComponent(ExpensesPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<ExpensesPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  function buttonByText(fixture: ComponentFixture<ExpensesPage>, label: string): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')].find(
      (b: HTMLButtonElement) => b.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  it('renders KPI cards and the expenses tab by default', async () => {
    const fixture = await createLoadedFixture();

    expect(text(fixture)).toContain('المصروفات');
    expect(text(fixture)).toContain('مصروفات اليوم');
    expect(text(fixture)).toContain('مصروفات الشهر');
    expect(text(fixture)).toContain('أكثر تصنيف');
    expect(text(fixture)).toContain('إيجار');
    expect(text(fixture)).toContain('إيجار المحل');
    expect(text(fixture)).toContain('250.000');
    expect(text(fixture)).not.toContain('كهرباء');
  });

  it('switches to the categories tab and shows the category rows', async () => {
    const fixture = await createLoadedFixture();

    const categoriesTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (b: HTMLButtonElement) => b.textContent?.includes('التصنيفات'),
    ) as HTMLButtonElement;
    categoriesTab.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const content = text(fixture);
    expect(content).toContain('تصنيفات المصروفات');
    expect(content).toContain('كهرباء');
    expect(content).toContain('إيجار');
  });

  it('creates an expense through the dialog with the exact payload', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(EXPENSES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('exp-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    await TestBed.inject(ExpensesStore).ensureAccounts();
    buttonByText(fixture, 'مصروف جديد').click();
    fixture.detectChanges();
    await fixture.whenStable();

    // Fill amount + select account
    const amountInput = fixture.nativeElement.querySelector('#expense-amount') as HTMLInputElement;
    amountInput.value = '250';
    amountInput.dispatchEvent(new Event('input'));

    const accountSelect = fixture.nativeElement.querySelector('#expense-account') as HTMLSelectElement;
    accountSelect.value = 'acc-1';
    accountSelect.dispatchEvent(new Event('change'));

    const categorySelect = fixture.nativeElement.querySelector('#expense-category') as HTMLSelectElement;
    categorySelect.value = 'cat-1';
    categorySelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'تسجيل المصروف').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['amount']).toBe(250);
    expect(captured?.['accountId']).toBe('acc-1');
    expect(captured?.['categoryId']).toBe('cat-1');
    expect(captured?.['expenseDate']).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(Object.keys(captured!)).not.toContain('tenantId');
  });

  it('creates a category through the dialog with the exact payload', async () => {
    let captured: Record<string, unknown> | null = null;
    server.use(
      http.post(CATEGORIES, async ({ request }) => {
        captured = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('cat-new', { status: 201 });
      }),
    );

    const fixture = await createLoadedFixture();
    // Switch to categories to ensure visibility, but dialog is global
    const categoriesTab = [...fixture.nativeElement.querySelectorAll('[role="tab"]')].find(
      (b: HTMLButtonElement) => b.textContent?.includes('التصنيفات'),
    ) as HTMLButtonElement;
    categoriesTab.click();
    fixture.detectChanges();
    await fixture.whenStable();

    buttonByText(fixture, 'تصنيف جديد').click();
    fixture.detectChanges();
    await fixture.whenStable();

    const nameInput = fixture.nativeElement.querySelector('#category-name') as HTMLInputElement;
    nameInput.value = 'إيجار';
    nameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(fixture, 'إنشاء التصنيف').click();
    await vi.waitFor(() => expect(captured).not.toBeNull());

    expect(captured?.['name']).toBe('إيجار');
    expect(Object.keys(captured!)).not.toContain('tenantId');
  });

  it('applies the category filter', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(EXPENSES, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(EXPENSES_PAGE);
      }),
    );

    const fixture = await createLoadedFixture();
    const select = fixture.nativeElement.querySelector('#expense-category-filter') as HTMLSelectElement;
    select.value = 'cat-1';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    buttonByText(fixture, 'تصفية').click();
    await vi.waitFor(() => expect((captured as any)?.get('categoryId')).toBe('cat-1'));
  });

  it('applies the accountName filter', async () => {
    let captured: URLSearchParams | null = null;
    server.use(
      http.get(EXPENSES, ({ request }) => {
        captured = new URL(request.url).searchParams;
        return HttpResponse.json(EXPENSES_PAGE);
      }),
    );

    const fixture = await createLoadedFixture();
    const input = fixture.nativeElement.querySelector('#expense-account-filter') as HTMLInputElement;
    input.value = 'الصندوق';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(fixture, 'تصفية').click();
    await vi.waitFor(() => expect((captured as any)?.get('accountName')).toBe('الصندوق'));
  });

  it('deletes an expense through the confirmation card with financial reversal', async () => {
    let deleted = false;
    server.use(
      http.delete(`${EXPENSES}/exp-1`, () => {
        deleted = true;
        return HttpResponse.json(null, { status: 200 });
      }),
    );

    const fixture = await createLoadedFixture();
    const deleteButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (b: HTMLButtonElement) => b.title === 'حذف',
    ) as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(text(fixture)).toContain('هل أنت متأكد من حذف هذا المصروف');
    buttonByText(fixture, 'حذف المصروف').click();
    await vi.waitFor(() => expect(deleted).toBe(true));
  });
});
