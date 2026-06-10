import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="px-4 lg:px-6 py-4 border-b border-gold-border/20 bg-surface-card">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-xl font-semibold text-text-primary">{{ title() }}</h1>
          @if (subtitle()) {
            <p class="text-sm text-text-muted mt-1">{{ subtitle() }}</p>
          }
        </div>
        <div class="flex items-center gap-2">
          <ng-content />
        </div>
      </div>
    </div>
  `,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
