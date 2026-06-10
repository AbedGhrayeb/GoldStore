import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
      [class]="statusClass()"
    >
      <ng-content />
    </span>
  `,
})
export class StatusBadgeComponent {
  readonly variant = input<'success' | 'warning' | 'error' | 'info' | 'default'>('default');

  protected statusClass(): string {
    const variants: Record<string, string> = {
      success: 'bg-success/10 text-success',
      warning: 'bg-warning/10 text-warning',
      error: 'bg-error/10 text-error',
      info: 'bg-info/10 text-info',
      default: 'bg-gold-primary/10 text-gold-primary',
    };
    return variants[this.variant()] || variants['default'];
  }
}
