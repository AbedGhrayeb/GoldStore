import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
  type TemplateRef,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth-store';

import {
  Badge,
  Button,
  Card,
  Dialog,
  EmptyState,
  RetryButton,
  Table,
  resolveIcon,
  type TableColumn,
} from '../../shared/ui';
import { CategoryFormDialog, type CategoryFormMode } from './category-form-dialog';
import {
  buildCategoryRows,
  buildParentOptions,
  type CategoryParentOption,
  type CategoryTreeRow,
} from './catalog.model';
import { CatalogStore, type CategoryResponse } from './catalog-store';

/**
 * P3.3 — Catalog: categories. A self-parenting tree table rendered depth-first (roots first,
 * children indented), with create/edit/delete and toggle-active row actions. Tree order is
 * canonical, so the table does not offer sorting. Backed by {@link CatalogStore}.
 */
@Component({
  selector: 'app-categories-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Badge,
    Button,
    Card,
    CategoryFormDialog,
    Dialog,
    EmptyState,
    LucideAngularModule,
    RetryButton,
    Table,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">الكتالوج</h1>
          <p class="mt-1 text-sm text-gray-600">
            تصنيفات الأصناف المعروضة في فواتير البيع والشراء.
          </p>
        </div>
        <app-button
          icon="plus"
          [disabled]="!canManageInventory()"
          [title]="!canManageInventory() ? 'ليس لديك صلاحية إدارة الكتالوج' : ''"
          (clicked)="canManageInventory() && openCreate()">إضافة تصنيف</app-button>
      </div>

      <app-card>
        @if (error(); as error) {
          <app-empty-state
            icon="alert-circle"
            title="تعذّر تحميل التصنيفات"
            [description]="error.detail ?? ''"
          >
            <app-retry-button (retry)="reload()" />
          </app-empty-state>
        } @else {
          <app-table
            [columns]="columns()"
            [rows]="rows()"
            [loading]="loading()"
            emptyIcon="tags"
            emptyTitle="لا توجد تصنيفات"
            emptyDescription="أضف أول تصنيف لتنظيم الأصناف في فواتير البيع والشراء."
          >
            <ng-template #nameCell let-row>
              <span class="flex items-center gap-2">
                <span
                  class="inline-block h-1.5 w-1.5 rounded-full bg-gold"
                  [class.invisible]="row.depth === 0"
                ></span>
                <span [class.font-semibold]="row.depth === 0">{{ row.name }}</span>
              </span>
            </ng-template>
            <ng-template #statusCell let-row>
              <app-badge [variant]="row.isActive ? 'success' : 'neutral'">
                {{ row.isActive ? 'نشط' : 'موقوف' }}
              </app-badge>
            </ng-template>
            <ng-template #actionsCell let-row>
              <div class="flex justify-center gap-1">
                <button
                  type="button"
                  class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
                  title="تعديل"
                  [attr.aria-label]="'تعديل ' + row.name"
                  [disabled]="!canManageInventory() || mutatingId() === row.id"
                  [attr.title]="!canManageInventory() ? 'ليس لديك صلاحية التعديل' : 'تعديل'"
                  (click)="canManageInventory() && openEdit(row)"
                >
                  <lucide-icon [img]="pencilIcon" [size]="16" />
                </button>
                <button
                  type="button"
                  class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
                  [title]="row.isActive ? 'إيقاف' : 'تفعيل'"
                  [attr.aria-label]="(row.isActive ? 'إيقاف ' : 'تفعيل ') + row.name"
                  [disabled]="!canManageInventory() || mutatingId() === row.id"
                  [attr.title]="!canManageInventory() ? 'ليس لديك صلاحية' : (row.isActive ? 'إيقاف' : 'تفعيل')"
                  (click)="canManageInventory() && toggleActive(row)"
                >
                  <lucide-icon [img]="powerIcon" [size]="16" />
                </button>
                <button
                  type="button"
                  class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-error/10 hover:text-red-700 disabled:cursor-not-allowed disabled:opacity-40"
                  title="حذف"
                  [attr.aria-label]="'حذف ' + row.name"
                  [disabled]="!canManageInventory() || mutatingId() === row.id"
                  [attr.title]="!canManageInventory() ? 'ليس لديك صلاحية الحذف' : 'حذف'"
                  (click)="canManageInventory() && openDelete(row)"
                >
                  <lucide-icon [img]="trashIcon" [size]="16" />
                </button>
              </div>
            </ng-template>
          </app-table>
        }
      </app-card>

      <app-category-form-dialog
        [open]="formOpen()"
        [mode]="formMode()"
        [category]="formCategory()"
        [parentOptions]="parentOptions()"
        (openChange)="closeForm()"
        (saved)="onSaved()"
      />

      <app-dialog
        [open]="deleteOpen()"
        title="حذف التصنيف"
        [subtitle]="'الكتالوج'"
        [icon]="'trash-2'"
        (openChange)="closeDelete()"
      >
        @if (deleteTarget(); as target) {
          <p class="text-sm leading-6 text-gray-700">
            هل أنت متأكد من حذف التصنيف
            <span class="font-semibold">{{ target.name }}</span>
            ؟ لا يمكن التراجع عن هذا الإجراء.
          </p>
          <div class="flex justify-end gap-3 pt-4">
            <app-button
              variant="secondary"
              type="button"
              [disabled]="mutatingId() !== null"
              (clicked)="closeDelete()"
            >
              إلغاء
            </app-button>
            <app-button
              variant="danger"
              type="button"
              [loading]="mutatingId() === target.id"
              (clicked)="confirmDelete()"
            >
              حذف
            </app-button>
          </div>
        }
      </app-dialog>
    </main>
  `,
})
export class CategoriesPage {
  private readonly auth = inject(AuthStore);
  readonly canManageInventory = computed(() => this.auth.hasPermission('inventory.manage') || this.auth.hasRole('store_admin'));
  private readonly store = inject(CatalogStore);

  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly mutatingId = this.store.mutatingId;

  readonly formOpen = signal(false);
  readonly formMode = signal<CategoryFormMode>('create');
  readonly formCategory = signal<CategoryResponse | null>(null);
  readonly deleteOpen = signal(false);
  readonly deleteTarget = signal<CategoryTreeRow | null>(null);

  readonly rows = computed(() => buildCategoryRows(this.store.categories() ?? []));

  readonly parentOptions = computed<CategoryParentOption[]>(() =>
    buildParentOptions(this.store.categories() ?? [], this.formCategory()?.id ?? null),
  );

  readonly nameCell = viewChild<TemplateRef<{ $implicit: CategoryTreeRow }>>('nameCell');
  readonly statusCell = viewChild<TemplateRef<{ $implicit: CategoryTreeRow }>>('statusCell');
  readonly actionsCell = viewChild<TemplateRef<{ $implicit: CategoryTreeRow }>>('actionsCell');

  readonly columns = computed<TableColumn<CategoryTreeRow>[]>(() => [
    { key: 'name', header: 'الاسم', cell: (row) => row.name, cellTemplate: this.nameCell() },
    { key: 'parent', header: 'التصنيف الأب', cell: (row) => row.parentCategoryName ?? '—' },
    { key: 'description', header: 'الوصف', cell: (row) => row.description ?? '—' },
    {
      key: 'status',
      header: 'الحالة',
      cell: (row) => (row.isActive ? 'نشط' : 'موقوف'),
      cellTemplate: this.statusCell(),
    },
    {
      key: 'actions',
      header: 'إجراءات',
      cell: () => '',
      align: 'center',
      cellTemplate: this.actionsCell(),
    },
  ]);

  readonly pencilIcon = resolveIcon('pencil');
  readonly powerIcon = resolveIcon('power');
  readonly trashIcon = resolveIcon('trash-2');

  constructor() {
    void this.store.ensureLoaded();
  }

  reload(): void {
    void this.store.load();
  }

  openCreate(): void {
    this.formMode.set('create');
    this.formCategory.set(null);
    this.formOpen.set(true);
  }

  openEdit(row: CategoryTreeRow): void {
    this.formMode.set('edit');
    this.formCategory.set({
      id: row.id,
      name: row.name,
      description: row.description,
      parentCategoryId: row.parentCategoryId,
      parentCategoryName: row.parentCategoryName,
      isActive: row.isActive,
    });
    this.formOpen.set(true);
  }

  closeForm(): void {
    this.formOpen.set(false);
  }

  onSaved(): void {
    this.closeForm();
  }

  toggleActive(row: CategoryTreeRow): void {
    void this.store.toggleActive(row.id);
  }

  openDelete(row: CategoryTreeRow): void {
    this.deleteTarget.set(row);
    this.deleteOpen.set(true);
  }

  closeDelete(): void {
    this.deleteOpen.set(false);
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (target !== null) {
      void this.store.deleteCategory(target.id).then(() => this.closeDelete());
    }
  }
}
