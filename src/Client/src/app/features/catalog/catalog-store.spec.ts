import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { ToastStore } from '../../core/toast/toast-store';
import { apiUrl } from '../../testing/api-url';
import type { CategoryResponse } from './catalog-api.service';
import { CatalogStore } from './catalog-store';

const CATEGORY_1: CategoryResponse = {
  id: 'cat-1',
  name: 'خواتم',
  description: 'خواتم ذهبية',
  parentCategoryId: null,
  parentCategoryName: null,
  isActive: true,
};

const CATEGORY_2: CategoryResponse = {
  id: 'cat-2',
  name: 'أطقم',
  description: null,
  parentCategoryId: null,
  parentCategoryName: null,
  isActive: true,
};

const CATEGORY_3: CategoryResponse = {
  id: 'cat-3',
  name: 'خواتم رجالي',
  description: null,
  parentCategoryId: 'cat-1',
  parentCategoryName: 'خواتم',
  isActive: false,
};

const BASE = apiUrl('/api/v1/categories');

describe('CatalogStore', () => {
  let store: CatalogStore;
  let toasts: ToastStore;
  let categories: CategoryResponse[];

  const server = setupServer(
    http.get(BASE, () => HttpResponse.json(categories)),
    http.post(BASE, async ({ request }) => {
      const body = (await request.json()) as { name: string; parentCategoryId: string | null };
      const newCategory: CategoryResponse = {
        id: 'cat-new',
        name: body.name,
        description: null,
        parentCategoryId: body.parentCategoryId,
        parentCategoryName: null,
        isActive: true,
      };
      categories = [...categories, newCategory];
      return HttpResponse.json('cat-new', { status: 201 });
    }),
    http.put(`${BASE}/:id`, async ({ request, params }) => {
      const body = (await request.json()) as { name: string; isActive: boolean };
      categories = categories.map((category) =>
        category.id === params['id']
          ? { ...category, name: body.name, isActive: body.isActive }
          : category,
      );
      return HttpResponse.json({});
    }),
    http.post(`${BASE}/:id/toggle-active`, ({ params }) => {
      categories = categories.map((category) =>
        category.id === params['id'] ? { ...category, isActive: !category.isActive } : category,
      );
      return HttpResponse.json({});
    }),
    http.delete(`${BASE}/:id`, ({ params }) => {
      categories = categories.filter((category) => category.id !== params['id']);
      return HttpResponse.json({});
    }),
  );

  beforeEach(() => {
    categories = [CATEGORY_1, CATEGORY_2, CATEGORY_3];
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    store = TestBed.inject(CatalogStore);
    toasts = TestBed.inject(ToastStore);
  });

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('loads the category list into the store', async () => {
    await store.load();

    expect(store.categories()).toEqual([CATEGORY_1, CATEGORY_2, CATEGORY_3]);
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('keeps the previous list and reports the error when a reload fails', async () => {
    await store.load();
    server.use(http.get(BASE, () => HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 })));

    await store.load();

    expect(store.error()).not.toBeNull();
    expect(store.error()?.detail).toBe('خطأ خادم');
    expect(store.categories()).toEqual([CATEGORY_1, CATEGORY_2, CATEGORY_3]);
  });

  it('loads the list only once for concurrent ensureLoaded calls', async () => {
    await Promise.all([store.ensureLoaded(), store.ensureLoaded(), store.ensureLoaded()]);

    expect(store.categories()).toHaveLength(3);
  });

  it('creates a category, posts the input and reloads the list', async () => {
    const ok = await store.createCategory({
      name: 'سلاسل',
      description: null,
      parentCategoryId: 'cat-1',
      isActive: true,
      weightInGrams: 12.5,
      karat: 21,
    });

    expect(ok).toBe(true);
    expect(store.categories()).toHaveLength(4);
    expect(store.categories()?.find((category) => category.id === 'cat-new')?.name).toBe('سلاسل');
    expect(toasts.toasts().some((toast) => toast.type === 'success')).toBe(true);
  });

  it('surfaces a duplicate-name conflict inline without reloading', async () => {
    await store.load();
    server.use(
      http.post(BASE, () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
            title: 'Conflict',
            status: 409,
            detail: 'اسم الفئة موجود بالفعل على هذا المستوى',
            errorCode: 'Categories.DuplicateName',
          },
          { status: 409 },
        ),
      ),
    );

    const ok = await store.createCategory({
      name: 'خواتم',
      description: null,
      parentCategoryId: null,
      isActive: true,
      weightInGrams: 8,
      karat: 18,
    });

    expect(ok).toBe(false);
    expect(store.saveError()?.detail).toBe('اسم الفئة موجود بالفعل على هذا المستوى');
    expect(store.categories()).toHaveLength(3);
  });

  it('updates a category through PUT and reloads the list', async () => {
    const ok = await store.updateCategory('cat-1', {
      name: 'خواتم نسائية',
      description: null,
      parentCategoryId: null,
      isActive: false,
      weightInGrams: 10,
      karat: 21,
    });

    expect(ok).toBe(true);
    const updated = store.categories()?.find((category) => category.id === 'cat-1');
    expect(updated?.name).toBe('خواتم نسائية');
    expect(updated?.isActive).toBe(false);
  });

  it('toggles a category active state and reloads', async () => {
    await store.toggleActive('cat-2');

    expect(store.categories()?.find((category) => category.id === 'cat-2')?.isActive).toBe(false);
    expect(store.mutatingId()).toBeNull();
  });

  it('deletes a category and reloads', async () => {
    await store.deleteCategory('cat-3');

    expect(store.categories()?.some((category) => category.id === 'cat-3')).toBe(false);
    expect(store.categories()).toHaveLength(2);
  });
});
