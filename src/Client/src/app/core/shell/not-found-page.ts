import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, SearchX } from 'lucide-angular';

import { OfflineStore } from '../offline/offline-store';

@Component({
  selector: 'app-not-found-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <div class="mx-auto flex max-w-lg flex-col items-center rounded-xl bg-card p-10 text-center shadow-sm">
      <span class="flex h-16 w-16 items-center justify-center rounded-full bg-gold-container/50">
        <lucide-icon [img]="icon" [size]="28" class="text-gold" />
      </span>
      <h1 class="mt-4 text-2xl font-bold text-gray-800">الصفحة غير موجودة</h1>
      <p class="mt-2 text-sm leading-6 text-gray-500">
        @if (offlineStore.offline()) {
          أنت غير متصل حالياً — قد تكون هذه الصفحة غير متوفرة دون اتصال.
        } @else {
          الرابط الذي اتبعته غير صحيح أو تم نقل الصفحة.
        }
      </p>
      <a routerLink="/" class="mt-6 rounded-full bg-gold px-6 py-2.5 text-sm font-bold text-white shadow-sm hover:bg-[#b8962e]">
        العودة للرئيسية
      </a>
    </div>
  `,
})
export class NotFoundPage {
  readonly offlineStore = inject(OfflineStore);
  readonly icon = SearchX;
}
