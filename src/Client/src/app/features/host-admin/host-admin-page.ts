import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
  type TemplateRef,
} from '@angular/core';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

import { AuthStore } from '../../core/auth/auth-store';

import {
  Badge,
  Button,
  Card,
  EmptyState,
  RetryButton,
  Table,
  resolveIcon,
  type TableColumn,
} from '../../shared/ui';
import type {
  ReconciliationAnomaly,
  SubscriptionPlanResponse,
  TenantReconciliationBlock,
  TenantSummaryResponse,
} from './host-api.service';
import { CreatePlanDialog } from './create-plan-dialog';
import { HostStore } from './host-store';
import { HostUpdateStatusDialog } from './host-update-status-dialog';
import { ProvisionTenantDialog } from './provision-tenant-dialog';
import { RenewSubscriptionDialog } from './renew-subscription-dialog';

type HostTab = 'tenants' | 'reconciliation' | 'plans';

/**
 * Host admin — sovereign control plane for the platform.
 * Signature: vault-inspired KPI header + pill tabs with counts + host-tenant cards
 * that echo a private vault hall: gold foil accents, mono subdomain chips, and
 * timeline-aware subscription drawers. All 3 tabs live in one route with lazy
 * loading of plans/reconciliation.
 */
@Component({
  selector: 'app-host-admin-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Badge,
    Button,
    Card,
    CreatePlanDialog,
    EmptyState,
    HostUpdateStatusDialog,
    LucideAngularModule,
    ProvisionTenantDialog,
    RenewSubscriptionDialog,
    RetryButton,
    Table,
  ],
  template: `
    <main class="mx-auto max-w-[1400px] space-y-6">
      <!-- ══ Host hero — vault hall header ══ -->
      <div class="overflow-hidden rounded-xl border border-amber-200/60 bg-gradient-to-br from-white via-amber-50/40 to-gold-container/20 shadow-sm">
        <div class="relative">
          <!-- subtle arabesque pattern -->
          <div class="pointer-events-none absolute inset-0 opacity-[0.03]" aria-hidden="true"
            style="background-image: radial-gradient(circle at 1px 1px, #D4AF37 1px, transparent 0); background-size: 20px 20px;"></div>
          <div class="relative flex flex-wrap items-start justify-between gap-4 px-6 py-6 sm:px-7">
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2 text-xs font-medium tracking-widest text-amber-700/80">
                <span class="material-symbols-outlined icon-fill text-[16px] leading-none text-gold" style="font-variation-settings:'FILL' 1">diamond</span>
                <span>منصة بريق — وحدة تحكم المضيف</span>
                <span class="hidden sm:inline rounded-full bg-gold/15 px-2 py-0.5 text-[10px] font-bold text-amber-800">HOST ONLY</span>
              </div>
              <h1 class="mt-2 flex items-center gap-3 text-2xl font-bold tracking-tight text-gray-900">
                <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-gold to-amber-600 text-white shadow-sm">
                  <lucide-icon [img]="crownIcon" [size]="18" />
                </span>
                إدارة المنصة
              </h1>
              <p class="mt-2 max-w-2xl text-sm leading-6 text-gray-600">
                سجل المستأجرين، دورة حياة الحالات، والمطابقة المحاسبية عبر قيود الملكية — كل ذلك من طبقة السيادة.
              </p>
              <div class="mt-3 flex flex-wrap items-center gap-2 text-xs text-gray-500">
                <span class="inline-flex items-center gap-1.5 rounded-full border border-gray-200 bg-white px-2.5 py-1">
                  <span class="h-2 w-2 rounded-full bg-emerald-500"></span>
                  {{ tenants().length }} مستأجر مسجّل
                </span>
                <span class="inline-flex items-center gap-1.5 rounded-full border border-gray-200 bg-white px-2.5 py-1">
                  <lucide-icon [img]="layersIcon" [size]="12" />
                  {{ plans().length }} خطة اشتراك
                </span>
                @if (reconciliation(); as recon) {
                  <span class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1" [class]="recon.anomalies.length === 0 ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-red-200 bg-red-50 text-red-700'">
                    <lucide-icon [img]="recon.anomalies.length === 0 ? checkIcon : alertIcon" [size]="12" />
                    {{ recon.anomalies.length === 0 ? 'سليم — بلا انتهاكات' : recon.anomalies.length + ' انتهاك' }}
                  </span>
                }
              </div>
            </div>
            <div class="flex shrink-0 items-center gap-2">
              <app-button
                variant="secondary"
                icon="refresh-cw"
                [loading]="tenantsLoading() || reconciliationLoading() || plansLoading()"
                (clicked)="refreshActiveTab()"
              >
                تحديث
              </app-button>
              <button
                type="button"
                class="inline-flex items-center gap-2 rounded-input border border-gray-200 bg-white px-3 py-2 text-sm font-medium text-gray-600 shadow-sm transition-colors hover:bg-gray-50 hover:text-gray-900 disabled:opacity-50"
                [disabled]="hostSigningOut()"
                (click)="signOut()"
                aria-label="تسجيل خروج المنصة"
                title="تسجيل خروج المنصة"
              >
                <lucide-icon [img]="logOutIcon" [size]="16" />
                تسجيل الخروج
              </button>
            </div>
          </div>
        </div>
        <!-- KPI strip — vault metrics -->
        <div class="grid grid-cols-2 gap-px border-t border-amber-200/40 bg-amber-200/40 sm:grid-cols-4">
          @for (kpi of kpis(); track kpi.label) {
            <div class="flex items-center gap-3 bg-white px-4 py-3.5 sm:px-5">
              <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg text-white shadow-sm" [class]="kpi.bg">
                <lucide-icon [img]="kpi.icon" [size]="18" />
              </span>
              <div class="min-w-0">
                <p class="text-[11px] font-medium tracking-widest text-gray-500">{{ kpi.label }}</p>
                <p class="text-lg font-bold leading-none text-gray-900 data-mono">{{ kpi.value }}</p>
                <p class="text-xs text-gray-500">{{ kpi.hint }}</p>
              </div>
            </div>
          }
        </div>
      </div>

      <!-- Tabs — pill sovereign nav -->
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div class="inline-flex rounded-full bg-gray-100 p-1" role="tablist" aria-label="أقسام إدارة المنصة">
          @for (tab of tabs; track tab.key) {
            <button
              type="button"
              role="tab"
              [attr.aria-selected]="activeTab() === tab.key"
              [class]="pillTabClass(tab.key)"
              (click)="setTab(tab.key)"
            >
              <lucide-icon [img]="tab.icon" [size]="15" />
              {{ tab.label }}
              <span class="rounded-full px-1.5 py-0.5 text-[11px] font-bold data-mono" [class]="activeTab() === tab.key ? 'bg-white/90 text-gray-800' : 'bg-white text-gray-600'">
                {{ tabCount(tab.key) }}
              </span>
            </button>
          }
        </div>
        @if (activeTab() === 'tenants') {
          <p class="text-xs text-gray-500">
            <span class="hidden sm:inline">بحث سريع في السجل — </span>
            <span class="data-mono">{{ filteredTenants().length }} / {{ tenants().length }}</span>
          </p>
        }
      </div>

      @switch (activeTab()) {
        @case ('tenants') {
          <app-card>
            <!-- Tenants toolbar — search + status filter + provision -->
            <div class="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
              <div class="flex flex-1 flex-col gap-3 sm:flex-row sm:items-center">
                <div class="relative min-w-0 flex-1 sm:max-w-xs">
                  <lucide-icon [img]="searchIcon" [size]="16" class="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-gray-400" />
                  <input
                    type="search"
                    placeholder="بحث بالاسم أو المعرّف..."
                    [value]="tenantSearch()"
                    (input)="tenantSearch.set($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white py-2 pe-3 ps-9 text-sm outline-none placeholder:text-gray-400 focus:border-gold focus:ring-2 focus:ring-gold/30"
                  />
                </div>
                <select
                  [value]="tenantStatusFilter()"
                  (change)="tenantStatusFilter.set($any($event.target).value)"
                  class="rounded-input border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                >
                  <option value="">كل الحالات</option>
                  <option value="active">نشط</option>
                  <option value="trial">تجريبي</option>
                  <option value="cancelled">ملغى</option>
                  <option value="pending">قيد الانتظار</option>
                </select>
              </div>
              <div class="flex items-center gap-2">
                <span class="hidden text-xs text-gray-500 data-mono sm:inline">{{ filteredTenants().length }} نتيجة</span>
                <app-button icon="plus" (clicked)="openProvisionDialog()">مستأجر جديد</app-button>
              </div>
            </div>

            <div class="mb-2 flex items-center gap-2">
              <h2 class="text-sm font-semibold text-gray-700">المستأجرون</h2>
              <span class="rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-600 data-mono">{{ tenants().length }} مستأجر</span>
              @if (tenantSearch() || tenantStatusFilter()) {
                <button type="button" class="text-xs font-medium text-gold hover:text-amber-700" (click)="tenantSearch.set(''); tenantStatusFilter.set('')">مسح الفلتر</button>
              }
            </div>

            @if (tenantsError(); as err) {
              <app-empty-state
                icon="alert-circle"
                title="تعذّر تحميل المستأجرين"
                [description]="err.detail ?? err.title ?? ''"
              >
                <app-retry-button (retry)="reloadTenants()" />
              </app-empty-state>
            } @else {
              <app-table
                [columns]="tenantColumns()"
                [rows]="tenants()"
                [loading]="tenantsLoading()"
                emptyIcon="server"
                emptyTitle="لا يوجد مستأجرون"
                emptyDescription="المستأجرون المسجلون سيظهرون هنا. استخدم البحث أو غيّر الفلتر."
              >
                <ng-template #tenantKeyCell let-row>
                  <div class="flex flex-col gap-1">
                    <span class="inline-flex w-fit items-center gap-1.5 rounded-full border border-amber-200 bg-amber-50 px-2.5 py-0.5 text-xs font-medium text-amber-800 data-mono">
                      <lucide-icon [img]="globeIcon" [size]="12" />
                      {{ row.key }}
                    </span>
                    <span class="text-[11px] text-gray-400 data-mono" dir="ltr">{{ row.key }}.goldstore.app</span>
                  </div>
                </ng-template>
                <ng-template #tenantNameCell let-row>
                  <div class="flex items-center gap-2.5">
                    <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-gold-container to-amber-100 text-xs font-bold text-amber-800">
                      {{ row.name.charAt(0) }}
                    </span>
                    <span class="font-medium text-gray-900">{{ row.name }}</span>
                  </div>
                </ng-template>
                <ng-template #tenantStatusCell let-row>
                  <span class="inline-flex items-center gap-1.5">
                    <span class="h-1.5 w-1.5 rounded-full" [class]="statusDotClass(row.status)"></span>
                    <app-badge [variant]="statusVariant(row.status)">{{ statusLabel(row.status) }}</app-badge>
                  </span>
                </ng-template>
                <ng-template #tenantActionsCell let-row>
                  <div class="flex items-center justify-center gap-1">
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800"
                      title="تفاصيل الاشتراك"
                      [attr.aria-label]="'تفاصيل اشتراك ' + row.name"
                      (click)="openSubscriptionDetails(row)"
                    >
                      <lucide-icon [img]="eyeIcon" [size]="16" />
                    </button>
                    <button
                      type="button"
                      class="rounded-md p-1.5 text-gray-500 transition-colors hover:bg-gold-container/40 hover:text-gray-800"
                      title="تغيير الحالة"
                      [attr.aria-label]="'تغيير حالة ' + row.name"
                      (click)="openStatusDialog(row)"
                    >
                      <lucide-icon [img]="pencilIcon" [size]="16" />
                    </button>
                  </div>
                </ng-template>
              </app-table>
            }

            @if (saveError(); as err) {
              <p class="mt-4 rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
                {{ saveErrorMessage(err) }}
              </p>
            }
          </app-card>

          <!-- Subscription drawer — vault card -->
          @if (selectedSubscriptionTenant(); as selTenant) {
            <app-card>
              <div class="mb-4 flex flex-wrap items-center justify-between gap-3 border-b border-gray-100 pb-4">
                <div class="flex items-center gap-3">
                  <span class="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-gold to-amber-600 text-white shadow-sm">
                    <lucide-icon [img]="awardIcon" [size]="18" />
                  </span>
                  <div>
                    <h3 class="text-sm font-semibold text-gray-900">{{ selTenant.name }}</h3>
                    <p class="text-xs text-gray-500 data-mono">{{ selTenant.key }}.goldstore.app — {{ selTenant.key }}</p>
                  </div>
                </div>
                <div class="flex items-center gap-2">
                  <app-button variant="secondary" icon="refresh-cw" [loading]="subscriptionLoading()" (clicked)="reloadSubscription()">تحديث الاشتراك</app-button>
                  <app-button icon="sparkles" (clicked)="openRenewDialog(selTenant)">تجديد / تغيير الخطة</app-button>
                  <button type="button" class="rounded-md p-1.5 text-gray-500 hover:bg-gray-100" (click)="closeSubscriptionDetails()" aria-label="إغلاق">
                    <lucide-icon [img]="closeIcon" [size]="16" />
                  </button>
                </div>
              </div>

              @if (subscriptionError(); as err) {
                <app-empty-state icon="alert-circle" title="تعذّر تحميل الاشتراك" [description]="err.detail ?? err.title ?? ''">
                  <app-retry-button (retry)="reloadSubscription()" />
                </app-empty-state>
              } @else if (subscriptionLoading()) {
                <div class="space-y-3">
                  <div class="h-6 w-32 animate-pulse rounded-full bg-gray-100"></div>
                  <div class="h-24 animate-pulse rounded-lg bg-gray-100"></div>
                </div>
              } @else if (subscription(); as sub) {
                <div class="space-y-4">
                  <div class="flex flex-wrap items-center gap-2">
                    <app-badge [variant]="subscriptionStatusVariant(sub.status)">{{ subscriptionStatusLabel(sub.status) }}</app-badge>
                    <span class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium" [class]="daysRemainingClass(sub)">
                      <lucide-icon [img]="calendarIcon" [size]="12" />
                      {{ daysRemainingLabel(sub) }}
                    </span>
                    <span class="rounded-full border border-amber-200 bg-amber-50 px-3 py-1 text-xs font-medium text-amber-800 data-mono">{{ sub.plan.name }} — {{ sub.plan.key }}</span>
                  </div>
                  <!-- timeline bar -->
                  <div class="h-1.5 overflow-hidden rounded-full bg-gray-100">
                    <div class="h-full rounded-full bg-gradient-to-r from-gold to-amber-500 transition-all" [style.width.%]="subscriptionProgress(sub)"></div>
                  </div>
                  <div class="grid gap-3 sm:grid-cols-3 text-sm">
                    <div class="rounded-lg border border-gray-200 bg-gray-50/50 p-3">
                      <p class="flex items-center gap-1.5 text-xs text-gray-500"><lucide-icon [img]="calendarIcon" [size]="12" />دورة الفوترة</p>
                      <p class="mt-1 font-medium">{{ billingCycleLabel(sub.billingCycle) }}</p>
                    </div>
                    <div class="rounded-lg border border-gray-200 bg-gray-50/50 p-3">
                      <p class="flex items-center gap-1.5 text-xs text-gray-500"><lucide-icon [img]="activityIcon" [size]="12" />بداية الاشتراك</p>
                      <p class="mt-1 font-medium data-mono" dir="ltr">{{ sub.startsAtUtc }}</p>
                    </div>
                    <div class="rounded-lg border border-gray-200 bg-gray-50/50 p-3">
                      <p class="flex items-center gap-1.5 text-xs text-gray-500"><lucide-icon [img]="activityIcon" [size]="12" />نهاية الاشتراك</p>
                      <p class="mt-1 font-medium data-mono" dir="ltr">{{ sub.endsAtUtc }}</p>
                    </div>
                  </div>
                  <div class="flex flex-wrap gap-2 text-xs">
                    <span class="inline-flex items-center gap-1.5 rounded-full bg-gray-100 px-3 py-1.5 data-mono"><lucide-icon [img]="usersIcon" [size]="12" />مستخدمون: {{ formatLimit(sub.plan.maximumActiveUsers) }}</span>
                    <span class="inline-flex items-center gap-1.5 rounded-full bg-gray-100 px-3 py-1.5 data-mono"><lucide-icon [img]="receiptIcon" [size]="12" />فواتير/فترة: {{ formatLimit(sub.plan.maximumPostedInvoicesPerPeriod) }}</span>
                    <span class="inline-flex items-center gap-1.5 rounded-full bg-gray-100 px-3 py-1.5 data-mono"><lucide-icon [img]="buildingIcon" [size]="12" />فروع: {{ formatLimit(sub.plan.maximumActiveBranches) }}</span>
                    <span class="inline-flex items-center gap-1.5 rounded-full bg-gray-100 px-3 py-1.5 data-mono"><lucide-icon [img]="databaseIcon" [size]="12" />تخزين: {{ formatBytes(sub.plan.maximumStorageBytes) }}</span>
                  </div>
                </div>
              }
            </app-card>
          }
        }

        @case ('reconciliation') {
          <app-card>
            <div class="mb-4 flex flex-col gap-4">
              <div class="flex items-center gap-2">
                <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-gray-900 text-white"><lucide-icon [img]="databaseIcon" [size]="16" /></span>
                <div>
                  <h2 class="text-sm font-semibold text-gray-900">المطابقة المحاسبية</h2>
                  <p class="text-xs text-gray-500">تحقق من قيود الملكية والأرصدة المحسوبة عبر جميع المستأجرين</p>
                </div>
              </div>
              <div class="flex flex-wrap items-end gap-3 rounded-lg border border-gray-200 bg-gray-50/70 p-4">
                <label class="block min-w-64 flex-1">
                  <span class="mb-2 block text-xs font-medium tracking-widest text-gray-600">المستأجر المستهدف</span>
                  <select
                    id="recon-tenant"
                    [value]="selectedTenantId()"
                    (change)="selectedTenantId.set($any($event.target).value)"
                    class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                  >
                    <option value="">جميع المستأجرين — فحص شامل</option>
                    @for (tenant of tenants(); track tenant.id) {
                      <option [value]="tenant.id">{{ tenant.name }} — {{ tenant.key }}</option>
                    }
                  </select>
                </label>
                <app-button icon="search" [loading]="reconciliationLoading()" (clicked)="runReconciliation()">
                  تشغيل المطابقة
                </app-button>
                @if (reconciliation(); as recon) {
                  <span class="rounded-full bg-white px-3 py-1 text-xs text-gray-600 data-mono shadow-sm">
                    تم التوليد: {{ recon.generatedAtUtc }}
                  </span>
                }
              </div>
            </div>

            @if (reconciliationError(); as err) {
              <app-empty-state
                icon="alert-circle"
                title="تعذّرت المطابقة"
                [description]="err.detail ?? err.title ?? ''"
              >
                <app-retry-button (retry)="runReconciliation()" />
              </app-empty-state>
            } @else if (reconciliationLoading()) {
              <div class="space-y-3">
                <div class="h-20 animate-pulse rounded-lg bg-gray-100"></div>
                <div class="h-40 animate-pulse rounded-lg bg-gray-100"></div>
              </div>
            } @else if (!reconciliation()) {
              <app-empty-state
                icon="search"
                title="لم يتم تشغيل المطابقة بعد"
                description="اختر مستأجراً (أو جميعهم) ثم اضغط تشغيل المطابقة لعرض الانتهاكات والأرصدة المحسوبة من القيود الخام."
              />
            } @else {
              @let recon = reconciliation()!;
              <!-- Anomalies banner -->
              <section class="mb-6">
                <h3 class="mb-3 flex items-center gap-2 text-sm font-semibold text-gray-700">
                  <lucide-icon [img]="alertIcon" [size]="16" class="text-amber-600" />
                  الانتهاكات
                  <span class="rounded-full bg-gray-100 px-2 py-0.5 text-xs data-mono">{{ recon.anomalies.length }}</span>
                </h3>
                @if (recon.anomalies.length === 0) {
                  <div class="flex items-center gap-3 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                    <span class="flex h-8 w-8 items-center justify-center rounded-full bg-emerald-500 text-white"><lucide-icon [img]="checkIcon" [size]="16" /></span>
                    <div>
                      <p class="font-medium">لا توجد انتهاكات — جميع الصفوف تملك مستأجراً صحيحاً.</p>
                      <p class="text-xs text-emerald-700/80">سلامة قيود الملكية مؤكدة عبر جميع الجداول.</p>
                    </div>
                  </div>
                } @else {
                  <div class="overflow-hidden rounded-lg border border-red-200">
                    <app-table
                      [columns]="anomalyColumns()"
                      [rows]="recon.anomalies"
                      emptyIcon="alert-circle"
                      emptyTitle="لا توجد انتهاكات"
                    />
                  </div>
                }
              </section>

              <!-- Per-tenant vault cards -->
              @for (block of recon.tenants; track block.tenantId) {
                <section class="mb-5 overflow-hidden rounded-xl border border-gray-200 bg-card shadow-sm">
                  <div class="flex flex-wrap items-center justify-between gap-3 bg-gradient-to-r from-gray-50 to-white px-5 py-4">
                    <div class="flex items-center gap-3">
                      <span class="flex h-10 w-10 items-center justify-center rounded-lg bg-gray-900 text-white text-xs font-bold">{{ block.name.charAt(0) }}</span>
                      <div>
                        <h3 class="text-sm font-semibold text-gray-900">{{ block.name }} — {{ block.key }}</h3>
                        <p class="flex items-center gap-2 text-xs text-gray-500">
                          <app-badge [variant]="statusVariant(block.status)" class="me-1">
                            {{ statusLabel(block.status) }}
                          </app-badge>
                          <span class="inline-flex items-center gap-1 data-mono"><lucide-icon [img]="sparklesIcon" [size]="12" class="text-gold" />إجمالي مكافئ 21K: {{ block.totalEquivalent21K }} غ</span>
                        </p>
                      </div>
                    </div>
                    <span class="rounded-full bg-gray-100 px-2.5 py-1 text-[11px] text-gray-500 data-mono">{{ block.tenantId }}</span>
                  </div>

                  <div class="space-y-3 px-5 py-4">
                    <!-- Row counts -->
                    <details open class="group">
                      <summary class="flex cursor-pointer list-none items-center gap-2 text-xs font-semibold text-gray-700">
                        <lucide-icon [img]="layersIcon" [size]="14" class="text-gray-400 group-open:rotate-0 -rotate-90 transition-transform" />
                        عدد الصفوف حسب الكيان — {{ block.rowCounts.length }} كيانات
                      </summary>
                      <div class="mt-3 flex flex-wrap gap-2">
                        @for (row of block.rowCounts; track row.entity) {
                          <span class="inline-flex items-center gap-1.5 rounded-full border border-gray-200 bg-white px-3 py-1 text-xs shadow-sm data-mono">
                            <span class="h-1.5 w-1.5 rounded-full bg-gold"></span>
                            {{ row.entity }}: {{ row.count }}
                          </span>
                        }
                      </div>
                    </details>

                    <!-- Gold stock -->
                    @if (block.goldStock.length > 0) {
                      <details class="group">
                        <summary class="flex cursor-pointer list-none items-center gap-2 text-xs font-semibold text-gray-700">
                          <lucide-icon [img]="coinsIcon" [size]="14" class="text-amber-600" />
                          المخزون الذهبي — {{ block.goldStock.length }} عيارات
                        </summary>
                        <div class="mt-3 overflow-hidden rounded-lg border border-amber-100">
                          <table class="w-full text-xs">
                            <thead>
                              <tr class="border-b bg-amber-50/60 text-amber-800">
                                <th class="px-3 py-2 text-start font-semibold">العيار</th>
                                <th class="px-3 py-2 text-start font-semibold">الوزن (غ)</th>
                                <th class="px-3 py-2 text-start font-semibold">مكافئ 21K</th>
                              </tr>
                            </thead>
                            <tbody>
                              @for (balance of block.goldStock; track balance.karat) {
                                <tr class="border-b last:border-0 hover:bg-amber-50/30">
                                  <td class="px-3 py-2 data-mono font-medium">{{ balance.karat }}K</td>
                                  <td class="px-3 py-2 data-mono">{{ balance.weightGrams }}</td>
                                  <td class="px-3 py-2 data-mono font-bold text-amber-700">{{ balance.equivalent21K }}</td>
                                </tr>
                              }
                            </tbody>
                          </table>
                        </div>
                      </details>
                    }

                    <!-- Financial totals -->
                    @if (block.financialTotals.length > 0) {
                      <details class="group">
                        <summary class="flex cursor-pointer list-none items-center gap-2 text-xs font-semibold text-gray-700">
                          <lucide-icon [img]="walletIcon" [size]="14" class="text-emerald-600" />
                          الأرصدة المالية (محسوبة)
                        </summary>
                        <div class="mt-3 overflow-hidden rounded-lg border border-gray-200">
                          <table class="w-full text-xs">
                            <thead>
                              <tr class="border-b bg-gray-50 text-gray-600">
                                <th class="px-3 py-2 text-start">العملة</th>
                                <th class="px-3 py-2 text-start">وارد</th>
                                <th class="px-3 py-2 text-start">صادر</th>
                                <th class="px-3 py-2 text-start">الصافي</th>
                              </tr>
                            </thead>
                            <tbody>
                              @for (total of block.financialTotals; track total.currency) {
                                <tr class="border-b last:border-0 hover:bg-gray-50">
                                  <td class="px-3 py-2 data-mono font-medium">{{ total.currency }}</td>
                                  <td class="px-3 py-2 data-mono text-emerald-700">{{ total.totalInflow }}</td>
                                  <td class="px-3 py-2 data-mono text-red-600">{{ total.totalOutflow }}</td>
                                  <td class="px-3 py-2 data-mono font-bold">{{ total.net }}</td>
                                </tr>
                              }
                            </tbody>
                          </table>
                        </div>
                        @if (block.financialBalances.length > 0) {
                          <div class="mt-2 flex flex-wrap gap-2">
                            @for (balance of block.financialBalances; track balance.accountId) {
                              <span class="rounded-full bg-emerald-50 px-3 py-1 text-xs text-emerald-800 data-mono border border-emerald-100">
                                {{ balance.accountName }} ({{ balance.currency }}): {{ balance.balance }}
                              </span>
                            }
                          </div>
                        }
                      </details>
                    }

                    <!-- Debt totals -->
                    @if (block.debtTotals.length > 0) {
                      <details class="group">
                        <summary class="flex cursor-pointer list-none items-center gap-2 text-xs font-semibold text-gray-700">
                          <lucide-icon [img]="receiptIcon" [size]="14" class="text-gray-500" />
                          الذمم
                        </summary>
                        <div class="mt-3 overflow-hidden rounded-lg border border-gray-200">
                          <table class="w-full text-xs">
                            <thead>
                              <tr class="border-b bg-gray-50 text-gray-600">
                                <th class="px-3 py-2 text-start">العملة</th>
                                <th class="px-3 py-2 text-start">لنا</th>
                                <th class="px-3 py-2 text-start">علينا</th>
                                <th class="px-3 py-2 text-start">الصافي</th>
                              </tr>
                            </thead>
                            <tbody>
                              @for (total of block.debtTotals; track total.currency) {
                                <tr class="border-b last:border-0 hover:bg-gray-50">
                                  <td class="px-3 py-2 data-mono">{{ total.currency }}</td>
                                  <td class="px-3 py-2 data-mono">{{ total.totalReceivable }}</td>
                                  <td class="px-3 py-2 data-mono">{{ total.totalPayable }}</td>
                                  <td class="px-3 py-2 data-mono font-bold">{{ total.net }}</td>
                                </tr>
                              }
                            </tbody>
                          </table>
                        </div>
                      </details>
                    }

                    <!-- Supplier balances -->
                    @if (
                      block.supplierGoldBalances.length > 0 ||
                      block.supplierManufacturingBalances.length > 0
                    ) {
                      <details class="group">
                        <summary class="flex cursor-pointer list-none items-center gap-2 text-xs font-semibold text-gray-700">
                          <lucide-icon [img]="truckIcon" [size]="14" />
                          أرصدة الموردين
                        </summary>
                        <div class="mt-3 flex flex-wrap gap-2">
                          @for (balance of block.supplierGoldBalances; track balance.supplierId + '-' + balance.karat) {
                            <span class="rounded-full bg-amber-50 border border-amber-200 px-3 py-1 text-xs text-amber-800 data-mono">
                              {{ balance.supplierName }} — {{ balance.karat }}K: {{ balance.netWeight }} غ صافي
                            </span>
                          }
                          @for (
                            balance of block.supplierManufacturingBalances;
                            track balance.supplierId + '-' + balance.currency
                          ) {
                            <span class="rounded-full bg-gray-100 px-3 py-1 text-xs text-gray-700 data-mono">
                              {{ balance.supplierName }} ({{ balance.currency }}): {{ balance.netAmount }}
                            </span>
                          }
                        </div>
                      </details>
                    }
                  </div>
                </section>
              }
            }
          </app-card>
        }

        @case ('plans') {
          <!-- Plans — card grid + table for power users -->
          <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            @for (plan of plans(); track plan.id) {
              <div class="group relative overflow-hidden rounded-xl border bg-card p-5 shadow-sm transition hover:shadow-md" [class]="plan.isActive ? 'border-emerald-200' : 'border-gray-200 opacity-80'">
                <div class="absolute inset-x-0 top-0 h-1" [class]="plan.isActive ? 'bg-emerald-500' : 'bg-gray-300'"></div>
                <div class="flex items-start justify-between gap-2">
                  <div class="min-w-0">
                    <h3 class="truncate text-sm font-bold text-gray-900">{{ plan.name }}</h3>
                    <p class="text-xs text-gray-500 data-mono">{{ plan.key }}</p>
                  </div>
                  <div class="flex flex-col items-end gap-1">
                    <div class="flex items-center gap-1">
                      <button type="button" class="rounded-md p-1.5 text-gray-400 hover:bg-white hover:text-gray-700 border border-transparent hover:border-gray-200" title="تعديل الخطة" [attr.aria-label]="'تعديل ' + plan.name" (click)="openEditPlanDialog(plan)">
                        <lucide-icon [img]="pencilIcon" [size]="14" />
                      </button>
                      <app-badge [variant]="plan.isActive ? 'success' : 'neutral'">{{ plan.isActive ? 'نشط' : 'غير نشط' }}</app-badge>
                    </div>
                    @if (plan.isTrial) {
                      <span class="rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-bold text-amber-800 border border-amber-200">تجريبية</span>
                    }
                  </div>
                </div>
                <div class="mt-3 flex flex-wrap items-center gap-1.5 text-xs">
                  <span class="rounded-full bg-gray-900 px-2.5 py-1 text-white data-mono">{{ durationLabel(plan.durationInMonths) }}</span>
                  <span class="rounded-full bg-emerald-50 px-2.5 py-1 text-emerald-800 border border-emerald-200 data-mono">
                    {{ plan.price.toFixed(2) }}
                    @if (plan.discountPercent !== null && plan.discountPercent !== undefined) {
                      <span> -{{ plan.discountPercent }}% = {{ plan.effectivePrice.toFixed(2) }}</span>
                    }
                  </span>
                  @if (plan.isTrial) {
                    <span class="rounded-full bg-gray-100 px-2 py-1 text-gray-600">مجانية</span>
                  }
                </div>
                <div class="mt-4 grid grid-cols-2 gap-2 text-xs">
                  <span class="rounded-lg bg-gray-50 px-2.5 py-2 data-mono border border-gray-100"><span class="block text-[11px] text-gray-500">مستخدمون</span><span class="font-semibold">{{ formatLimit(plan.maximumActiveUsers) }}</span></span>
                  <span class="rounded-lg bg-gray-50 px-2.5 py-2 data-mono border border-gray-100"><span class="block text-[11px] text-gray-500">فواتير/فترة</span><span class="font-semibold">{{ formatLimit(plan.maximumPostedInvoicesPerPeriod) }}</span></span>
                  <span class="rounded-lg bg-gray-50 px-2.5 py-2 data-mono border border-gray-100"><span class="block text-[11px] text-gray-500">فروع</span><span class="font-semibold">{{ formatLimit(plan.maximumActiveBranches) }}</span></span>
                  <span class="rounded-lg bg-gray-50 px-2.5 py-2 data-mono border border-gray-100"><span class="block text-[11px] text-gray-500">تخزين</span><span class="font-semibold">{{ formatBytes(plan.maximumStorageBytes) }}</span></span>
                </div>
              </div>
            }
          </div>

          <app-card>
            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
              <div class="flex items-center gap-2">
                <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-amber-500 text-white"><lucide-icon [img]="tagsIcon" [size]="16" /></span>
                <div>
                  <h2 class="text-sm font-semibold text-gray-900">خطط الاشتراك — جدول تفصيلي</h2>
                  <p class="text-xs text-gray-500 data-mono">{{ plans().length }} خطة في الكتالوج</p>
                </div>
              </div>
              <app-button icon="plus" (clicked)="openCreatePlanDialog()">خطة جديدة</app-button>
            </div>

            @if (plansError(); as err) {
              <app-empty-state icon="alert-circle" title="تعذّر تحميل الخطط" [description]="err.detail ?? err.title ?? ''">
                <app-retry-button (retry)="reloadPlans()" />
              </app-empty-state>
            } @else {
              <app-table
                [columns]="planColumns()"
                [rows]="plans()"
                [loading]="plansLoading()"
                emptyIcon="tags"
                emptyTitle="لا توجد خطط"
                emptyDescription="أنشئ خطة اشتراك جديدة للبدء. الخطط تحدد حدود المستخدمين والفروع والتخزين."
              >
                <ng-template #planStatusCell let-row>
                  <app-badge [variant]="row.isActive ? 'success' : 'neutral'">{{ row.isActive ? 'نشط' : 'غير نشط' }}</app-badge>
                </ng-template>
                <ng-template #planActionsCell let-row>
                  <div class="flex items-center justify-center">
                    <button type="button" class="rounded-md p-1.5 text-gray-500 hover:bg-gold-container/40 hover:text-gray-800" title="تعديل الخطة" [attr.aria-label]="'تعديل ' + row.name" (click)="openEditPlanDialog(row)">
                      <lucide-icon [img]="pencilIcon" [size]="14" />
                    </button>
                  </div>
                </ng-template>
              </app-table>
            }

            @if (saveError(); as err) {
              <p class="mt-4 rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
                {{ saveErrorMessage(err) }}
              </p>
            }
          </app-card>
        }
      }

      <app-host-update-status-dialog
        [open]="statusDialogOpen()"
        [tenant]="editingTenant()"
        (openChange)="closeStatusDialog($event)"
        (saved)="onStatusSaved()"
      />

      <app-provision-tenant-dialog
        [open]="provisionDialogOpen()"
        (openChange)="closeProvisionDialog($event)"
        (saved)="onProvisionSaved()"
      />

      <app-create-plan-dialog
        [open]="createPlanDialogOpen()"
        [plan]="editingPlan()"
        (openChange)="closeCreatePlanDialog($event)"
        (saved)="onPlanCreated()"
      />

      <app-renew-subscription-dialog
        [open]="renewDialogOpen()"
        [tenant]="selectedRenewTenant()"
        (openChange)="closeRenewDialog($event)"
        (saved)="onRenewSaved()"
      />
    </main>
  `,
})
export class HostAdminPage {
  private readonly store = inject(HostStore);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  readonly tenants = computed(() => this.store.tenants() ?? []);
  readonly tenantsLoading = this.store.tenantsLoading;
  readonly tenantsError = this.store.tenantsError;
  readonly reconciliation = this.store.reconciliation;
  readonly reconciliationLoading = this.store.reconciliationLoading;
  readonly reconciliationError = this.store.reconciliationError;
  readonly plans = computed(() => this.store.plans() ?? []);
  readonly plansLoading = this.store.plansLoading;
  readonly plansError = this.store.plansError;
  readonly subscription = this.store.subscription;
  readonly subscriptionLoading = this.store.subscriptionLoading;
  readonly subscriptionError = this.store.subscriptionError;
  readonly saveError = this.store.saveError;

  readonly activeTab = signal<HostTab>('tenants');
  private readonly loadedTabs = new Set<HostTab>(['tenants']);

  readonly statusDialogOpen = signal(false);
  readonly editingTenant = signal<TenantSummaryResponse | null>(null);
  readonly provisionDialogOpen = signal(false);
  readonly createPlanDialogOpen = signal(false);
  readonly editingPlan = signal<SubscriptionPlanResponse | null>(null);
  readonly renewDialogOpen = signal(false);
  readonly selectedSubscriptionTenant = signal<TenantSummaryResponse | null>(null);
  readonly selectedRenewTenant = signal<TenantSummaryResponse | null>(null);

  readonly selectedTenantId = signal('');
  readonly tenantSearch = signal('');
  readonly tenantStatusFilter = signal('');

  readonly checkIcon = resolveIcon('check-circle');
  readonly pencilIcon = resolveIcon('pencil');
  readonly eyeIcon = resolveIcon('eye');
  readonly closeIcon = resolveIcon('x');
  readonly logOutIcon = resolveIcon('log-out');
  readonly shieldIcon = resolveIcon('shield-check');
  readonly crownIcon = resolveIcon('crown');
  readonly layersIcon = resolveIcon('layers');
  readonly searchIcon = resolveIcon('search');
  readonly globeIcon = resolveIcon('globe');
  readonly awardIcon = resolveIcon('award');
  readonly calendarIcon = resolveIcon('calendar');
  readonly activityIcon = resolveIcon('activity');
  readonly usersIcon = resolveIcon('users');
  readonly receiptIcon = resolveIcon('receipt');
  readonly buildingIcon = resolveIcon('building-2');
  readonly databaseIcon = resolveIcon('database');
  readonly alertIcon = resolveIcon('alert-circle');
  readonly sparklesIcon = resolveIcon('sparkles');
  readonly coinsIcon = resolveIcon('coins');
  readonly walletIcon = resolveIcon('wallet');
  readonly truckIcon = resolveIcon('truck');
  readonly tagsIcon = resolveIcon('tags');

  readonly hostSigningOut = signal(false);

  readonly tabs: ReadonlyArray<{ key: HostTab; label: string; icon: ReturnType<typeof resolveIcon> }> = [
    { key: 'tenants', label: 'المستأجرون', icon: resolveIcon('server')! },
    { key: 'reconciliation', label: 'المطابقة', icon: resolveIcon('search')! },
    { key: 'plans', label: 'الاشتراكات', icon: resolveIcon('tags')! },
  ];

  readonly filteredTenants = computed(() => {
    const all = this.tenants();
    const q = this.tenantSearch().trim().toLowerCase();
    const status = this.tenantStatusFilter().trim().toLowerCase();
    return all.filter((t) => {
      const matchSearch = !q || t.name.toLowerCase().includes(q) || t.key.toLowerCase().includes(q);
      const matchStatus = !status || t.status.toLowerCase() === status;
      return matchSearch && matchStatus;
    });
  });

  readonly kpis = computed(() => {
    const tenants = this.tenants();
    const plans = this.plans();
    const active = tenants.filter((t) => t.status.toLowerCase() === 'active').length;
    const trial = tenants.filter((t) => t.status.toLowerCase() === 'trial').length;
    const cancelled = tenants.filter((t) => t.status.toLowerCase() === 'cancelled').length;
    return [
      {
        label: 'إجمالي المستأجرين',
        value: String(tenants.length),
        hint: `${active} نشط • ${trial} تجريبي`,
        icon: resolveIcon('building-2'),
        bg: 'bg-gradient-to-br from-gray-900 to-gray-700',
      },
      {
        label: 'نشط',
        value: String(active),
        hint: cancelled > 0 ? `${cancelled} ملغى` : 'جاهز للتشغيل',
        icon: resolveIcon('check-circle'),
        bg: 'bg-gradient-to-br from-emerald-500 to-emerald-600',
      },
      {
        label: 'تجريبي',
        value: String(trial),
        hint: 'بحاجة متابعة',
        icon: resolveIcon('zap'),
        bg: 'bg-gradient-to-br from-amber-500 to-gold',
      },
      {
        label: 'خطط الاشتراك',
        value: String(plans.length),
        hint: `${plans.filter((p) => p.isActive).length} نشطة`,
        icon: resolveIcon('layers'),
        bg: 'bg-gradient-to-br from-violet-500 to-indigo-600',
      },
    ];
  });

  private readonly tenantKeyCell =
    viewChild<TemplateRef<{ $implicit: TenantSummaryResponse }>>('tenantKeyCell');
  private readonly tenantNameCell =
    viewChild<TemplateRef<{ $implicit: TenantSummaryResponse }>>('tenantNameCell');
  private readonly tenantStatusCell =
    viewChild<TemplateRef<{ $implicit: TenantSummaryResponse }>>('tenantStatusCell');
  private readonly tenantActionsCell =
    viewChild<TemplateRef<{ $implicit: TenantSummaryResponse }>>('tenantActionsCell');
  private readonly planStatusCell =
    viewChild<TemplateRef<{ $implicit: SubscriptionPlanResponse }>>('planStatusCell');
  private readonly planActionsCell =
    viewChild<TemplateRef<{ $implicit: SubscriptionPlanResponse }>>('planActionsCell');

  readonly tenantColumns = computed<TableColumn<TenantSummaryResponse>[]>(() => [
    {
      key: 'name',
      header: 'المتجر',
      cell: (row) => row.name ?? '',
      cellTemplate: this.tenantNameCell(),
      sortable: true,
      sortValue: (row) => row.name ?? '',
    },
    {
      key: 'key',
      header: 'المعرّف والنطاق',
      cell: (row) => row.key ?? '',
      cellTemplate: this.tenantKeyCell(),
      sortable: true,
      sortValue: (row) => row.key ?? '',
    },
    {
      key: 'status',
      header: 'الحالة',
      cell: (row) => row.status ?? '',
      cellTemplate: this.tenantStatusCell(),
      sortable: true,
      sortValue: (row) => row.status ?? '',
    },
    {
      key: 'actions',
      header: 'إجراءات',
      cell: () => '',
      align: 'center',
      cellTemplate: this.tenantActionsCell(),
    },
  ]);

  readonly planColumns = computed<TableColumn<SubscriptionPlanResponse>[]>(() => [
    {
      key: 'name',
      header: 'الاسم',
      cell: (row) => row.name ?? '',
      sortable: true,
      sortValue: (row) => row.name ?? '',
    },
    {
      key: 'key',
      header: 'المفتاح',
      cell: (row) => row.key ?? '',
      sortable: true,
      sortValue: (row) => row.key ?? '',
      numeric: false,
    },
    {
      key: 'isTrial',
      header: 'تجريبية',
      cell: (row) => (row.isTrial ? 'نعم' : 'لا'),
      sortable: true,
      sortValue: (row) => (row.isTrial ? 1 : 0),
    },
    {
      key: 'duration',
      header: 'المدة',
      cell: (row) => this.durationLabel(row.durationInMonths),
      sortable: true,
      sortValue: (row) => row.durationInMonths ?? 0,
      numeric: true,
    },
    {
      key: 'price',
      header: 'السعر',
      cell: (row) => `${row.price.toFixed(2)}${row.discountPercent ? ` -${row.discountPercent}%` : ''}`,
      sortable: true,
      sortValue: (row) => row.effectivePrice ?? row.price ?? 0,
      numeric: true,
    },
    {
      key: 'maxUsers',
      header: 'مستخدمون',
      cell: (row) => this.formatLimit(row.maximumActiveUsers),
      sortable: true,
      sortValue: (row) => row.maximumActiveUsers ?? 999999,
      numeric: true,
    },
    {
      key: 'maxInvoices',
      header: 'فواتير/فترة',
      cell: (row) => this.formatLimit(row.maximumPostedInvoicesPerPeriod),
      sortable: true,
      sortValue: (row) => row.maximumPostedInvoicesPerPeriod ?? 999999,
      numeric: true,
    },
    {
      key: 'isActive',
      header: 'نشط',
      cell: (row) => (row.isActive ? 'نعم' : 'لا'),
      cellTemplate: this.planStatusCell(),
      sortable: true,
      sortValue: (row) => (row.isActive ? 1 : 0),
    },
    {
      key: 'actions',
      header: 'إجراءات',
      cell: () => '',
      align: 'center',
      cellTemplate: this.planActionsCell(),
    },
  ]);

  readonly anomalyColumns = computed<TableColumn<ReconciliationAnomaly>[]>(() => [
    { key: 'table', header: 'الجدول', cell: (row) => row.table },
    { key: 'tenantId', header: 'المستأجر', cell: (row) => row.tenantId ?? '—' },
    { key: 'count', header: 'العدد', cell: (row) => String(row.count), numeric: true },
    { key: 'message', header: 'الرسالة', cell: (row) => row.message },
  ]);

  constructor() {
    if (this.store.tenants() === null) {
      void this.store.loadTenants();
    }
  }

  pillTabClass(key: HostTab): string {
    const base =
      'inline-flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-medium transition-colors';
    return this.activeTab() === key
      ? `${base} bg-gray-900 text-white shadow-sm`
      : `${base} text-gray-600 hover:bg-white hover:text-gray-900`;
  }

  tabClass(key: HostTab): string {
    const base =
      'flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium transition-colors';
    return this.activeTab() === key
      ? `${base} border-gold text-gold`
      : `${base} border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700`;
  }

  tabCount(key: HostTab): string {
    if (key === 'tenants') return String(this.tenants().length);
    if (key === 'plans') return String(this.plans().length);
    const recon = this.reconciliation();
    return recon ? String(recon.tenants.length) : '—';
  }

  statusDotClass(status: string): string {
    const lower = status.toLowerCase();
    if (lower === 'active') return 'bg-emerald-500';
    if (lower === 'trial') return 'bg-amber-500';
    if (lower === 'cancelled') return 'bg-red-500';
    return 'bg-gray-400';
  }

  setTab(tab: HostTab): void {
    if (this.activeTab() === tab) return;
    this.activeTab.set(tab);
    if (!this.loadedTabs.has(tab)) {
      this.loadedTabs.add(tab);
      if (tab === 'plans') {
        void this.store.loadPlans();
      }
    }
  }

  refreshActiveTab(): void {
    if (this.activeTab() === 'tenants') {
      this.reloadTenants();
      if (this.selectedSubscriptionTenant() !== null) {
        void this.reloadSubscription();
      }
    } else if (this.activeTab() === 'plans') {
      this.reloadPlans();
    } else {
      this.runReconciliation();
    }
  }

  reloadTenants(): void {
    void this.store.loadTenants();
  }

  reloadPlans(): void {
    void this.store.loadPlans();
  }

  // Provision
  openProvisionDialog(): void {
    this.provisionDialogOpen.set(true);
  }

  closeProvisionDialog(open: boolean): void {
    this.provisionDialogOpen.set(open);
  }

  onProvisionSaved(): void {
    void this.store.loadTenants();
  }

  // Create / Edit plan
  openCreatePlanDialog(): void {
    this.editingPlan.set(null);
    this.createPlanDialogOpen.set(true);
  }

  openEditPlanDialog(plan: SubscriptionPlanResponse): void {
    this.editingPlan.set(plan);
    this.createPlanDialogOpen.set(true);
  }

  closeCreatePlanDialog(open: boolean): void {
    this.createPlanDialogOpen.set(open);
    if (!open) this.editingPlan.set(null);
  }

  onPlanCreated(): void {
    void this.store.loadPlans();
    this.editingPlan.set(null);
  }

  // Status
  openStatusDialog(row: TenantSummaryResponse): void {
    this.editingTenant.set(row);
    this.statusDialogOpen.set(true);
  }

  closeStatusDialog(open: boolean): void {
    this.statusDialogOpen.set(open);
    if (!open) this.editingTenant.set(null);
  }

  onStatusSaved(): void {
    void this.store.loadTenants();
    if (this.store.reconciliation() !== null) {
      void this.store.loadReconciliation(this.selectedTenantId() || null);
    }
    if (this.selectedSubscriptionTenant() !== null) {
      void this.reloadSubscription();
    }
  }

  // Subscription details
  async openSubscriptionDetails(row: TenantSummaryResponse): Promise<void> {
    this.selectedSubscriptionTenant.set(row);
    await this.store.loadSubscription(row.id);
  }

  closeSubscriptionDetails(): void {
    this.selectedSubscriptionTenant.set(null);
    this.store.clearSubscription();
  }

  async reloadSubscription(): Promise<void> {
    const tenant = this.selectedSubscriptionTenant();
    if (tenant !== null) {
      await this.store.loadSubscription(tenant.id);
    }
  }

  openRenewDialog(row: TenantSummaryResponse): void {
    if (this.selectedSubscriptionTenant()?.id !== row.id) {
      void this.openSubscriptionDetails(row).then(() => {
        this.selectedRenewTenant.set(row);
        this.renewDialogOpen.set(true);
      });
    } else {
      this.selectedRenewTenant.set(row);
      this.renewDialogOpen.set(true);
    }
  }

  closeRenewDialog(open: boolean): void {
    this.renewDialogOpen.set(open);
    if (!open) this.selectedRenewTenant.set(null);
  }

  async onRenewSaved(): Promise<void> {
    await this.reloadSubscription();
  }

  runReconciliation(): void {
    void this.store.loadReconciliation(this.selectedTenantId() || null);
  }

  async signOut(): Promise<void> {
    if (this.hostSigningOut()) return;
    this.hostSigningOut.set(true);
    try {
      await this.auth.hostLogout();
    } finally {
      this.hostSigningOut.set(false);
      await this.router.navigateByUrl('/host/login');
    }
  }

  statusVariant(status: string): 'success' | 'warning' | 'error' | 'neutral' {
    const lower = status.toLowerCase();
    if (lower === 'active') return 'success';
    if (lower === 'trial') return 'warning';
    if (lower === 'cancelled') return 'error';
    return 'neutral';
  }

  statusLabel(status: string): string {
    const lower = status.toLowerCase();
    if (lower === 'active') return 'نشط';
    if (lower === 'trial') return 'تجريبي';
    if (lower === 'cancelled') return 'ملغى';
    if (lower === 'pending') return 'قيد الانتظار';
    return status;
  }

  subscriptionStatusVariant(status: string): 'success' | 'warning' | 'error' | 'neutral' {
    const lower = status.toLowerCase();
    if (lower === 'active') return 'success';
    if (lower === 'pastdue') return 'warning';
    if (lower === 'cancelled') return 'error';
    if (lower === 'expired') return 'error';
    return 'neutral';
  }

  subscriptionStatusLabel(status: string): string {
    const lower = status.toLowerCase();
    if (lower === 'active') return 'نشط';
    if (lower === 'pastdue') return 'متأخر';
    if (lower === 'cancelled') return 'ملغى';
    if (lower === 'expired') return 'منتهي';
    return status;
  }

  billingCycleLabel(cycle: string): string {
    const lower = cycle.toLowerCase();
    if (lower === 'annual') return 'سنوي';
    if (lower === 'monthly') return 'شهري';
    return cycle;
  }

  formatLimit(value: number | null | undefined): string {
    if (value === null || value === undefined) return 'غير محدود';
    return String(value);
  }

  formatBytes(value: number | null | undefined): string {
    if (value === null || value === undefined) return 'غير محدود';
    if (value < 1024) return `${value} بايت`;
    if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} ك.بايت`;
    return `${(value / (1024 * 1024)).toFixed(1)} م.بايت`;
  }

  durationLabel(months: number): string {
    switch (months) {
      case 1: return 'شهر';
      case 3: return '3 أشهر';
      case 6: return '6 أشهر';
      case 12: return 'سنة';
      case 24: return 'سنتان';
      default: return `${months} شهر`;
    }
  }

  daysRemainingLabel(sub: { endsAtUtc: string }): string {
    const ends = new Date(sub.endsAtUtc).getTime();
    const now = Date.now();
    const diff = ends - now;
    const days = Math.ceil(diff / (1000 * 60 * 60 * 24));
    if (Number.isNaN(days)) return '—';
    if (days > 0) return `متبقي ${days} يوم`;
    if (days === 0) return 'ينتهي اليوم';
    return `منتهي منذ ${Math.abs(days)} يوم`;
  }

  daysRemainingClass(sub: { endsAtUtc: string }): string {
    const ends = new Date(sub.endsAtUtc).getTime();
    const days = Math.ceil((ends - Date.now()) / (1000 * 60 * 60 * 24));
    if (days > 30) return 'bg-success/10 text-green-700';
    if (days > 7) return 'bg-gold-container/50 text-gray-700';
    if (days >= 0) return 'bg-warning/10 text-amber-700';
    return 'bg-error/10 text-red-700';
  }

  subscriptionProgress(sub: { startsAtUtc: string; endsAtUtc: string }): number {
    const start = new Date(sub.startsAtUtc).getTime();
    const end = new Date(sub.endsAtUtc).getTime();
    const now = Date.now();
    if (Number.isNaN(start) || Number.isNaN(end) || end <= start) return 50;
    const total = end - start;
    const elapsed = now - start;
    return Math.max(4, Math.min(100, Math.round((elapsed / total) * 100)));
  }

  saveErrorMessage(error: import('../../core/http/api-error').ApiError): string {
    const first = (() => {
      for (const messages of Object.values(error.validation ?? {})) {
        const msg = messages[0];
        if (msg !== undefined) return msg;
      }
      return null;
    })();
    return first ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }
}
