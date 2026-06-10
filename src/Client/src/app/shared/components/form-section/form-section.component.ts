import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-form-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-6">
      @if (title()) {
        <h3 class="text-base font-semibold text-text-primary mb-4">{{ title() }}</h3>
      }
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <ng-content />
      </div>
    </div>
  `,
})
export class FormSectionComponent {
  readonly title = input<string>();
}
