import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule, WifiOff } from 'lucide-angular';

import { OfflineStore } from '../offline/offline-store';
import { RetryButton } from '../../shared/ui';

/**
 * Global offline banner. Shown whenever the browser reports the network is down (or the
 * connectivity probe fails); offers a retry that re-checks connectivity. Cached data
 * (service worker / stores) remains visible beneath it.
 */
@Component({
  selector: 'app-offline-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RetryButton, LucideAngularModule],
  template: `
    @if (offlineStore.offline()) {
      <div
        role="alert"
        class="flex items-center gap-3 border-b border-amber-200 bg-amber-50 px-6 py-2 text-sm text-amber-800"
      >
        <span class="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-amber-200">
          <lucide-icon [img]="wifiOff" [size]="14" />
        </span>
        <p class="flex-1">أنت غير متصل بالإنترنت — تُعرض البيانات المخزنة مؤقتاً.</p>
        <app-retry-button
          label="إعادة المحاولة"
          [loading]="offlineStore.checking()"
          (retry)="offlineStore.checkNow()"
        />
      </div>
    }
  `,
})
export class OfflineBanner {
  readonly offlineStore = inject(OfflineStore);
  readonly wifiOff = WifiOff;
}
