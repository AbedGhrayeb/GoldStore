import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Brand block (logo tile + store name + optional tenant key chip) — sidebar now, login later. */
@Component({
  selector: 'app-tenant-brand',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center gap-3">
      <span
        class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-gold text-lg font-bold text-white"
      >
        ذ
      </span>
      <div class="min-w-0">
        <p class="truncate font-semibold leading-tight">متجر الذهب</p>
        <p class="text-xs text-gray-500">نظام إدارة متكامل</p>
        @if (tenantKey(); as key) {
          <span
            class="mt-1 inline-block rounded-full bg-gold-container/60 px-2 py-0.5 text-[11px] font-medium text-gray-700"
            data-mono
            >{{ key }}</span
          >
        }
      </div>
    </div>
  `,
})
export class TenantBrand {
  readonly tenantKey = input<string | null>(null);
}
