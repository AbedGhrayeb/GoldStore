import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-loading-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-6">
      <div class="space-y-4">
        @for (row of rowsArray(); track $index) {
          <div class="flex gap-4">
            @for (col of columns(); track $index) {
              <div
                class="h-4 bg-gray-200 rounded animate-pulse"
                [style.width]="col"
              ></div>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class LoadingSkeletonComponent {
  readonly rows = input(4);
  readonly columns = input(['25%', '40%', '20%', '15%']);

  protected rowsArray(): number[] {
    return Array(this.rows()).fill(0);
  }
}
