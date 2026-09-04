import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Brand block — بريق diamond mark. Used in sidebar and tenant login hero. */
@Component({
  selector: 'app-tenant-brand',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center gap-3">
      <span
        class="relative flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-gold to-amber-600 shadow-sm ring-1 ring-amber-900/10"
        aria-hidden="true"
      >
        <span class="absolute inset-0 bg-gradient-to-tr from-white/20 to-transparent"></span>
        <!-- diamond icon-fill — material-symbols style -->
        <span class="material-symbols-outlined icon-fill relative text-[22px] leading-none text-white" style="font-variation-settings: 'FILL' 1, 'wght' 400; font-family: 'Material Symbols Outlined';">diamond</span>
      </span>
      <div class="min-w-0">
        <p class="flex items-center gap-1.5 truncate text-[13px] font-bold leading-tight tracking-tight text-gray-900">
          بريق
          <span class="hidden rounded-full bg-gold-container/70 px-1.5 py-0.5 text-[10px] font-bold tracking-widest text-amber-800 sm:inline">BARIQ</span>
        </p>
        <p class="text-xs leading-none text-gray-500">Gold Stores Management System</p>
        @if (key(); as k) {
          <span
            class="mt-1.5 inline-flex items-center gap-1 rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-800"
            data-mono
            dir="ltr"
            >{{ k }}.goldstore.app</span
          >
        } @else if (tenantKey(); as tk) {
          <span
            class="mt-1.5 inline-flex items-center gap-1 rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-800"
            data-mono
            >{{ tk }}</span
          >
        }
      </div>
    </div>
  `,
})
export class TenantBrand {
  /** New flexible key — full subdomain or bare key. Prefer `key` over legacy `tenantKey`. */
  readonly key = input<string | null>(null);
  /** @deprecated — kept for backwards compat with sidebar: maps to `key` */
  readonly tenantKey = input<string | null>(null);
}
