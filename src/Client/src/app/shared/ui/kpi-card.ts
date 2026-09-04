import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import { resolveIcon } from './icon-registry';

@Component({
  selector: 'app-kpi-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <section class="rounded-lg border border-gray-200 bg-card p-6 shadow-sm">
      <div class="flex items-center justify-between">
        <p class="text-xs font-medium text-gray-500">{{ title() }}</p>
        @if (resolveIcon(icon()); as iconData) {
          <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gold-container/50">
            <lucide-icon [img]="iconData" [size]="18" class="text-gray-700" />
          </span>
        }
      </div>
      <p class="mt-3 text-2xl font-semibold text-gray-900" data-mono>
        {{ value() }}
        @if (unit(); as unit) {
          <span class="ms-1 text-sm font-normal text-gray-500">{{ unit }}</span>
        }
      </p>
      @if (trend(); as trend) {
        <p class="mt-2 flex items-center gap-1 text-xs font-medium" [class]="trendClass()">
          @if (trendUp()) {
            <lucide-icon [img]="trendUpIcon" [size]="14" />
          } @else {
            <lucide-icon [img]="trendDownIcon" [size]="14" />
          }
          <span data-mono>{{ trend }}</span>
        </p>
      }
    </section>
  `,
})
export class KpiCard {
  readonly title = input.required<string>();
  readonly value = input.required<string | number>();
  readonly unit = input('');
  readonly icon = input('wallet');
  readonly trend = input('');
  readonly trendUp = input(true);

  readonly trendUpIcon = resolveIcon('trending-up');
  readonly trendDownIcon = resolveIcon('trending-down');

  readonly trendClass = () => (this.trendUp() ? 'text-emerald-600' : 'text-red-600');

  readonly resolveIcon = resolveIcon;
}
