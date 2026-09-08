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
  weightInGrams: 10,
  karat: 21,
};

const BASE = apiUrl('/api/v1/categories');
const REFERENCE = apiUrl('/api/v1/reference');

describe('CategoryFormDialog', () => {
  let lastCreateBody: unknown;
  let lastUpdateBody: unknown;
  let lastUpdateId: string | null;

  const server = setupServer(
    http.get(`${REFERENCE}/karats`, () =>
      HttpResponse.json([
        { value: 18, label: 'عيار 18' },
        { value: 21, label: 'عيار 21' },
        { value: 24, label: 'عيار 24' },
      ]),
    ),
    http.get(`${REFERENCE}/currencies`, () =>
      HttpResponse.json([
        { code: 'JOD', symbol: 'د.أ' },
        { code: 'USD', symbol: '$' },
        { code: 'ILS', symbol: '₪' },
      ]),
    ),
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

  function setWeight(fixture: ComponentFixture<CategoryFormDialog>, value: string): void {
    const input = fixture.nativeElement.querySelector('#category-weight') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function setKarat(fixture: ComponentFixture<CategoryFormDialog>, value: string): void {
    const select = fixture.nativeElement.querySelector('#category-karat') as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('change'));
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
    const select = fixture.nativeElement.querySelector('#category-parent') as HTMLSelectElement;
    select.value = 'cat-1';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    setWeight(fixture, '12.5');
    setKarat(fixture, '21');

    clickSave(fixture);

    await vi.waitFor(() =>
      expect(lastCreateBody).toEqual({
        name: 'سلاسل',
        description: null,
        parentCategoryId: 'cat-1',
        isActive: true,
        weightInGrams: 12.5,
        karat: 21,
      }),
    );
  });

  it('blocks submit until weight and karat are provided', async () => {
    const fixture = await createFixture();
    setName(fixture, 'سلاسل');
    clickSave(fixture);

    expect(fixture.nativeElement.textContent).toContain('أدخل وزناً أكبر من صفر');
    expect(fixture.nativeElement.textContent).toContain('اختر العيار');
    expect(lastCreateBody).toBeNull();
  });

  it('requires a name and does not call the API when empty', async () => {
    const fixture = await createFixture();
    clickSave(fixture);

    expect(fixture.nativeElement.textContent).toContain('اسم التصنيف مطلوب');
    expect(lastCreateBody).toBeNull();
  });

  it('prefills the edit target and submits through PUT', async () => {
    const fixture = await createFixture('edit', CATEGORY);
    await vi.waitFor(() => {
      const options = fixture.nativeElement.querySelectorAll('#category-karat option');
      expect(options.length).toBeGreaterThan(1);
    });
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#category-name') as HTMLInputElement;
    expect(input.value).toBe('خواتم');
    expect(
      (fixture.nativeElement.querySelector('#category-weight') as HTMLInputElement).value,
    ).toBe('10');
    expect(
      (fixture.nativeElement.querySelector('#category-karat') as HTMLSelectElement).value,
    ).toBe('21');

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
        weightInGrams: 10,
        karat: 21,
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
    setWeight(fixture, '9');
    setKarat(fixture, '18');
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
    setWeight(fixture, '12.5');
    setKarat(fixture, '21');
    clickSave(fixture);

    await vi.waitFor(() => expect(saved).toHaveBeenCalled());
    expect(closed).toHaveBeenCalledWith(false);
  });
});
