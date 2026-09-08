import { ChangeDetectionStrategy, Component, computed, effect, signal } from '@angular/core';
import type { OnDestroy } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { LucideAngularModule, TrendingDown, TrendingUp } from 'lucide-angular';

import { environment } from '../../environment/environment';
import type { GoldPriceInfo, GoldPricesResponse } from '../../core/gold-prices/gold-price-store';
import { Button, Card, EmptyState, RetryButton, Skeleton } from '../../shared/ui';

const GOLD_PRICE_REFRESH_MS = 24 * 60 * 60_000;

interface PriceTile {
  key: 'spot' | 'karat24' | 'karat21';
  label: string;
  badge: string;
  unit: string;
  info: () => GoldPriceInfo | undefined;
}

/**
 * P3.4 — Gold prices (auth only, no feature gate). Reads the daily external feed (JOD-only)
 * from `/gold-prices/current` via `httpResource` (fetch-based so the service worker's
 * `gold-prices` dataGroup serves stale-while-revalidate up to a day) and auto-refreshes
 * on the same interval. The tiles update in place through signals — no page reload.
 */
@Component({
  selector: 'app-gold-prices-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Card, EmptyState, LucideAngularModule, RetryButton, Skeleton],
  template: `
    <main class="space-y-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">أسعار الذهب</h1>
          <p class="mt-1 text-sm text-gray-600">
            سعر الذهب — تغذية خارجية (دينار) تُحدَّث تلقائيًا مرة يوميًا.
          </p>
        </div>
        <app-button variant="secondary" icon="refresh-cw" (clicked)="refresh()">تحديث</app-button>
      </div>

      @if (prices.error() && !prices.hasValue()) {
        <app-card>
          <app-empty-state
            icon="alert-circle"
            title="تعذّر تحميل الأسعار"
            [description]="errorMessage()"
          >
            <app-retry-button (retry)="refresh()" />
          </app-empty-state>
        </app-card>
      } @else {
        <section class="grid grid-cols-1 gap-4 md:grid-cols-3">
          @for (tile of tiles(); track tile.key) {
            <app-card>
              <div class="flex items-center justify-between">
                <p class="text-xs font-medium text-gray-500">{{ tile.label }}</p>
                <span
                  class="rounded-full bg-gold-container/60 px-2.5 py-1 text-xs font-semibold"
                  data-mono
                >
                  {{ tile.badge }}
                </span>
              </div>
              @if (prices.isLoading() && !prices.hasValue()) {
                <app-skeleton class="mt-3" width="6rem" height="1.75rem" />
              } @else {
                <p class="mt-3 text-3xl font-semibold text-gray-900" data-mono>
                  {{ tile.info()?.displayPrice ?? '—' }}
                </p>
                <p class="mt-2 flex items-center gap-2 text-xs text-gray-500">
                  <span>{{ tile.unit }}</span>
                  @if (direction(tile.info()) === 'up') {
                    <span class="flex items-center gap-1 text-emerald-600">
                      <lucide-icon [img]="trendUpIcon" [size]="12" />
                      +{{ change(tile.info()) }}%
                    </span>
                  } @else if (direction(tile.info()) === 'down') {
                    <span class="flex items-center gap-1 text-red-600">
                      <lucide-icon [img]="trendDownIcon" [size]="12" />
                      {{ change(tile.info()) }}%
                    </span>
                  }
                </p>
              }
            </app-card>
          }
        </section>
      }

      <p class="text-xs text-gray-500" data-mono>آخر تحديث: {{ lastUpdated() }}</p>
    </main>
  `,
})
export class GoldPricesPage implements OnDestroy {
  readonly prices = httpResource<GoldPricesResponse | null>(
    () => ({
      url: `${environment.API_BASE_URL}/api/v1/gold-prices/current`,
      withCredentials: true,
    }),
    { defaultValue: null },
  );

  private readonly lastUpdatedSignal = signal('—');
  readonly lastUpdated = this.lastUpdatedSignal.asReadonly();

  private readonly timer = setInterval(() => this.refresh(), GOLD_PRICE_REFRESH_MS);

  readonly trendUpIcon = TrendingUp;
  readonly trendDownIcon = TrendingDown;

  constructor() {
    effect(() => {
      if (this.prices.hasValue() && this.prices.value() !== null) {
        this.lastUpdatedSignal.set(
          new Date().toLocaleTimeString('ar', { hour: '2-digit', minute: '2-digit' }),
        );
      }
    });
  }

  readonly tiles = computed<PriceTile[]>(() => {
    const prices = this.prices.hasValue() ? this.prices.value() : null;
    return [
      {
        key: 'spot',
        label: 'سعر الأونصة',
        badge: 'العالمي',
        unit: 'أونصة',
        info: () => prices?.spotPrice,
      },
      {
        key: 'karat24',
        label: 'غرام عيار 24',
        badge: '24',
        unit: 'جرام / عيار 24',
        info: () => prices?.pricePerGram24K,
      },
      {
        key: 'karat21',
        label: 'غرام عيار 21',
        badge: '21',
        unit: 'جرام / عيار 21',
        info: () => prices?.pricePerGram21K,
      },
    ];
  });

  readonly errorMessage = computed(() => {
    const error = this.prices.error() as {
      message?: unknown;
      error?: { detail?: unknown; title?: unknown };
    } | null;
    const detail = error?.error?.detail;
    if (typeof detail === 'string' && detail !== '') {
      return detail;
    }
    const title = error?.error?.title;
    if (typeof title === 'string' && title !== '') {
      return title;
    }
    return typeof error?.message === 'string' && error.message !== ''
      ? error.message
      : 'حدث خطأ أثناء جلب الأسعار.';
  });

  refresh(): void {
    void this.prices.reload();
  }

  direction(info: GoldPriceInfo | undefined): string {
    if (!info || (info.changeDirection !== 'up' && info.changeDirection !== 'down')) {
      return '';
    }
    return info.changeDirection;
  }

  change(info: GoldPriceInfo | undefined): string {
    return Number(info?.changePercent24H ?? 0).toFixed(2);
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
  }
}
