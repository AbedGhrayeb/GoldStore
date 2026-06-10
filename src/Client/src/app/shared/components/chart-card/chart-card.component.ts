import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-chart-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-5">
      @if (title()) {
        <h3 class="text-base font-semibold text-text-primary mb-4">{{ title() }}</h3>
      }
      <ng-content />
    </div>
  `,
})
export class ChartCardComponent {
  readonly title = input<string>();
}
