import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import type { CategoryResponse } from './catalog-api.service';
import { CategoriesPage } from './categories-page';
import { CatalogStore } from './catalog-store';

const CATEGORIES: CategoryResponse[] = [
  {
    id: 'cat-1',
    name: 'خواتم',
    description: 'خواتم ذهبية',
    parentCategoryId: null,
    parentCategoryName: null,
    isActive: true,
  },
  {
    id: 'cat-2',
    name: 'أطقم',
    description: null,
    parentCategoryId: null,
    parentCategoryName: null,
    isActive: true,
  },
  {
    id: 'cat-3',
    name: 'خواتم رجالي',
    description: null,
    parentCategoryId: 'cat-1',
    parentCategoryName: 'خواتم',
    isActive: false,
  },
];

const BASE = apiUrl('/api/v1/categories');

describe('CategoriesPage', () => {
  let categories: CategoryResponse[];

  const server = setupServer(
    http.get(BASE, () => HttpResponse.json(categories)),
    http.post(BASE, async ({ request }) => {
      const body = (await request.json()) as { name: string; parentCategoryId: string | null };
      categories = [
        ...categories,
        {
          id: 'cat-new',
          name: body.name,
          description: null,
          parentCategoryId: body.parentCategoryId,
          parentCategoryName: body.parentCategoryId ? 'خواتم' : null,
          isActive: true,
        },
      ];
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

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    categories = [CATEGORIES[0], CATEGORIES[1], CATEGORIES[2]];
  });
  afterAll(() => server.close());

  beforeEach(() => {
    categories = [CATEGORIES[0], CATEGORIES[1], CATEGORIES[2]];
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function createLoadedFixture(): Promise<ComponentFixture<CategoriesPage>> {
    const store = TestBed.inject(CatalogStore);
    await store.load();
    const fixture = TestBed.createComponent(CategoriesPage);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<CategoriesPage>): string {
    return fixture.nativeElement.textContent as string;
  }

  it('renders the tree rows with parent names and status badges', async () => {
    const fixture = await createLoadedFixture();
    const content = text(fixture);

    expect(content).toContain('الكتالوج');
    expect(content).toContain('خواتم');
    expect(content).toContain('أطقم');
    expect(content).toContain('خواتم رجالي');
    expect(content).toContain('خواتم ذهبية');
    expect(content).toContain('نشط');
    expect(content).toContain('موقوف');
  });

  it('creates a category through the form dialog and refreshes the table', async () => {
    const fixture = await createLoadedFixture();
    const addButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.includes('إضافة تصنيف'),
    ) as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('#category-name') as HTMLInputElement;
    input.value = 'سلاسل';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حفظ',
    ) as HTMLButtonElement;
    saveButton.click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).toContain('سلاسل'));
  });

  it('opens the edit dialog prefilled and saves through PUT', async () => {
    const fixture = await createLoadedFixture();
    const editButton = fixture.nativeElement.querySelector(
      'button[aria-label="تعديل خواتم"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('#category-name') as HTMLInputElement;
    expect(input.value).toBe('خواتم');

    input.value = 'خواتم نسائية';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حفظ',
    ) as HTMLButtonElement;
    saveButton.click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).toContain('خواتم نسائية'));
  });

  it('toggles a category through the row action', async () => {
    const fixture = await createLoadedFixture();
    const toggleButton = fixture.nativeElement.querySelector(
      'button[aria-label="إيقاف أطقم"]',
    ) as HTMLButtonElement;
    toggleButton.click();
    fixture.detectChanges();
    await vi.waitFor(() =>
      expect(fixture.nativeElement.querySelector('button[aria-label="تفعيل أطقم"]')).not.toBeNull(),
    );
    fixture.detectChanges();

    expect(text(fixture)).toContain('موقوف');
  });

  it('deletes a category after confirming the dialog', async () => {
    const fixture = await createLoadedFixture();
    const deleteButton = fixture.nativeElement.querySelector(
      'button[aria-label="حذف أطقم"]',
    ) as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();

    expect(text(fixture)).toContain('هل أنت متأكد');

    const confirmButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حذف',
    ) as HTMLButtonElement;
    confirmButton.click();
    fixture.detectChanges();
    await vi.waitFor(() => expect(text(fixture)).not.toContain('أطقم'));
  });

  it('shows an inline retry state when the list fetch fails', async () => {
    const store = TestBed.inject(CatalogStore);
    server.use(http.get(BASE, () => HttpResponse.json({ detail: 'خطأ خادم' }, { status: 500 })));
    await store.load();

    const fixture = TestBed.createComponent(CategoriesPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(store.loading()).toBe(false));
    fixture.detectChanges();

    expect(text(fixture)).toContain('تعذّر تحميل التصنيفات');
    expect(text(fixture)).toContain('إعادة المحاولة');
  });
});
