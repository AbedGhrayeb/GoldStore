import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type BadgeVariant = 'gold' | 'success' | 'error' | 'warning' | 'neutral';
export type BadgeSize = 'sm' | 'md';

const VARIANTS: Readonly<Record<BadgeVariant, string>> = {
  gold: 'bg-gold-container/60 text-gray-800',
  success: 'bg-success/15 text-emerald-700',
  error: 'bg-error/15 text-red-700',
  warning: 'bg-amber-100 text-amber-800',
  neutral: 'bg-gray-100 text-gray-600',
};

const SIZES: Readonly<Record<BadgeSize, string>> = {
  sm: 'px-2 py-0.5 text-[11px]',
  md: 'px-2.5 py-1 text-xs',
};

@Component({
  selector: 'app-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="classes()" data-mono>
      <ng-content />
    </span>
  `,
})
export class Badge {
  readonly variant = input<BadgeVariant>('gold');
  readonly size = input<BadgeSize>('md');

  readonly classes = computed(
    () =>
      `inline-flex items-center rounded-full font-medium ${VARIANTS[this.variant()]} ${SIZES[this.size()]}`,
  );
}
