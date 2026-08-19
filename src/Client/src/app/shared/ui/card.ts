import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="rounded-lg border border-gray-200 bg-card p-6 shadow-sm">
      @if (title()) {
        <h2 class="mb-4 text-sm font-semibold text-gray-700">{{ title() }}</h2>
      }
      <ng-content />
    </section>
  `,
})
export class Card {
  readonly title = input<string>('');
}
