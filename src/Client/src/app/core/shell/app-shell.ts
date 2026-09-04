import { SlicePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LucideAngularModule, TrendingDown, TrendingUp } from 'lucide-angular';

import { AuthStore } from '../auth/auth-store';
import type { GoldPriceInfo } from '../gold-prices/gold-price-store';
import { GoldPriceStore } from '../gold-prices/gold-price-store';
import { buildNavItems, type NavItem } from '../navigation/nav-items';
import { PwaInstallButton } from '../pwa/install-button';
import { resolveIcon, Skeleton } from '../../shared/ui';
import { TenantBrand } from './tenant-brand';

/** Mobile-primary keys shown in the bottom tab bar (order matters). Fallback fills from navItems. */
const BOTTOM_PRIORITY: readonly string[] = ['dashboard', 'sales', 'purchases', 'inventory', 'finance'] as const;

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideAngularModule, TenantBrand, Skeleton, PwaInstallButton, SlicePipe],
  template: `
    <div class="flex h-dvh bg-surface" dir="rtl">
      <!-- Mobile overlay -->
      @if (drawerOpen()) {
        <div
          class="fixed inset-0 z-30 bg-black/40 backdrop-blur-[2px] lg:hidden"
          (click)="closeDrawer()"
          aria-hidden="true"
        ></div>
      }

      <!-- Sidebar: drawer on mobile/tablet, fixed on desktop -->
      <aside
        [class]="sidebarClass()"
        aria-label="القائمة الرئيسية"
        (touchstart)="onTouchStart($event)"
        (touchmove)="onTouchMove($event)"
        (touchend)="onTouchEnd($event)"
      >
        <!-- Drag handle (mobile) -->
        <div class="flex justify-center pt-2 pb-1 lg:hidden" aria-hidden="true">
          <span class="h-1.5 w-10 rounded-full bg-gray-300"></span>
        </div>

        <div class="flex items-center justify-between gap-3 px-5 py-3">
          <app-tenant-brand [tenantKey]="tenantKey()" />
          <button
            type="button"
            class="inline-flex h-9 w-9 items-center justify-center rounded-xl bg-gray-50 text-gray-500 hover:bg-gray-100 active:bg-gray-200 lg:hidden"
            aria-label="إغلاق القائمة"
            (click)="closeDrawer()"
          >
            <lucide-icon [img]="closeIcon" [size]="18" />
          </button>
        </div>

        <!-- User chip -->
        <div class="mx-3 mb-3 flex items-center gap-3 rounded-xl bg-gold-container/30 px-3 py-3">
          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gold text-sm font-bold text-white">
            {{ initial() }}
          </span>
          <div class="min-w-0 flex-1">
            <p class="truncate text-sm font-semibold text-gray-900" dir="ltr">{{ userEmail() ?? '—' }}</p>
            <p class="text-xs text-gray-600" data-mono>{{ tenantKey() ?? '' }}</p>
          </div>
          <span class="h-2 w-2 shrink-0 rounded-full bg-success"></span>
        </div>

        <nav class="flex-1 overflow-y-auto px-3 pb-3 overscroll-contain">
          @for (group of groupedNav(); track group.title) {
            <p class="mt-4 mb-2 px-3 text-[11px] font-bold tracking-widest text-gray-400">
              {{ group.title }}
            </p>
            <div class="space-y-1">
              @for (item of group.items; track item.key) {
                <a
                  [routerLink]="item.route"
                  routerLinkActive="nav-active"
                  #rla="routerLinkActive"
                  class="group relative flex items-center gap-3 rounded-xl px-3 py-3 text-sm text-gray-700 transition hover:bg-gold-container/40 active:bg-gold-container/60 min-h-[48px]"
                  [class.!bg-gold-container]="rla.isActive"
                  [class.!text-gray-900]="rla.isActive"
                  [class.font-semibold]="rla.isActive"
                  (click)="closeDrawer()"
                >
                  <span
                    class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl transition"
                    [class.bg-gold]="rla.isActive"
                    [class.text-white]="rla.isActive"
                    [class.bg-white]=" !rla.isActive"
                    [class.shadow-sm]=" !rla.isActive"
                  >
                    <lucide-icon [img]="resolveIcon(item.icon)" [size]="18" [strokeWidth]="rla.isActive ? 2.4 : 2" />
                  </span>
                  <span class="flex-1">{{ item.label }}</span>
                  @if (rla.isActive) {
                    <span class="h-2 w-2 rounded-full bg-gold"></span>
                  }
                </a>
              }
            </div>
          }
        </nav>

        <div class="border-t border-gray-200 p-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]">
          <div class="mb-2 flex items-center justify-between px-1 text-[11px] text-gray-400">
            <span class="flex items-center gap-1"><span class="material-symbols-outlined icon-fill text-[14px] leading-none" style="font-variation-settings:'FILL' 1">diamond</span> بريق</span>
            <span data-mono>v1</span>
          </div>
          <button
            type="button"
            class="flex w-full items-center justify-center gap-2 rounded-xl bg-error px-4 py-3 text-sm font-bold text-white shadow-sm transition hover:bg-red-600 active:bg-red-700 disabled:opacity-60 min-h-[48px]"
            [disabled]="signingOut()"
            (click)="signOut()"
            aria-label="تسجيل الخروج"
          >
            <lucide-icon [img]="logOutIcon" [size]="18" [strokeWidth]="2.2" class="shrink-0" />
            <span>تسجيل الخروج</span>
          </button>
        </div>
      </aside>

      <!-- Content column -->
      <div class="flex min-w-0 flex-1 flex-col lg:ms-64">
        <!-- Sticky app bar -->
        <header class="sticky top-0 z-20 flex h-[56px] shrink-0 items-center gap-2 border-b border-gray-200 bg-card/95 px-3 backdrop-blur supports-[backdrop-filter]:bg-card/90 sm:gap-3 sm:px-4 lg:px-6">
          <!-- Hamburger: mobile+tablet only -->
          <button
            type="button"
            class="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-gray-200 bg-card text-gray-700 shadow-sm hover:bg-gray-50 active:bg-gray-100 active:scale-[0.98] lg:hidden min-h-[40px] min-w-[40px] transition"
            aria-label="فتح القائمة"
            [attr.aria-expanded]="drawerOpen()"
            (click)="toggleDrawer()"
          >
            <lucide-icon [img]="menuIcon" [size]="20" />
          </button>

          <!-- Tenant badge: desktop full, mobile compact -->
          @if (tenantKey(); as key) {
            <span class="hidden sm:inline-flex items-center gap-1.5 rounded-full bg-gold-container/60 px-3 py-1 text-xs font-medium" data-mono>
              <span class="h-1.5 w-1.5 rounded-full bg-gold"></span>
              {{ key }}</span
            >
          }
          @if (tenantKey(); as key) {
            <span class="inline-flex sm:hidden items-center gap-1 rounded-full bg-gold-container/60 px-2.5 py-1 text-[11px] font-bold" data-mono>{{ key | slice:0:10 }}</span>
          }

          <div class="hidden sm:flex items-center gap-1 text-xs text-gray-400">
            <span class="h-1 w-1 rounded-full bg-gray-300"></span>
            <span class="hidden md:inline">{{ userEmail() }}</span>
          </div>

          <app-pwa-install-button class="ms-auto hidden sm:flex" />

          <!-- Gold price chip: native pill -->
          <button
            type="button"
            class="flex items-center gap-2 rounded-full bg-gold-container/40 px-3 py-2 text-xs font-medium text-gray-700 transition hover:bg-gold-container/60 active:scale-[0.98] sm:px-3.5"
            title="تحديث سعر الذهب"
            (click)="refreshGoldPrice()"
          >
            <span class="flex items-center gap-1.5">
              <span class="flex h-6 w-6 items-center justify-center rounded-full bg-gold text-white sm:h-7 sm:w-7">
                <lucide-icon [img]="goldIcon" [size]="12" class="sm:[&>svg]:!h-[14px] sm:[&>svg]:!w-[14px]" />
              </span>
              <span class="hidden sm:inline">سعر {{ goldLabel() }}</span>
              <span class="sm:hidden">{{ goldLabel() }}</span>
            </span>
            @if (goldLoading()) {
              <app-skeleton width="3rem" height="0.875rem" />
            } @else if (goldChip(); as chip) {
              <span class="flex items-center gap-1 font-bold" data-mono>
                {{ chip.info.displayPrice }}
                @if (direction(chip.info) === 'up') {
                  <lucide-icon [img]="trendUpIcon" [size]="12" class="text-emerald-600" />
                } @else if (direction(chip.info) === 'down') {
                  <lucide-icon [img]="trendDownIcon" [size]="12" class="text-red-600" />
                }
              </span>
            } @else {
              <span>—</span>
            }
          </button>

          <span
            class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gold-container/60 text-sm font-semibold text-gray-700 ring-2 ring-white shadow-sm"
            [attr.title]="userEmail() ?? ''"
          >
            {{ initial() }}
          </span>
        </header>

        <main class="flex-1 overflow-y-auto p-3 sm:p-4 lg:p-6 pb-[calc(5.5rem+env(safe-area-inset-bottom))] lg:pb-6">
          <router-outlet />
        </main>
      </div>

      <!-- Bottom tab bar: mobile + tablet (up to lg) — native app feel -->
      <nav
        class="fixed inset-x-0 bottom-0 z-20 flex items-stretch gap-1 border-t border-gray-200 bg-card px-1 pt-1 backdrop-blur supports-[backdrop-filter]:bg-card/95 lg:hidden"
        style="padding-bottom: max(0.5rem, env(safe-area-inset-bottom))"
        aria-label="التنقل السفلي"
      >
        @for (item of bottomNav(); track item.key) {
          <a
            [routerLink]="item.route"
            routerLinkActive="bottom-active"
            #mRla="routerLinkActive"
            class="relative flex min-h-[56px] flex-1 flex-col items-center justify-center gap-1 rounded-2xl px-1 py-1.5 text-[11px] font-medium text-gray-500 transition-all active:scale-[0.96]"
            [class.!text-gray-900]="mRla.isActive"
          >
            <span
              class="flex h-7 w-12 items-center justify-center rounded-full transition"
              [class.bg-gold-container]="mRla.isActive"
              [class.shadow-sm]="mRla.isActive"
            >
              <lucide-icon [img]="resolveIcon(item.icon)" [size]="20" [strokeWidth]="mRla.isActive ? 2.6 : 2" />
            </span>
            <span class="leading-none truncate max-w-[72px]">{{ shortLabel(item.label) }}</span>
          </a>
        }
        <!-- More sheet trigger -->
        <button
          type="button"
          class="flex min-h-[56px] flex-1 flex-col items-center justify-center gap-1 rounded-2xl px-1 py-1.5 text-[11px] font-medium text-gray-500 transition active:scale-[0.96] hover:bg-gray-50"
          [class.!text-gray-900]="drawerOpen()"
          [class.bg-gray-50]="drawerOpen()"
          (click)="toggleDrawer()"
          aria-label="المزيد"
          [attr.aria-expanded]="drawerOpen()"
        >
          <span
            class="flex h-7 w-12 items-center justify-center rounded-full transition"
            [class.bg-gold-container]="drawerOpen()"
          >
            <lucide-icon [img]="menuIcon" [size]="20" />
          </span>
          <span class="leading-none">المزيد</span>
        </button>
      </nav>
    </div>
  `,
  styles: `
    .nav-active {
      background-color: color-mix(in srgb, var(--color-gold-container) 55%, transparent);
      font-weight: 600;
      color: #111827;
    }
    .bottom-active {
      color: #111827;
      font-weight: 700;
    }
    .bottom-active span:first-of-type {
      background-color: var(--color-gold-container);
    }
  `,
})
export class AppShell {
  private readonly auth = inject(AuthStore);
  private readonly goldPrices = inject(GoldPriceStore);
  private readonly router = inject(Router);

  readonly drawerOpen = signal(false);
  private touchStartX = 0;

  readonly navItems = computed(() =>
    buildNavItems(this.auth.permissions(), this.auth.isHostAdmin(), this.auth.user()?.roles ?? []),
  );

  readonly bottomNav = computed(() => {
    const items = this.navItems();
    const byKey = new Map(items.map((i) => [i.key, i]));
    const primary: NavItem[] = [];
    for (const key of BOTTOM_PRIORITY) {
      const hit = byKey.get(key as NavItem['key']);
      if (hit) primary.push(hit);
      if (primary.length >= 4) break;
    }
    // fill fallback with first items not already included
    for (const item of items) {
      if (primary.length >= 4) break;
      if (!primary.find((p) => p.key === item.key)) primary.push(item);
    }
    return primary.slice(0, 4);
  });

  readonly groupedNav = computed(() => {
    const items = this.navItems();
    const groups: { title: string; keys: string[] }[] = [
      { title: 'الرئيسية', keys: ['dashboard', 'gold-prices'] },
      { title: 'العمليات', keys: ['sales', 'purchases', 'suppliers', 'inventory', 'catalog'] },
      { title: 'المالية', keys: ['finance', 'expenses'] },
      { title: 'الإدارة', keys: ['hr', 'settings', 'permissions', 'host-admin'] },
    ];
    const byKey = new Map(items.map((i) => [i.key, i]));
    const result: { title: string; items: NavItem[] }[] = [];
    const seen = new Set<string>();
    for (const group of groups) {
      const groupItems = group.keys.map((k) => byKey.get(k as NavItem['key'])).filter((v): v is NavItem => !!v);
      groupItems.forEach((i) => seen.add(i.key));
      if (groupItems.length) result.push({ title: group.title, items: groupItems });
    }
    const rest = items.filter((i) => !seen.has(i.key));
    if (rest.length) result.push({ title: 'أخرى', items: rest });
    return result;
  });

  readonly tenantKey = this.auth.tenantKey;
  readonly userEmail = computed(() => this.auth.user()?.email ?? null);
  readonly initial = computed(() => (this.auth.user()?.email ?? '؟').charAt(0).toUpperCase());
  readonly signingOut = signal(false);

  readonly goldChip = this.goldPrices.chip;
  readonly goldLoading = this.goldPrices.loading;
  readonly goldLabel = computed(() => this.goldPrices.chip()?.label ?? '21');

  readonly goldIcon = resolveIcon('wallet');
  readonly logOutIcon = resolveIcon('log-out');
  readonly menuIcon = resolveIcon('menu')!;
  readonly closeIcon = resolveIcon('x')!;
  readonly trendUpIcon = TrendingUp;
  readonly trendDownIcon = TrendingDown;

  readonly resolveIcon = resolveIcon;

  sidebarClass = computed(() => {
    const base =
      'fixed inset-y-0 start-0 z-40 flex w-[86vw] max-w-[360px] flex-col border-e border-gray-200 bg-card shadow-2xl transition-transform duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] lg:w-64 lg:translate-x-0 lg:shadow-none';
    // RTL drawer on the right (start) → hidden is translate-x-full (to the right) in RTL, but Tailwind's physical translate is LTR; use logical by toggling both.
    // We keep start-0 and hide with translate-x-full; in RTL this moves right off-screen correctly in modern browsers with logical support.
    // Fallback: use data attribute for explicit RTL transform.
    return this.drawerOpen() ? `${base} translate-x-0` : `${base} translate-x-full lg:translate-x-0`;
  });

  constructor() {
    void this.goldPrices.ensureLoaded();
    this.goldPrices.startAutoRefresh();
  }

  shortLabel(label: string): string {
    // Keep bottom bar labels ultra-compact on 320px
    return label.length > 8 ? label.slice(0, 8) + '…' : label;
  }

  toggleDrawer(): void {
    this.drawerOpen.update((v) => !v);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  onTouchStart(event: TouchEvent): void {
    this.touchStartX = event.touches[0]?.clientX ?? 0;
  }

  onTouchMove(event: TouchEvent): void {
    // Allow native scroll; only intercept horizontal swipe
    const dx = (event.touches[0]?.clientX ?? 0) - this.touchStartX;
    if (Math.abs(dx) > 20) {
      // prevent scroll jank while swiping drawer horizontally
    }
  }

  onTouchEnd(event: TouchEvent): void {
    const dx = (event.changedTouches[0]?.clientX ?? 0) - this.touchStartX;
    // Swipe right (in RTL, swipe to the right closes drawer that sits on right)
    // For RTL drawer on right: swipe right (dx > 60) should close? Actually drawer open → swipe toward start edge (right) keeps open, swipe opposite closes.
    // Simplify: swipe left (negative) when drawer open → close
    if (this.drawerOpen() && dx < -60) this.closeDrawer();
  }

  direction(info: GoldPriceInfo): string {
    return info.changeDirection === 'up' || info.changeDirection === 'down' ? info.changeDirection : '';
  }

  change(info: GoldPriceInfo): string {
    return Number(info.changePercent24H ?? 0).toFixed(2);
  }

  refreshGoldPrice(): void {
    void this.goldPrices.refresh();
  }

  async signOut(): Promise<void> {
    if (this.signingOut()) return;
    this.signingOut.set(true);
    try {
      await this.auth.logout();
    } finally {
      this.signingOut.set(false);
      await this.router.navigateByUrl('/login');
    }
  }
}
