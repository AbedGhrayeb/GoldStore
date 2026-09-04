import { Injectable, computed, signal } from '@angular/core';

/**
 * Global connectivity state. Tracks the browser's online/offline events and exposes a
 * `checkNow()` that re-probes the API so retry buttons can confirm recovery.
 */
@Injectable({ providedIn: 'root' })
export class OfflineStore {
  private readonly onlineSignal = signal(
    typeof navigator !== 'undefined' ? navigator.onLine : true,
  );
  private readonly checkingSignal = signal(false);

  readonly online = this.onlineSignal.asReadonly();
  readonly offline = computed(() => !this.onlineSignal());
  readonly checking = this.checkingSignal.asReadonly();

  constructor() {
    window.addEventListener('online', () => this.setOnline(true));
    window.addEventListener('offline', () => this.setOnline(false));
  }

  /** Re-probe connectivity (used by the retry button / banner). */
  async checkNow(): Promise<boolean> {
    this.checkingSignal.set(true);
    try {
      const online = navigator.onLine;
      if (!online) {
        this.setOnline(false);
        return false;
      }
      this.setOnline(true);
      return true;
    } finally {
      this.checkingSignal.set(false);
    }
  }

  private setOnline(online: boolean): void {
    this.onlineSignal.set(online);
  }
}
