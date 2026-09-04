import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';
import type { VersionReadyEvent } from '@angular/service-worker';
import { filter, interval } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PwaUpdateService {
  private readonly swUpdate = inject(SwUpdate, { optional: true });

  readonly updateAvailable = signal(false);
  readonly checking = signal(false);

  constructor() {
    if (!this.swUpdate?.isEnabled) {
      return;
    }

    // Prompt when a new version is ready (ngsw has downloaded it in background)
    this.swUpdate.versionUpdates
      .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'))
      .subscribe(() => {
        this.updateAvailable.set(true);
      });

    // SwUpdate already polls on navigation; add periodic check every 6h and on visibility regain
    interval(6 * 60 * 60 * 1000).subscribe(() => void this.checkForUpdate());
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') {
        void this.checkForUpdate();
      }
    });
  }

  async checkForUpdate(): Promise<boolean> {
    if (!this.swUpdate?.isEnabled) return false;
    this.checking.set(true);
    try {
      const hasUpdate = await this.swUpdate.checkForUpdate();
      return hasUpdate;
    } catch {
      return false;
    } finally {
      this.checking.set(false);
    }
  }

  async activateUpdate(): Promise<void> {
    if (!this.swUpdate?.isEnabled) return;
    await this.swUpdate.activateUpdate();
    document.location.reload();
  }

  dismiss(): void {
    this.updateAvailable.set(false);
  }
}
