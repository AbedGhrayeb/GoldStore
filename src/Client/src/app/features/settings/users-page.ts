import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
  viewChild,
  type TemplateRef,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

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
import { AuthStore } from '../../core/auth/auth-store';
import { UserFormDialog, type UserFormMode } from './user-form-dialog';
import { UsersStore, type UserResponse } from './users-store';

/** Display row for a tenant user; the full name is joined for the sortable cell. */
export interface UserTableRow extends UserResponse {
  fullName: string;
}

function toRow(user: UserResponse): UserTableRow {
  return { ...user, fullName: `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() };
}

/**
 * P3.5 — Users & settings. Lists the tenant's users with create/edit/delete. The server
 * only allows editing the current user's own profile (`UpdateUserCommandHandler`), so the
 * edit action renders for the current user's row only and the delete action hides for self
 * (deleting your own account mid-session is destructive). The profile card above the table
 * is fed by `/users/me`.
 */
@Component({
  selector: 'app-users-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Badge,
    Button,
    Card,
    Dialog,
    EmptyState,
    LucideAngularModule,
    RetryButton,
    RouterLink,
    Table,
    UserFormDialog,
  ],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">الإعدادات</h1>
          <p class="mt-1 text-sm text-gray-600">مستخدمي المتجر وملفك الشخصي.</p>
        </div>
        @if (isStoreAdmin()) {
          <div class="flex gap-2">
            <a routerLink="/settings/permissions" class="inline-flex items-center gap-2 rounded-lg border border-gray-200 bg-white px-4 py-2 text-sm font-medium hover:bg-gray-50">
              <lucide-icon [img]="shieldIcon" [size]="16" />
              إدارة الصلاحيات
            </a>
            <app-button icon="user-plus" (clicked)="openCreate()">إضافة مستخدم</app-button>
          </div>
        }
      </div>

      @if (me(); as me) {
        <app-card>
          <div class="flex items-center gap-4">
            <span
              class="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-gold-container/60 text-gold"
            >
              <lucide-icon [img]="userIcon" [size]="24" />
            </span>
            <div class="min-w-0">
              <h2 class="truncate text-base font-semibold text-gray-900">
                {{ me.firstName }} {{ me.lastName }}
              </h2>
              <p class="mt-0.5 truncate text-sm text-gray-600" dir="ltr">{{ me.email }}</p>
            </div>
            <div class="ms-auto shrink-0">
              <app-button variant="secondary" size="sm" icon="pencil" (clicked)="openEdit(me)">
                تعديل بياناتي
              </app-button>
            </div>
          </div>
        </app-card>
      }

      @if (isStoreAdmin()) {
        <app-card title="المستخدمون">
          @if (error(); as error) {
            <app-empty-state
              icon="alert-circle"
              title="تعذّر تحميل المستخدمين"
              [description]="error.detail ?? ''"
            >
              <app-retry-button (retry)="reload()" />
            </app-empty-state>
          } @else {
            <app-table
              [columns]="columns()"
              [rows]="rows()"
              [loading]="loading()"
              emptyIcon="users"
              emptyTitle="لا يوجد مستخدمون"
              emptyDescription="أضف أول مستخدم ليتمكن من الوصول إلى المتجر."
            >
              <ng-template #nameCell let-row>
                <span class="flex items-center gap-2">
                  <span class="font-semibold">{{ row.fullName }}</span>
                  @if (row.id === me()?.id) {
                    <app-badge variant="gold">أنت</app-badge>
                  }
                </span>
              </ng-template>
              <ng-template #emailCell let-row>
                <span dir="ltr" class="inline-block text-start">{{ row.email }}</span>
              </ng-template>
              <ng-template #actionsCell let-row>
                <div class="flex justify-center gap-1">
                  @if (row.id === me()?.id) {
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800"
                      title="تعديل بياناتي"
                      [attr.aria-label]="'تعديل بياناتي'"
                      [disabled]="mutatingId() === row.id"
                      (click)="openEdit(row)"
                    >
                      <lucide-icon [img]="pencilIcon" [size]="16" />
                    </button>
                  }
                  @if (row.id !== me()?.id) {
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-error/10 hover:text-red-700"
                      title="حذف"
                      [attr.aria-label]="'حذف ' + (row.fullName || row.email)"
                      [disabled]="mutatingId() === row.id"
                      (click)="openDelete(row)"
                    >
                      <lucide-icon [img]="trashIcon" [size]="16" />
                    </button>
                  }
                </div>
              </ng-template>
            </app-table>
          }
        </app-card>
      }

      <app-user-form-dialog
        [open]="formOpen()"
        [mode]="formMode()"
        [user]="formUser()"
        (openChange)="closeForm()"
        (saved)="onSaved()"
      />

      <app-dialog
        [open]="deleteOpen()"
        title="حذف المستخدم"
        [subtitle]="'الإعدادات'"
        [icon]="'trash-2'"
        (openChange)="closeDelete()"
      >
        @if (deleteTarget(); as target) {
          <p class="text-sm leading-6 text-gray-700">
            هل أنت متأكد من حذف المستخدم
            <span class="font-semibold">{{ target.fullName || target.email }}</span>
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
export class UsersPage {
  private readonly store = inject(UsersStore);
  private readonly auth = inject(AuthStore);

  readonly me = this.store.me;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly mutatingId = this.store.mutatingId;
  readonly isStoreAdmin = computed(() => this.auth.user()?.roles.includes('store_admin') ?? false);

  readonly formOpen = signal(false);
  readonly formMode = signal<UserFormMode>('create');
  readonly formUser = signal<UserResponse | null>(null);
  readonly deleteOpen = signal(false);
  readonly deleteTarget = signal<UserTableRow | null>(null);

  readonly rows = computed(() => (this.store.users() ?? []).map(toRow));

  readonly nameCell = viewChild<TemplateRef<{ $implicit: UserTableRow }>>('nameCell');
  readonly emailCell = viewChild<TemplateRef<{ $implicit: UserTableRow }>>('emailCell');
  readonly actionsCell = viewChild<TemplateRef<{ $implicit: UserTableRow }>>('actionsCell');

  readonly columns = computed<TableColumn<UserTableRow>[]>(() => [
    {
      key: 'fullName',
      header: 'الاسم',
      cell: (row) => row.fullName,
      cellTemplate: this.nameCell(),
    },
    {
      key: 'email',
      header: 'البريد الإلكتروني',
      cell: (row) => row.email ?? '',
      cellTemplate: this.emailCell(),
    },
    {
      key: 'actions',
      header: 'إجراءات',
      cell: () => '',
      align: 'center',
      cellTemplate: this.actionsCell(),
    },
  ]);

  readonly userIcon = resolveIcon('user');
  readonly pencilIcon = resolveIcon('pencil');
  readonly trashIcon = resolveIcon('trash-2');
  readonly shieldIcon = resolveIcon('shield');

  constructor() {
    // Always load profile for "edit my data" card
    void this.store.reloadMe();
    // List of users just display on store admin page — load users only for store_admin
    effect(() => {
      if (this.isStoreAdmin()) {
        void this.store.ensureLoaded();
      }
    });
  }

  reload(): void {
    void this.store.load();
  }

  openCreate(): void {
    if (!this.isStoreAdmin()) return;
    this.formMode.set('create');
    this.formUser.set(null);
    this.formOpen.set(true);
  }

  openEdit(user: UserResponse): void {
    // Only allow editing own profile — keep edit my data form
    if (user.id !== this.store.me()?.id) return;
    this.formMode.set('edit');
    this.formUser.set(toRow(user));
    this.formOpen.set(true);
  }

  closeForm(): void {
    this.formOpen.set(false);
  }

  onSaved(): void {
    this.closeForm();
    void this.store.reloadMe();
  }

  openDelete(row: UserTableRow): void {
    if (!this.isStoreAdmin()) return;
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
      void this.store.deleteUser(target.id ?? '').then(() => this.closeDelete());
    }
  }
}
