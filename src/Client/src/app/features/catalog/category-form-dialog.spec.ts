import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

import { apiUrl } from '../../testing/api-url';
import type { CategoryResponse } from './catalog-api.service';
import { CategoryFormDialog, type CategoryFormMode } from './category-form-dialog';
import type { CategoryParentOption } from './catalog.model';

const CATEGORY: CategoryResponse = {
  id: 'cat-1',
  name: 'خواتم',
  description: 'خواتم ذهبية',
  parentCategoryId: null,
  parentCategoryName: null,
  isActive: true,
};

const BASE = apiUrl('/api/v1/categories');

describe('CategoryFormDialog', () => {
  let lastCreateBody: unknown;
  let lastUpdateBody: unknown;
  let lastUpdateId: string | null;

  const server = setupServer(
    http.post(BASE, async ({ request }) => {
      lastCreateBody = await request.json();
      return HttpResponse.json('cat-new', { status: 201 });
    }),
    http.put(`${BASE}/:id`, async ({ request, params }) => {
      lastUpdateId = params['id'] as string;
      lastUpdateBody = await request.json();
      return HttpResponse.json({});
    }),
  );

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    server.resetHandlers();
    lastCreateBody = null;
    lastUpdateBody = null;
    lastUpdateId = null;
  });
  afterAll(() => server.close());

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  async function createFixture(
    mode: CategoryFormMode = 'create',
    category: CategoryResponse | null = null,
    parentOptions: CategoryParentOption[] = [],
  ): Promise<ComponentFixture<CategoryFormDialog>> {
    const fixture = TestBed.createComponent(CategoryFormDialog);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('mode', mode);
    fixture.componentRef.setInput('category', category);
    fixture.componentRef.setInput('parentOptions', parentOptions);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  function setName(fixture: ComponentFixture<CategoryFormDialog>, value: string): void {
    const input = fixture.nativeElement.querySelector('#category-name') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function clickSave(fixture: ComponentFixture<CategoryFormDialog>): void {
    const saveButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'حفظ',
    ) as HTMLButtonElement;
    saveButton.click();
    fixture.detectChanges();
  }

  it('posts the create payload with parent id and active flag', async () => {
    const fixture = await createFixture('create', null, [
      { id: 'cat-1', name: 'خواتم', depth: 0 },
      { id: 'cat-3', name: 'خواتم رجالي', depth: 1 },
    ]);
    setName(fixture, 'سلاسل');
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'cat-1';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    clickSave(fixture);

    await vi.waitFor(() =>
      expect(lastCreateBody).toEqual({
        name: 'سلاسل',
        description: null,
        parentCategoryId: 'cat-1',
        isActive: true,
      }),
    );
  });

  it('requires a name and does not call the API when empty', async () => {
    const fixture = await createFixture();
    clickSave(fixture);

    expect(fixture.nativeElement.textContent).toContain('اسم التصنيف مطلوب');
    expect(lastCreateBody).toBeNull();
  });

  it('prefills the edit target and submits through PUT', async () => {
    const fixture = await createFixture('edit', CATEGORY);
    const input = fixture.nativeElement.querySelector('#category-name') as HTMLInputElement;
    expect(input.value).toBe('خواتم');

    setName(fixture, 'خواتم نسائية');
    const activeCheckbox = fixture.nativeElement.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    activeCheckbox.checked = false;
    activeCheckbox.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    clickSave(fixture);

    await vi.waitFor(() =>
      expect(lastUpdateBody).toEqual({
        name: 'خواتم نسائية',
        description: 'خواتم ذهبية',
        parentCategoryId: null,
        isActive: false,
      }),
    );
    expect(lastUpdateId).toBe('cat-1');
  });

  it('renders the server conflict message inline', async () => {
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
    const fixture = await createFixture();
    setName(fixture, 'خواتم');
    clickSave(fixture);

    await vi.waitFor(() =>
      expect(fixture.nativeElement.textContent).toContain('اسم الفئة موجود بالفعل على هذا المستوى'),
    );
  });

  it('emits saved and closes after a successful create', async () => {
    const fixture = await createFixture();
    const saved = vi.spyOn(fixture.componentInstance.saved, 'emit');
    const closed = vi.spyOn(fixture.componentInstance.openChange, 'emit');
    setName(fixture, 'سلاسل');
    clickSave(fixture);

    await vi.waitFor(() => expect(saved).toHaveBeenCalled());
    expect(closed).toHaveBeenCalledWith(false);
  });
});
