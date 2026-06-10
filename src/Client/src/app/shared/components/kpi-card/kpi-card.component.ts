import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-surface-card rounded-xl border border-gold-border/20 p-5 shadow-card">
      <div class="flex items-start justify-between mb-3">
        <span class="text-sm text-text-muted">{{ label() }}</span>
        @if (icon()) {
          <span class="w-10 h-10 rounded-lg bg-gold-primary/10 flex items-center justify-center text-gold-primary" [innerHTML]="icon()"></span>
        }
      </div>
      <div class="text-2xl font-bold text-text-primary mb-1" dir="ltr">{{ value() }}</div>
      @if (change(); as c) {
        <div class="flex items-center gap-1">
          <span [class.text-success]="changeDir() === 'up'" [class.text-error]="changeDir() === 'down'" class="text-sm font-medium">
            {{ changeDir() === 'up' ? '↑' : '↓' }} {{ c }}
          </span>
          <span class="text-xs text-text-muted">عن الشهر الماضي</span>
        </div>
      }
    </div>
  `,
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly change = input<string>();
  readonly changeDir = input<'up' | 'down'>();
  readonly icon = input<string>();
}
