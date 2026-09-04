import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type { components } from '../../shared/api/schema';
import { ApiClient } from '../http/api-client.service';

export type GoldPricesResponse = components['schemas']['GoldPricesResponse'];
export type GoldPriceInfo = components['schemas']['GoldPriceInfo'];

export const GOLD_PRICE_REFRESH_MS = 5 * 60_000;

/**
 * Live gold price feed (external, not tenant-owned). The topbar chip shows the 21K per-gram
 * price when present, falling back to the global spot price. Auto-refreshes every 5 minutes
 * (P3.4 refines this with a service worker stale-while-revalidate).
 */
@Injectable({ providedIn: 'root' })
export class GoldPriceStore {
  private readonly api = inject(ApiClient);

  private readonly currentSignal = signal<GoldPricesResponse | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly lastUpdatedSignal = signal<Date | null>(null);
  private loadPromise: Promise<void> | null = null;
  private timer: ReturnType<typeof setInterval> | null = null;

  readonly current = this.currentSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly lastUpdated = this.lastUpdatedSignal.asReadonly();

  /** What the topbar chip renders: 21K per-gram price (preferred) or spot. */
  readonly chip = computed(() => {
    const response = this.currentSignal();
    if (response?.pricePerGram21K) {
      return { info: response.pricePerGram21K, label: '21' };
    }
    if (response?.spotPrice) {
      return { info: response.spotPrice, label: 'العالمي' };
    }
    return null;
  });

  ensureLoaded(): Promise<void> {
    if (this.currentSignal() !== null) {
      return Promise.resolve();
    }
    this.loadPromise ??= this.load();
    return this.loadPromise;
  }

  refresh(): Promise<void> {
    this.loadPromise = this.load();
    return this.loadPromise;
  }

  startAutoRefresh(intervalMs = GOLD_PRICE_REFRESH_MS): void {
    this.stopAutoRefresh();
    this.timer = setInterval(() => void this.refresh(), intervalMs);
  }

  stopAutoRefresh(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  private async load(): Promise<void> {
    this.loadingSignal.set(true);
    try {
      const response = await firstValueFrom(
        this.api.get<GoldPricesResponse>('/api/v1/gold-prices/current'),
      );
      this.currentSignal.set(response);
      this.lastUpdatedSignal.set(new Date());
    } finally {
      this.loadingSignal.set(false);
      this.loadPromise = null;
    }
  }
}
