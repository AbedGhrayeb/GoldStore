import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type { CreateExpenseInput, CreateExpenseCategoryInput } from './expenses-api.service';
import { ExpensesStore } from './expenses-store';

const BASE = apiUrl('/api/v1');
const EXPENSES = `${BASE}/expenses`;
const CATEGORIES = `${BASE}/expenses/categories`;
const KPIS = `${BASE}/expenses/kpis`;
const ACCOUNTS = `${BASE}/finance/accounts`;

function expenseInput(): CreateExpenseInput {
  return {
    expenseDate: '2026-08-21',
    categoryId: 'cat-1',
    description: 'إيجار المحل',
    amount: 250,
    accountId: 'acc-1',
  };
}

function categoryInput(): CreateExpenseCategoryInput {
  return { name: 'إيجار' };
}

describe('ExpensesStore', () => {
  let store: ExpensesStore;
  let toasts: ToastStore;

  const server = setupServer(
    http.get(EXPENSES, ({ request }) => {
      const url = new URL(request.url);
      const categoryId = url.searchParams.get('categoryId');
      const accountName = url.searchParams.get('accountName');
      const items: unknown[] =
        categoryId === 'cat-1' || accountName === 'الصندوق'
          ? [
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
    http.get(KPIS, () =>
      HttpResponse.json({
        todayTotals: [{ currency: 'JOD', symbol: 'د.أ', amount: 250 }],
        monthTotals: [{ currency: 'JOD', symbol: 'د.أ', amount: 1200 }],
        topCategoryName: 'إيجار',
        topCategoryAmounts: [{ currency: 'JOD', symbol: 'د.أ', amount: 800 }],
      }),
    ),
    http.get(CATEGORIES, ({ request }) => {
      const url = new URL(request.url);
      expect(url.searchParams.get('activeOnly')).toBe('false');
      return HttpResponse.json([
        { id: 'cat-1', name: 'إيجار', isActive: true },
        { id: 'cat-2', name: 'كهرباء', isActive: true },
      ]);
    }),
    http.get(ACCOUNTS, () =>
      HttpResponse.json([
        { id: 'acc-1', name: 'الصندوق الرئيسي', currency: 'JOD', accountType: 'Cash', isActive: true },
      ]),
    ),
    http.post(EXPENSES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'accountId',
        'amount',
        'categoryId',
        'description',
        'expenseDate',
      ]);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('exp-new', { status: 201 });
    }),
    http.put(`${EXPENSES}/exp-1`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual([
        'accountId',
        'amount',
        'categoryId',
        'description',
        'expenseDate',
      ]);
      expect(body['amount']).toBe(300);
      return HttpResponse.json(null, { status: 200 });
    }),
    http.delete(`${EXPENSES}/exp-1`, () => HttpResponse.json(null, { status: 200 })),
    http.post(CATEGORIES, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(Object.keys(body)).toEqual(['name']);
      expect(Object.keys(body)).not.toContain('tenantId');
      return HttpResponse.json('cat-new', { status: 201 });
    }),
    http.put(`${CATEGORIES}/cat-1`, async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      expect(body['name']).toBe('إيجار محدث');
      return HttpResponse.json(null, { status: 200 });
    }),
    http.delete(`${CATEGORIES}/cat-1`, () => HttpResponse.json(null, { status: 200 })),
  );

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    store = TestBed.inject(ExpensesStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads the paged expenses with accountName and categoryId filters', async () => {
    await store.loadExpenses({ page: 1, pageSize: 15, accountName: 'الصندوق' });

    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
    expect((store.page() as any)?.items?.[0]?.categoryName).toBe('إيجار');
    expect(store.page()?.totalCount).toBe(1);

    await store.loadExpenses({ page: 1, pageSize: 15, categoryId: 'cat-1' });
    expect((store.page() as any)?.items?.[0]?.id).toBe('exp-1');

    await store.loadExpenses({ page: 1, pageSize: 15 });
    expect((store.page() as any)?.items).toHaveLength(0);
  });

  it('surfaces a paged load failure without a toast', async () => {
    server.use(http.get(EXPENSES, () => HttpResponse.json({ detail: 'خطأ داخلي' }, { status: 500 })));

    await store.loadExpenses({ page: 1 });

    expect(store.page()).toBeNull();
    expect(store.error()?.detail).toBe('خطأ داخلي');
    expect(toasts.toasts().length).toBe(0);
  });

  it('loads the KPIs', async () => {
    await store.loadKpis();

    expect(store.kpisLoading()).toBe(false);
    expect(store.kpis()?.topCategoryName).toBe('إيجار');
    expect((store.kpis() as any)?.todayTotals[0]?.amount).toBe(250);
  });

  it('loads the categories (activeOnly=false) and exposes them', async () => {
    await store.loadCategories();

    expect(store.categoriesLoading()).toBe(false);
    expect(store.categoriesError()).toBeNull();
    expect(store.categories()).toHaveLength(2);
    expect(store.categories()?.[0]?.name).toBe('إيجار');
  });

  it('creates an expense with the exact request keys (no tenantId) and toasts', async () => {
    const ok = await store.createExpense(expenseInput());

    expect(ok).toBe(true);
    expect(toasts.toasts().some((t) => t.type === 'success')).toBe(true);
  });

  it('updates an expense with the exact request keys', async () => {
    const ok = await store.updateExpense('exp-1', { ...expenseInput(), amount: 300 });

    expect(ok).toBe(true);
  });

  it('deletes an expense (financial reversal) and toasts', async () => {
    const ok = await store.deleteExpense('exp-1');

    expect(ok).toBe(true);
    expect(toasts.toasts().some((t) => t.type === 'success')).toBe(true);
  });

  it('creates a category with the exact request keys (no tenantId)', async () => {
    const ok = await store.createCategory(categoryInput());

    expect(ok).toBe(true);
  });

  it('updates a category', async () => {
    const ok = await store.updateCategory('cat-1', { name: 'إيجار محدث' });

    expect(ok).toBe(true);
  });

  it('deletes an unused category', async () => {
    const ok = await store.deleteCategory('cat-1');

    expect(ok).toBe(true);
  });

  it('surfaces a create failure with server validation, no success toast', async () => {
    server.use(
      http.post(EXPENSES, () =>
        HttpResponse.json(
          { title: 'طلب غير صالح', errors: { amount: ['المبلغ يجب أن يكون أكبر من صفر.'] } },
          { status: 400 },
        ),
      ),
    );

    const ok = await store.createExpense({ ...expenseInput(), amount: 0 });

    expect(ok).toBe(false);
    expect(store.saveError()?.validation?.['amount']?.[0]).toBe('المبلغ يجب أن يكون أكبر من صفر.');
    expect(toasts.toasts().some((t) => t.type === 'success')).toBe(false);
  });

  it('surfaces a category conflict when deleting a referenced category', async () => {
    server.use(
      http.delete(`${CATEGORIES}/cat-1`, () =>
        HttpResponse.json({ detail: 'التصنيف مرتبط بمصروفات' }, { status: 409 }),
      ),
    );

    const ok = await store.deleteCategory('cat-1');

    expect(ok).toBe(false);
    expect(store.saveError()?.detail).toBe('التصنيف مرتبط بمصروفات');
  });

  it('degrades gracefully when the accounts endpoint is gated behind finance', async () => {
    server.use(http.get(ACCOUNTS, () => HttpResponse.json({ detail: 'غير مصرح' }, { status: 403 })));

    await store.ensureAccounts();

    expect(store.accounts()).toHaveLength(0);
    expect(store.accountsError()).toContain('المالية');
    expect(toasts.toasts().length).toBe(0);
  });
});
