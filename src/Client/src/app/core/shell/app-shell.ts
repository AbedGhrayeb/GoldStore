import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LucideAngularModule, TrendingDown, TrendingUp } from 'lucide-angular';

import { AuthStore } from '../auth/auth-store';
import type { GoldPriceInfo } from '../gold-prices/gold-price-store';
import { GoldPriceStore } from '../gold-prices/gold-price-store';
import { buildNavItems } from '../navigation/nav-items';
import { resolveIcon, Skeleton } from '../../shared/ui';
import { TenantBrand } from './tenant-brand';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideAngularModule, TenantBrand, Skeleton],
  template: `
    <div class="flex h-dvh bg-surface">
      <aside class="fixed inset-y-0 start-0 flex w-64 flex-col border-e border-gray-200 bg-card">
        <div class="px-6 py-5">
          <app-tenant-brand [tenantKey]="tenantKey()" />
        </div>

        <nav class="flex-1 space-y-1 overflow-y-auto px-3 py-2">
          @for (item of navItems(); track item.key) {
            <a
              [routerLink]="item.route"
              routerLinkActive="nav-active"
              #rla="routerLinkActive"
              class="relative flex items-center gap-3 rounded-md px-4 py-2.5 text-sm text-gray-700 hover:bg-gold-container/40"
            >
              @if (rla.isActive) {
                <span class="absolute inset-y-1.5 start-0 w-1 rounded-full bg-gold"></span>
              }
              <lucide-icon
                [img]="resolveIcon(item.icon)"
                [size]="18"
                [strokeWidth]="2"
                class="shrink-0"
              />
              <span>{{ item.label }}</span>
            </a>
          }
        </nav>
      </aside>

      <div class="ms-64 flex min-w-0 flex-1 flex-col">
        <header class="flex h-16 shrink-0 items-center gap-4 border-b border-gray-200 bg-card px-6">
          @if (tenantKey(); as key) {
            <span
              class="rounded-full bg-gold-container/60 px-3 py-1 text-xs font-medium"
              data-mono
              >{{ key }}</span
            >
          }
          <button
            type="button"
            class="ms-auto flex items-center gap-2 rounded-full bg-gold-container/40 px-3 py-1 text-xs font-medium text-gray-600 transition-colors hover:bg-gold-container/60"
            title="تحديث سعر الذهب"
            (click)="refreshGoldPrice()"
          >
            <span class="flex items-center gap-1">
              <lucide-icon [img]="goldIcon" [size]="14" />
              سعر {{ goldLabel() }}
            </span>
            @if (goldLoading()) {
              <app-skeleton width="4rem" height="0.875rem" />
            } @else if (goldChip(); as chip) {
              <span class="flex items-center gap-1" data-mono>
                {{ chip.info.displayPrice }}
                @if (direction(chip.info) === 'up') {
                  <lucide-icon [img]="trendUpIcon" [size]="12" class="text-emerald-600" />
                  <span class="text-emerald-600">+{{ change(chip.info) }}%</span>
                } @else if (direction(chip.info) === 'down') {
                  <lucide-icon [img]="trendDownIcon" [size]="12" class="text-red-600" />
                  <span class="text-red-600">{{ change(chip.info) }}%</span>
                }
              </span>
            } @else {
              <span>—</span>
            }
          </button>
          <span
            class="flex h-9 w-9 items-center justify-center rounded-full bg-gold-container/60 text-sm font-semibold text-gray-700"
          >
            {{ initial() }}
          </span>
        </header>

        <main class="flex-1 overflow-y-auto p-6">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: `
    .nav-active {
      background-color: color-mix(in srgb, var(--color-gold-container) 40%, transparent);
      font-weight: 600;
      color: #1f2937;
    }
  `,
})
export class AppShell {
  private readonly auth = inject(AuthStore);
  private readonly goldPrices = inject(GoldPriceStore);

  readonly navItems = computed(() =>
    buildNavItems(this.auth.permissions(), this.auth.isHostAdmin()),
  );
  readonly tenantKey = this.auth.tenantKey;
  readonly initial = computed(() => (this.auth.user()?.email ?? '؟').charAt(0).toUpperCase());

  readonly goldChip = this.goldPrices.chip;
  readonly goldLoading = this.goldPrices.loading;
  readonly goldLabel = computed(() => this.goldPrices.chip()?.label ?? '21');

  readonly goldIcon = resolveIcon('wallet');
  readonly trendUpIcon = TrendingUp;
  readonly trendDownIcon = TrendingDown;

  readonly resolveIcon = resolveIcon;

  constructor() {
    void this.goldPrices.ensureLoaded();
    this.goldPrices.startAutoRefresh();
  }

  direction(info: GoldPriceInfo): string {
    return info.changeDirection === 'up' || info.changeDirection === 'down'
      ? info.changeDirection
      : '';
  }

  change(info: GoldPriceInfo): string {
    return Number(info.changePercent24H ?? 0).toFixed(2);
  }

  refreshGoldPrice(): void {
    void this.goldPrices.refresh();
  }
}
