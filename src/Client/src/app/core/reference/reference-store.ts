import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type { components } from '../../shared/api/schema';
import { ApiClient } from '../http/api-client.service';

export type KaratReference = components['schemas']['KaratReference'];
export type CurrencyReference = components['schemas']['CurrencyReference'];

/**
 * Shared reference data (karats + currencies) for form selects across features.
 * Loaded once per session, lazily; single-flight; safe to call from every feature page.
 */
@Injectable({ providedIn: 'root' })
export class ReferenceStore {
  private readonly api = inject(ApiClient);

  private readonly karatsSignal = signal<KaratReference[]>([]);
  private readonly currenciesSignal = signal<CurrencyReference[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly loadedSignal = signal(false);
  private loadPromise: Promise<void> | null = null;

  readonly karats = this.karatsSignal.asReadonly();
  readonly currencies = this.currenciesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly loaded = this.loadedSignal.asReadonly();

  ensureLoaded(): Promise<void> {
    if (this.loadedSignal()) {
      return Promise.resolve();
    }
    this.loadPromise ??= this.load();
    return this.loadPromise;
  }

  refresh(): Promise<void> {
    this.loadPromise = null;
    this.loadedSignal.set(false);
    return this.ensureLoaded();
  }

  private async load(): Promise<void> {
    this.loadingSignal.set(true);
    try {
      const [karats, currencies] = await Promise.all([
        firstValueFrom(this.api.get<KaratReference[]>('/api/v1/reference/karats')),
        firstValueFrom(this.api.get<CurrencyReference[]>('/api/v1/reference/currencies')),
      ]);
      this.karatsSignal.set(karats);
      this.currenciesSignal.set(currencies);
      this.loadedSignal.set(true);
    } finally {
      this.loadingSignal.set(false);
      this.loadPromise = null;
    }
  }
}
