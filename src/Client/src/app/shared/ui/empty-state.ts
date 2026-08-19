import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import { resolveIcon } from './icon-registry';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="flex flex-col items-center gap-3 py-4 text-center">
      @if (resolveIcon(icon()); as iconData) {
        <span class="flex h-14 w-14 items-center justify-center rounded-full bg-gold-container/40">
          <lucide-icon [img]="iconData" [size]="26" class="text-gray-500" />
        </span>
      }
      <div>
        <p class="text-sm font-semibold text-gray-700">{{ title() }}</p>
        @if (description(); as description) {
          <p class="mt-1 text-xs text-gray-500">{{ description }}</p>
        }
      </div>
      <ng-content />
    </div>
  `,
})
export class EmptyState {
  readonly icon = input('inbox');
  readonly title = input('لا توجد بيانات');
  readonly description = input('');

  readonly resolveIcon = resolveIcon;
}
