import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import { AuthStore } from '../../core/auth/auth-store';
import { Badge, Button, Card, Dialog, EmptyState, RetryButton, resolveIcon } from '../../shared/ui';
import { PermissionsStore } from './permissions-store';
import type { UserResponse } from './users-api.service';
import type { PermissionResponse, RoleResponse } from './authorization-api.service';

interface UserRow extends UserResponse {
  fullName: string;
  initials: string;
}

function toRow(u: UserResponse): UserRow {
  const full = `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim();
  const initials = (u.firstName?.[0] ?? u.email?.[0] ?? '؟').toUpperCase();
  return { ...u, fullName: full || u.email || '—', initials };
}

const GROUP_LABELS: Record<string, string> = {
  users: 'المستخدمون',
  employees: 'الموظفون',
  suppliers: 'الموردون',
  inventory: 'المخزون',
  finance: 'المالية',
  sales: 'المبيعات',
  purchases: 'المشتريات',
  expenses: 'المصروفات',
  reports: 'التقارير',
  settings: 'الإعدادات',
};

@Component({
  selector: 'app-permissions-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Badge, Button, Card, Dialog, EmptyState, LucideAngularModule, RetryButton],
  template: `
    <main class="mx-auto max-w-6xl space-y-6">
      <!-- Header -->
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div class="flex items-center gap-3">
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-gold text-white shadow-sm">
              <lucide-icon [img]="shieldIcon" [size]="20" />
            </span>
            <div>
              <h1 class="text-2xl font-bold tracking-tight text-gray-900">إدارة الصلاحيات</h1>
              <p class="mt-1 text-sm text-gray-600">إسناد الأدوار والصلاحيات المباشرة — يطبّق بعد تسجيل الدخول التالي.</p>
            </div>
          </div>
          <p class="mt-3 hidden text-xs text-amber-700 sm:block">تلميح: المجموعة تحدد كل الصلاحيات الفرعية. يمكنك تحديد المجموعة ثم إلغاء أي صلاحية فرعية.</p>
        </div>
        <div class="flex items-center gap-2">
          <span class="hidden items-center gap-1.5 rounded-full bg-amber-50 px-3 py-1.5 text-xs font-medium text-amber-700 ring-1 ring-amber-200 sm:inline-flex">
            <lucide-icon [img]="infoIcon" [size]="14" /> يتطلب إعادة تسجيل الدخول
          </span>
          <app-button variant="secondary" icon="users" (clicked)="reload()">تحديث</app-button>
        </div>
      </div>

      @if (!isStoreAdmin()) {
        <app-card>
          <div class="flex items-start gap-3 rounded-lg bg-error/10 p-4 text-error ring-1 ring-error/20">
            <lucide-icon [img]="shieldOffIcon" [size]="18" class="mt-0.5 shrink-0" />
            <p class="text-sm font-medium">ليس لديك صلاحية إدارة الصلاحيات. هذه الصفحة لمدير المتجر فقط.</p>
          </div>
        </app-card>
      } @else {
        <!-- Stats -->
        <div class="grid gap-4 sm:grid-cols-3">
          <app-card>
            <div class="flex items-center gap-4">
              <span class="flex h-10 w-10 items-center justify-center rounded-lg bg-gold-container/60 text-gold"><lucide-icon [img]="usersIcon" [size]="18" /></span>
              <div>
                <p class="text-xs text-gray-500">المستخدمون</p>
                <p class="text-xl font-bold text-gray-900">{{ rows().length }}</p>
              </div>
              <span class="ms-auto text-xs text-gray-400" dir="ltr">{{ loading() ? '…' : 'total' }}</span>
            </div>
          </app-card>
          <app-card>
            <div class="flex items-center gap-4">
              <span class="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700"><lucide-icon [img]="shieldIcon" [size]="18" /></span>
              <div>
                <p class="text-xs text-gray-500">الأدوار</p>
                <p class="text-xl font-bold text-gray-900">{{ (roles()?.length ?? 0) }}</p>
              </div>
            </div>
          </app-card>
          <app-card>
            <div class="flex items-center gap-4">
              <span class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50 text-blue-700"><lucide-icon [img]="keyIcon" [size]="18" /></span>
              <div>
                <p class="text-xs text-gray-500">الصلاحيات</p>
                <p class="text-xl font-bold text-gray-900">{{ permissionCount() }}</p>
              </div>
            </div>
          </app-card>
        </div>

        @if (error(); as err) {
          <app-empty-state icon="alert-circle" title="تعذّر تحميل البيانات" [description]="err.detail ?? ''">
            <app-retry-button (retry)="reload()" />
          </app-empty-state>
        } @else {
          <!-- Filters -->
          <div class="flex flex-wrap items-center gap-3">
            <div class="relative min-w-[260px] flex-1">
              <lucide-icon [img]="searchIcon" [size]="16" class="pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="search"
                [value]="search()"
                (input)="search.set($any($event.target).value)"
                placeholder="بحث بالاسم أو البريد..."
                class="w-full rounded-lg border border-gray-200 bg-white py-2.5 pe-3 ps-9 text-sm outline-none placeholder:text-gray-400 focus:border-gold focus:ring-2 focus:ring-gold/20"
              />
            </div>
            <div class="flex items-center gap-2 text-xs">
              <span class="text-gray-500">تصفية:</span>
              <select [value]="filterRole()" (change)="filterRole.set($any($event.target).value)" class="rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm focus:border-gold focus:ring-2 focus:ring-gold/20">
                <option value="">كل الأدوار</option>
                @for (r of roles() ?? []; track r.id) { <option [value]="r.id">{{ r.name }}</option> }
              </select>
              @if (filterRole() || search()) {
                <button type="button" (click)="clearFilters()" class="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-50">مسح</button>
              }
            </div>
          </div>

          <!-- Users table -->
          <app-card>
            <div class="-mx-6 -mt-6">
              <div class="overflow-auto rounded-t-lg border-b border-gray-100">
                <table class="w-full text-sm">
                  <thead class="bg-gray-50/70 text-xs font-medium text-gray-500">
                    <tr>
                      <th class="px-5 py-3 text-start font-semibold">المستخدم</th>
                      <th class="px-4 py-3 text-start font-semibold">الأدوار</th>
                      <th class="px-4 py-3 text-start font-semibold">صلاحيات مباشرة</th>
                      <th class="px-4 py-3 text-center font-semibold">إجراءات</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100 bg-white">
                    @for (row of filteredRows(); track row.id) {
                      <tr class="group hover:bg-gold-container/20">
                        <td class="px-5 py-4">
                          <div class="flex items-center gap-3">
                            <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gold-container/60 text-sm font-bold text-gray-700 ring-1 ring-gold-container">{{ row.initials }}</span>
                            <div class="min-w-0">
                              <p class="flex items-center gap-1.5 truncate font-semibold text-gray-900">
                                {{ row.fullName }}
                                @if (row.id === meId()) { <app-badge variant="gold">أنت</app-badge> }
                                @if (isLastAdmin(row.id ?? '')) { <span class="inline-flex items-center gap-1 rounded-full bg-amber-50 px-2 py-0.5 text-[10px] font-medium text-amber-700 ring-1 ring-amber-200">آخر مدير</span> }
                              </p>
                              <p class="truncate text-xs text-gray-500" dir="ltr">{{ row.email }}</p>
                            </div>
                          </div>
                        </td>
                        <td class="px-4 py-3">
                          <div class="flex flex-wrap gap-1.5">
                            @for (label of roleBadges(row.id ?? ''); track label) {
                              <span class="inline-flex items-center rounded-full bg-gray-900 px-2.5 py-1 text-[11px] font-medium text-white">{{ label }}</span>
                            }
                            @if (roleBadges(row.id ?? '').length === 0) {
                              <span class="inline-flex items-center gap-1 rounded-full bg-gray-100 px-2.5 py-1 text-[11px] text-gray-500"><lucide-icon [img]="shieldOffIcon" [size]="12" /> بلا دور</span>
                            }
                          </div>
                        </td>
                        <td class="px-4 py-3">
                          <div class="flex flex-wrap gap-1">
                            @for (perm of directPermissionsFor(row.id ?? '').slice(0,3); track perm) {
                              <span class="rounded-full bg-emerald-50 px-2 py-1 text-[10px] text-emerald-700 ring-1 ring-emerald-200">{{ perm }}</span>
                            }
                            @if (directPermissionsFor(row.id ?? '').length > 3) {
                              <span class="rounded-full bg-gray-100 px-2 py-1 text-[10px]">+{{ directPermissionsFor(row.id ?? '').length - 3 }}</span>
                            }
                            @if (directPermissionsFor(row.id ?? '').length === 0) { <span class="text-[11px] text-gray-400">—</span> }
                          </div>
                        </td>
                        <td class="px-4 py-3 text-center">
                          <app-button size="sm" variant="secondary" icon="shield" (clicked)="openManage(row)">إدارة الصلاحيات</app-button>
                        </td>
                      </tr>
                    }
                    @if (loading()) {
                      <tr><td colspan="4" class="px-4 py-10 text-center"><span class="inline-flex items-center gap-2 text-sm text-gray-400"><span class="h-4 w-4 animate-spin rounded-full border-2 border-gray-300 border-t-gold"></span> جاري التحميل...</span></td></tr>
                    }
                    @if (!loading() && filteredRows().length === 0) {
                      <tr><td colspan="4" class="px-4 py-10 text-center text-sm text-gray-500">لا نتائج مطابقة للبحث</td></tr>
                    }
                  </tbody>
                </table>
              </div>
              <p class="px-1 pt-3 text-xs text-gray-500">التغييرات تُطبّق فورًا وتظهر للمستخدم بعد تسجيل دخول جديد.</p>
            </div>
          </app-card>

          <!-- Roles overview -->
          <app-card title="الأدوار المتاحة وصلاحياتها">
            <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              @for (role of roles() ?? []; track role.id) {
                <div class="group relative overflow-hidden rounded-xl border border-gray-200 bg-white p-4 shadow-sm transition hover:border-gold/30 hover:shadow-md">
                  <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-gold to-amber-400 opacity-80"></div>
                  <div class="flex items-start justify-between gap-3">
                    <div class="flex items-center gap-2">
                      <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-gold-container/60 text-gold"><lucide-icon [img]="roleIconFor(role.key)" [size]="16" /></span>
                      <div>
                        <p class="text-sm font-bold text-gray-900">{{ role.name }}</p>
                        <p class="text-[11px] font-mono text-gray-500" dir="ltr">{{ role.key }}</p>
                      </div>
                    </div>
                    <span class="rounded-full bg-gray-900 px-2 py-1 text-[11px] font-medium text-white">{{ role.permissionKeys.length }} صلاحية</span>
                  </div>
                  <div class="mt-3 flex flex-wrap gap-1.5">
                    @for (perm of role.permissionKeys; track perm) {
                      <span class="inline-flex items-center rounded-full bg-gray-50 px-2 py-1 text-[11px] font-medium text-gray-700 ring-1 ring-gray-200">{{ perm }}</span>
                    }
                  </div>
                </div>
              }
            </div>
          </app-card>
        }
      }

      <!-- Manage dialog -->
      <app-dialog
        [open]="dialogOpen()"
        title="إدارة صلاحيات المستخدم"
        subtitle="الإعدادات · يتطلب إعادة تسجيل دخول"
        icon="shield"
        maxWidth="max-w-3xl"
        (openChange)="closeDialog()"
      >
        @if (selectedUser(); as user) {
          <div class="space-y-5">
            <!-- User header -->
            <div class="flex items-center gap-3 rounded-xl border border-gold-container bg-gold-container/20 p-4">
              <span class="flex h-11 w-11 items-center justify-center rounded-full bg-gold text-white text-sm font-bold shadow">{{ user.initials }}</span>
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-bold text-gray-900">{{ user.fullName }}</p>
                <p class="truncate text-xs text-gray-600" dir="ltr">{{ user.email }}</p>
              </div>
              <div class="hidden items-center gap-2 sm:flex">
                <span class="rounded-full bg-white px-3 py-1 text-xs font-medium text-gray-700 ring-1 ring-gray-200">{{ selectedRoleIds().length }} أدوار</span>
                <span class="rounded-full bg-emerald-50 px-3 py-1 text-xs font-medium text-emerald-700 ring-1 ring-emerald-200">{{ selectedPermissionKeys().length }} مباشرة</span>
              </div>
            </div>

            @if (dialogLoading()) {
              <div class="flex items-center gap-2 rounded-lg bg-gray-50 p-4 text-sm text-gray-500"><span class="h-4 w-4 animate-spin rounded-full border-2 border-gray-300 border-t-gold"></span> جاري تحميل الأدوار والصلاحيات...</div>
            } @else {
              <!-- Roles section -->
              <div>
                <div class="mb-2 flex items-center justify-between">
                  <h3 class="text-sm font-bold text-gray-800">الأدوار</h3>
                  <div class="flex gap-2">
                    <button type="button" (click)="selectAllRoles()" class="rounded-md bg-white px-3 py-1.5 text-xs font-medium ring-1 ring-gray-200 hover:bg-gray-50">تحديد الكل</button>
                    <button type="button" (click)="clearAllRoles()" class="rounded-md bg-white px-3 py-1.5 text-xs font-medium ring-1 ring-gray-200 hover:bg-gray-50">إلغاء الكل</button>
                  </div>
                </div>
                <div class="grid gap-3 sm:grid-cols-2">
                  @for (role of roles() ?? []; track role.id) {
                    @let checked = selectedRoleIds().includes(role.id);
                    <label
                      class="group relative flex cursor-pointer flex-col gap-2 rounded-xl border p-4 transition"
                      [class]="checked ? 'border-gold bg-gold-container/20 ring-1 ring-gold shadow-sm' : 'border-gray-200 bg-white hover:border-gray-300 hover:bg-gray-50'"
                    >
                      <input type="checkbox" [checked]="checked" (change)="toggleRole(role.id, $any($event.target).checked)" class="peer sr-only" />
                      <div class="flex items-start justify-between gap-3">
                        <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-white text-gold ring-1 ring-gray-200 group-[.border-gold]:bg-gold group-[.border-gold]:text-white"><lucide-icon [img]="roleIconFor(role.key)" [size]="16" /></span>
                        <span class="flex h-5 w-5 items-center justify-center rounded-full border-2 bg-white text-white transition" [class]="checked ? 'border-gold bg-gold' : 'border-gray-300'">
                          @if (checked) { <lucide-icon [img]="checkIcon" [size]="12" /> }
                        </span>
                      </div>
                      <div>
                        <p class="text-sm font-bold text-gray-900">{{ role.name }}</p>
                        <p class="text-[11px] font-mono text-gray-500" dir="ltr">{{ role.key }}</p>
                      </div>
                      <div class="flex flex-wrap gap-1">
                        @for (perm of role.permissionKeys.slice(0,5); track perm) {
                          <span class="rounded-full bg-white px-2 py-0.5 text-[10px] font-medium text-gray-700 ring-1 ring-gray-200">{{ perm }}</span>
                        }
                        @if (role.permissionKeys.length > 5) { <span class="rounded-full bg-gray-900 px-2 py-0.5 text-[10px] font-medium text-white">+{{ role.permissionKeys.length - 5 }}</span> }
                      </div>
                      @if (role.key === 'store_admin' && isLastAdmin(user.id ?? '') && checked) {
                        <span class="inline-flex items-center gap-1 rounded-md bg-amber-50 px-2 py-1 text-[11px] font-medium text-amber-700 ring-1 ring-amber-200">تحذير: إزالة هذا الدور ستترك المتجر بلا مدير</span>
                      }
                    </label>
                  }
                </div>
              </div>

              <!-- Direct permissions with groups -->
              <div class="rounded-xl border border-gray-200 bg-white p-4">
                <div class="mb-3 flex items-center justify-between">
                  <h3 class="text-sm font-bold text-gray-800">صلاحيات مباشرة (مجموعات)</h3>
                  <div class="flex gap-2">
                    <button type="button" (click)="selectAllPermissions()" class="rounded-md bg-gray-900 px-3 py-1.5 text-xs font-medium text-white hover:bg-black">تحديد الكل</button>
                    <button type="button" (click)="clearAllPermissions()" class="rounded-md bg-white px-3 py-1.5 text-xs font-medium ring-1 ring-gray-200 hover:bg-gray-50">إلغاء الكل</button>
                  </div>
                </div>
                <p class="mb-3 text-xs text-gray-500">عند تحديد مجموعة، يتم تحديد كل الصلاحيات الفرعية تلقائيًا ويمكنك بعدها إلغاء أي صلاحية فرعية.</p>

                <div class="space-y-3">
                  @for (group of groupedPermissions(); track group.key) {
                    <div class="rounded-lg border border-gray-100 bg-gray-50/50 p-3">
                      <label class="flex cursor-pointer items-center gap-3">
                        <input
                          type="checkbox"
                          [checked]="isGroupChecked(group)"
                          [indeterminate]="isGroupIndeterminate(group)"
                          (change)="toggleGroup(group.key, $any($event.target).checked)"
                          class="h-4 w-4 rounded border-gray-300 text-gold focus:ring-gold"
                        />
                        <span class="flex-1 text-sm font-bold text-gray-900">{{ group.label }} <span class="font-mono text-xs text-gray-500">({{ group.key }})</span></span>
                        <span class="rounded-full bg-white px-2 py-0.5 text-[11px] ring-1 ring-gray-200">{{ group.permissions.length }} صلاحية</span>
                      </label>
                      <div class="mt-2 ms-7 grid gap-2 sm:grid-cols-2">
                        @for (perm of group.permissions; track perm.key) {
                          <label class="flex cursor-pointer items-center gap-2 rounded-md bg-white px-3 py-2 text-sm ring-1 ring-gray-200 hover:bg-gray-50">
                            <input
                              type="checkbox"
                              [checked]="selectedPermissionKeys().includes(perm.key)"
                              (change)="togglePermission(perm.key, $any($event.target).checked)"
                              class="h-4 w-4 rounded border-gray-300 text-gold focus:ring-gold"
                            />
                            <span class="flex-1">
                              <span class="block text-xs font-medium">{{ perm.name }}</span>
                              <span class="block font-mono text-[11px] text-gray-500">{{ perm.key }}</span>
                            </span>
                          </label>
                        }
                      </div>
                    </div>
                  }
                </div>

                <!-- View/Manage masters -->
                <div class="mt-4 flex flex-wrap gap-2 rounded-lg bg-amber-50 p-3 ring-1 ring-amber-200">
                  <span class="text-xs font-bold text-amber-800">اختصارات:</span>
                  <button type="button" (click)="toggleViewGroup(true)" class="rounded-full bg-white px-3 py-1 text-xs font-medium ring-1 ring-amber-200 hover:bg-amber-100">تحديد كل العرض</button>
                  <button type="button" (click)="toggleManageGroup(true)" class="rounded-full bg-white px-3 py-1 text-xs font-medium ring-1 ring-amber-200 hover:bg-amber-100">تحديد كل الإدارة</button>
                  <button type="button" (click)="toggleViewGroup(false)" class="rounded-full bg-white px-3 py-1 text-xs ring-1 ring-gray-200 hover:bg-gray-50">إلغاء العرض</button>
                  <button type="button" (click)="toggleManageGroup(false)" class="rounded-full bg-white px-3 py-1 text-xs ring-1 ring-gray-200 hover:bg-gray-50">إلغاء الإدارة</button>
                </div>

                <div class="mt-3 flex flex-wrap gap-1.5 rounded-lg bg-white p-3 ring-1 ring-gray-200">
                  @for (perm of selectedPermissionKeys(); track perm) {
                    <span class="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-medium text-emerald-700 ring-1 ring-emerald-200">{{ perm }}</span>
                  }
                  @if (selectedPermissionKeys().length === 0) { <span class="text-xs text-gray-500">لا صلاحيات مباشرة محددة — سيُعتمد على الأدوار فقط.</span> }
                </div>
              </div>

              <!-- Effective preview -->
              <div class="rounded-xl border border-gray-200 bg-gray-50 p-4">
                <p class="text-xs font-bold text-gray-700">الصلاحيات الفعلية (أدوار + مباشرة)</p>
                <div class="mt-2 flex flex-wrap gap-1.5">
                  @for (perm of effectivePermissions(); track perm) {
                    <span class="inline-flex items-center rounded-full bg-white px-2.5 py-1 text-[11px] font-medium text-gray-700 ring-1 ring-gray-200">{{ perm }}</span>
                  }
                  @if (effectivePermissions().length === 0) { <span class="text-xs text-gray-500">لا صلاحيات</span> }
                </div>
              </div>
            }

            @if (saveError(); as err) {
              <div class="flex gap-2 rounded-lg bg-error/10 p-3 text-sm text-red-700 ring-1 ring-error/20">
                <lucide-icon [img]="alertIcon" [size]="16" class="mt-0.5 shrink-0" />
                <span>{{ err.detail ?? err.title ?? 'تعذّر الحفظ' }}</span>
              </div>
            }

            <div class="flex justify-end gap-3 pt-1">
              <app-button variant="secondary" (clicked)="closeDialog()" [disabled]="saving()">إلغاء</app-button>
              <app-button (clicked)="save()" [loading]="saving()" [disabled]="dialogLoading()">
                <lucide-icon [img]="checkIcon" [size]="16" /> حفظ الصلاحيات
              </app-button>
            </div>
          </div>
        }
      </app-dialog>
    </main>
  `,
})
export class PermissionsPage {
  private readonly permsStore = inject(PermissionsStore);
  private readonly auth = inject(AuthStore);

  readonly isStoreAdmin = computed(() => this.auth.user()?.roles.includes('store_admin') ?? false);
  readonly meId = computed(() => this.auth.user()?.userId ?? null);
  readonly users = this.permsStore.users;
  readonly roles = this.permsStore.roles;
  readonly permissions = this.permsStore.permissions;
  readonly loading = this.permsStore.loading;
  readonly error = this.permsStore.error;
  readonly saving = this.permsStore.saving;
  readonly saveError = this.permsStore.saveError;

  readonly permissionCount = computed(() => this.permissions()?.length ?? 0);

  readonly search = signal('');
  readonly filterRole = signal<string>('');

  readonly rows = computed(() => (this.users() ?? []).map(toRow));
  readonly filteredRows = computed(() => {
    const q = this.search().trim().toLowerCase();
    const f = this.filterRole();
    return this.rows().filter((r) => {
      const matchSearch = !q || r.fullName.toLowerCase().includes(q) || (r.email ?? '').toLowerCase().includes(q);
      const matchRole = !f || (this.roleCache.get(r.id ?? '') ?? []).includes(f);
      return matchSearch && matchRole;
    });
  });

  readonly groupedPermissions = computed(() => {
    const perms = this.permissions() ?? [];
    const map = new Map<string, { key: string; label: string; permissions: PermissionResponse[] }>();
    for (const p of perms) {
      const groupKey = p.key.split('.')[0];
      const label = GROUP_LABELS[groupKey] ?? groupKey;
      if (!map.has(groupKey)) map.set(groupKey, { key: groupKey, label, permissions: [] });
      map.get(groupKey)!.permissions.push(p);
    }
    return [...map.values()].sort((a, b) => a.key.localeCompare(b.key));
  });

  // dialog state
  readonly dialogOpen = signal(false);
  readonly selectedUser = signal<UserRow | null>(null);
  readonly selectedRoleIds = signal<string[]>([]);
  readonly selectedPermissionKeys = signal<string[]>([]);
  readonly dialogLoading = signal(false);

  readonly shieldIcon = resolveIcon('shield');
  readonly shieldOffIcon = resolveIcon('shield');
  readonly usersIcon = resolveIcon('users');
  readonly keyIcon = resolveIcon('layers');
  readonly searchIcon = resolveIcon('search');
  readonly infoIcon = resolveIcon('info');
  readonly checkIcon = resolveIcon('check-circle');
  readonly alertIcon = resolveIcon('alert-circle');

  readonly effectivePermissions = computed(() => {
    const roleIds = new Set(this.selectedRoleIds());
    const perms = new Set<string>(this.selectedPermissionKeys());
    for (const r of this.roles() ?? []) if (roleIds.has(r.id)) for (const p of r.permissionKeys) perms.add(p);
    return [...perms].sort();
  });

  private readonly roleCache = new Map<string, string[]>();
  private readonly permCache = new Map<string, string[]>();

  constructor() {
    effect(() => {
      if (this.isStoreAdmin()) void this.permsStore.load();
    });
  }

  reload(): void {
    void this.permsStore.load();
  }

  clearFilters(): void {
    this.search.set('');
    this.filterRole.set('');
  }

  roleIconFor(key: string): ReturnType<typeof resolveIcon> {
    switch (key) {
      case 'store_admin': return resolveIcon('crown');
      case 'manager': return resolveIcon('building-2');
      case 'cashier': return resolveIcon('wallet');
      case 'viewer': return resolveIcon('eye');
      case 'inventory_clerk': return resolveIcon('boxes');
      default: return resolveIcon('shield');
    }
  }

  isLastAdmin(userId: string): boolean {
    const adminRoleId = this.roles()?.find((r) => r.key === 'store_admin')?.id;
    if (!adminRoleId) return false;
    let adminCount = 0;
    for (const [, ids] of this.roleCache) if (ids.includes(adminRoleId)) adminCount++;
    if (adminCount === 0) return false;
    return adminCount === 1 && (this.roleCache.get(userId) ?? []).includes(adminRoleId);
  }

  roleLabelsForUser(userId: string | undefined): string {
    if (!userId) return '—';
    const cached = this.roleCache.get(userId);
    if (cached === undefined) return 'غير محمّل';
    if (cached.length === 0) return 'بلا دور';
    const roles = this.roles() ?? [];
    return cached.map((id) => roles.find((r) => r.id === id)?.name ?? id).join('، ');
  }

  roleBadges(userId: string): string[] {
    const ids = this.roleCache.get(userId) ?? [];
    const roles = this.roles() ?? [];
    return ids.map((id) => roles.find((r) => r.id === id)?.name ?? id);
  }

  directPermissionsFor(userId: string): string[] {
    return this.permCache.get(userId) ?? [];
  }

  // group helpers
  isGroupChecked(group: { key: string; permissions: PermissionResponse[] }): boolean {
    const selected = new Set(this.selectedPermissionKeys());
    return group.permissions.every((p) => selected.has(p.key));
  }

  isGroupIndeterminate(group: { key: string; permissions: PermissionResponse[] }): boolean {
    const selected = new Set(this.selectedPermissionKeys());
    const hasSome = group.permissions.some((p) => selected.has(p.key));
    const hasAll = group.permissions.every((p) => selected.has(p.key));
    return hasSome && !hasAll;
  }

  toggleGroup(groupKey: string, checked: boolean): void {
    const group = this.groupedPermissions().find((g) => g.key === groupKey);
    if (!group) return;
    const cur = new Set(this.selectedPermissionKeys());
    for (const p of group.permissions) {
      if (checked) cur.add(p.key);
      else cur.delete(p.key);
    }
    this.selectedPermissionKeys.set([...cur]);
  }

  togglePermission(key: string, checked: boolean): void {
    const cur = new Set(this.selectedPermissionKeys());
    if (checked) cur.add(key);
    else cur.delete(key);
    this.selectedPermissionKeys.set([...cur]);
  }

  toggleViewGroup(checked: boolean): void {
    const viewKeys = (this.permissions() ?? []).filter((p) => p.key.endsWith('.view')).map((p) => p.key);
    const cur = new Set(this.selectedPermissionKeys());
    for (const k of viewKeys) { if (checked) cur.add(k); else cur.delete(k); }
    this.selectedPermissionKeys.set([...cur]);
  }

  toggleManageGroup(checked: boolean): void {
    const manageKeys = (this.permissions() ?? []).filter((p) => p.key.endsWith('.manage')).map((p) => p.key);
    const cur = new Set(this.selectedPermissionKeys());
    for (const k of manageKeys) { if (checked) cur.add(k); else cur.delete(k); }
    this.selectedPermissionKeys.set([...cur]);
  }

  async openManage(user: UserRow): Promise<void> {
    if (!this.isStoreAdmin()) return;
    this.selectedUser.set(user);
    this.dialogOpen.set(true);
    this.dialogLoading.set(true);
    this.permsStore.clearSaveError();
    try {
      const [roleIds, permKeys] = await Promise.all([
        this.permsStore.getUserRoles(user.id ?? ''),
        this.permsStore.getUserPermissions(user.id ?? ''),
      ]);
      this.selectedRoleIds.set([...roleIds]);
      this.selectedPermissionKeys.set([...permKeys]);
      this.roleCache.set(user.id ?? '', [...roleIds]);
      this.permCache.set(user.id ?? '', [...permKeys]);
    } catch {}
    this.dialogLoading.set(false);
  }

  closeDialog(): void {
    this.dialogOpen.set(false);
    this.selectedUser.set(null);
    this.selectedRoleIds.set([]);
    this.selectedPermissionKeys.set([]);
    this.permsStore.clearSaveError();
  }

  toggleRole(roleId: string, checked: boolean): void {
    const cur = new Set(this.selectedRoleIds());
    if (checked) cur.add(roleId);
    else cur.delete(roleId);
    this.selectedRoleIds.set([...cur]);
  }

  selectAllRoles(): void {
    this.selectedRoleIds.set((this.roles() ?? []).map((r) => r.id));
  }

  clearAllRoles(): void {
    this.selectedRoleIds.set([]);
  }

  selectAllPermissions(): void {
    this.selectedPermissionKeys.set((this.permissions() ?? []).map((p) => p.key));
  }

  clearAllPermissions(): void {
    this.selectedPermissionKeys.set([]);
  }

  async save(): Promise<void> {
    const user = this.selectedUser();
    if (!user) return;
    // Save both roles and direct permissions (two calls)
    const okRoles = await this.permsStore.setUserRoles(user.id ?? '', this.selectedRoleIds());
    if (!okRoles) return;
    const okPerms = await this.permsStore.setUserPermissions(user.id ?? '', this.selectedPermissionKeys());
    if (okPerms) {
      this.roleCache.set(user.id ?? '', [...this.selectedRoleIds()]);
      this.permCache.set(user.id ?? '', [...this.selectedPermissionKeys()]);
      this.closeDialog();
    }
  }
}
