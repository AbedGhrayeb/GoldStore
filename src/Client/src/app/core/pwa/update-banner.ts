import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule, RefreshCw, Sparkles, X } from 'lucide-angular';

import { PwaUpdateService } from './pwa-update.service';

@Component({
  selector: 'app-pwa-update-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    @if (updater.updateAvailable()) {
      <div
        role="status"
        aria-live="polite"
        class="flex items-center gap-3 border-b border-gold/30 bg-gold-container px-6 py-3 text-sm text-gray-800"
      >
        <span class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gold text-white">
          <lucide-icon [img]="sparkles" [size]="14" />
        </span>
        <p class="flex-1 font-medium">توفّر تحديث جديد للتطبيق</p>
        <button
          type="button"
          class="rounded-full bg-gold px-4 py-1.5 text-xs font-bold text-white shadow-sm transition hover:bg-[#b8962e] disabled:opacity-60"
          (click)="updater.activateUpdate()"
        >
          <span class="flex items-center gap-1.5">
            <lucide-icon [img]="refreshIcon" [size]="14" />
            تحديث الآن
          </span>
        </button>
        <button
          type="button"
          class="rounded-full p-1.5 text-gray-500 hover:bg-black/5"
          aria-label="تأجيل"
          (click)="updater.dismiss()"
        >
          <lucide-icon [img]="xIcon" [size]="14" />
        </button>
      </div>
    }
  `,
})
export class PwaUpdateBanner {
  readonly updater = inject(PwaUpdateService);
  readonly sparkles = Sparkles;
  readonly refreshIcon = RefreshCw;
  readonly xIcon = X;
}
