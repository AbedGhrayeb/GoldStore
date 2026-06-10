import { Component, input, output, model, ChangeDetectionStrategy } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

export interface Column {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  align?: 'right' | 'left' | 'center';
  format?: (value: unknown) => string;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card overflow-hidden">
      @if (showToolbar()) {
        <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100">
          <div class="flex items-center gap-2">
            <ng-content select="[table-actions]" />
          </div>
          @if (total()) {
            <span class="text-sm text-text-muted">إجمالي: {{ total() }}</span>
          }
        </div>
      }
      <div class="overflow-x-auto">
        <table class="w-full text-right">
          <thead>
            <tr class="border-b border-gold-border/20 bg-surface-base/50">
              @for (col of columns(); track col.key) {
                <th
                  class="px-4 py-3 text-sm font-medium text-text-muted whitespace-nowrap"
                  [class.cursor-pointer]="col.sortable"
                  [class.text-left]="col.align === 'left'"
                  [style.width]="col.width"
                  (click)="onSortClick(col)"
                >
                  <div class="flex items-center gap-1" [class.justify-end]="col.align !== 'left'">
                    {{ col.label }}
                    @if (col.sortable) {
                      <svg class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M8 3L12 7L16 3"/>
                        <path d="M8 21L12 17L16 21"/>
                      </svg>
                    }
                  </div>
                </th>
              }
            </tr>
          </thead>
          <tbody>
            @for (row of data(); track getTrackBy(row)) {
              <tr
                class="border-b border-gold-border/10 hover:bg-gold-primary/5 transition-colors cursor-pointer"
                (click)="rowClick.emit(row)"
              >
                @for (col of columns(); track col.key) {
                  <td
                    class="px-4 py-3 text-sm text-text-primary"
                    [class.text-left]="col.align === 'left'"
                    [class.text-center]="col.align === 'center'"
                  >
                    @if (cellTemplate()) {
                      <ng-container *ngTemplateOutlet="cellTemplate()" />
                    } @else {
                      {{ getCellValue(row, col) }}
                    }
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td [colSpan]="columns().length" class="py-12">
                  <ng-content select="[empty-state]">
                    <div class="text-center text-text-muted">لا توجد بيانات</div>
                  </ng-content>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class DataTableComponent {
  readonly columns = input.required<Column[]>();
  readonly data = input.required<unknown[]>();
  readonly total = input<number>();
  readonly showToolbar = input(false);
  readonly trackBy = input<(row: unknown) => string | number>();
  readonly cellTemplate = input<any>();
  readonly rowClick = output<unknown>();
  readonly sort = output<string>();

  protected getTrackBy(row: unknown): string | number {
    const fn = this.trackBy();
    return fn ? fn(row) : (row as Record<string, unknown>)[this.columns()[0]?.key] as string | number;
  }

  protected getCellValue(row: unknown, col: Column): string {
    const value = (row as Record<string, unknown>)[col.key];
    return col.format ? col.format(value) : `${value}`;
  }

  protected onSortClick(col: Column): void {
    if (col.sortable) {
      this.sort.emit(col.key);
    }
  }
}
