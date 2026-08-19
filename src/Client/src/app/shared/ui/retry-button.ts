import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { Button } from './button';

/**
 * Standard "retry" affordance for ApiError states. Renders a compact secondary button with a
 * refresh icon; the caller decides what `retry` does (re-run a load, re-check connectivity…).
 */
@Component({
  selector: 'app-retry-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button],
  template: `
    <app-button
      variant="secondary"
      size="sm"
      icon="refresh-cw"
      [loading]="loading()"
      [disabled]="disabled()"
      (clicked)="retry.emit()"
    >
      {{ label() }}
    </app-button>
  `,
})
export class RetryButton {
  readonly label = input('إعادة المحاولة');
  readonly loading = input(false);
  readonly disabled = input(false);
  readonly retry = output<void>();
}
