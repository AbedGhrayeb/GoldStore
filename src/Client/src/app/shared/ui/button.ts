import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Loader2, LucideAngularModule } from 'lucide-angular';

import { resolveIcon } from './icon-registry';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

const VARIANTS: Readonly<Record<ButtonVariant, string>> = {
  primary: 'bg-gold text-gray-900 hover:bg-gold/90 focus-visible:ring-gold/50',
  secondary:
    'border border-gray-300 bg-card text-gray-700 hover:bg-gray-50 focus-visible:ring-gray-300',
  ghost: 'text-gray-600 hover:bg-gray-100 focus-visible:ring-gray-300',
  danger: 'bg-error text-white hover:bg-error/90 focus-visible:ring-error/50',
};

const SIZES: Readonly<Record<ButtonSize, string>> = {
  sm: 'gap-1.5 px-3 py-2 text-xs min-h-[36px]',
  md: 'gap-2 px-4 py-2.5 text-sm min-h-[44px]',
  lg: 'gap-2 px-5 py-3 text-base min-h-[48px]',
};

@Component({
  selector: 'app-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || loading()"
      [class]="classes()"
      (click)="clicked.emit()"
    >
      @if (loading()) {
        <lucide-icon [img]="loader" [size]="iconSize" class="animate-spin" />
      } @else if (icon(); as iconName) {
        @if (resolveIcon(iconName); as iconData) {
          <lucide-icon [img]="iconData" [size]="iconSize" class="shrink-0" />
        }
      }
      <ng-content />
    </button>
  `,
})
export class Button {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly icon = input<string>('');
  readonly loading = input(false);
  readonly disabled = input(false);
  readonly clicked = output<void>();

  readonly loader = Loader2;
  readonly resolveIcon = resolveIcon;

  readonly classes = computed(() => {
    const base =
      'inline-flex select-none items-center justify-center rounded-input font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-60';
    return `${base} ${VARIANTS[this.variant()]} ${SIZES[this.size()]}`;
  });

  readonly iconSize = 16;
}
