import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal,
  TemplateRef,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import { EmptyState } from './empty-state';
import { resolveIcon } from './icon-registry';
import { Skeleton } from './skeleton';

export type SortDirection = 'asc' | 'desc';

export interface SortState {
  key: string;
  direction: SortDirection;
}

export interface TableColumn<T> {
  key: string;
  header: string;
  /** Plain-text renderer. Omit it (or provide `cellTemplate`) to render rich content per row. */
  cell?: (row: T) => string | number | null | undefined;
  /** Optional per-cell template — receives the row as `$implicit`. Supersedes `cell`. */
  cellTemplate?: TemplateRef<{ $implicit: T }>;
  sortable?: boolean;
  sortValue?: (row: T) => string | number;
  numeric?: boolean;
  align?: 'start' | 'end' | 'center';
  width?: string;
}

const ALIGN: Readonly<Record<'start' | 'end' | 'center', string>> = {
  start: 'text-start',
  end: 'text-end',
  center: 'text-center',
};

@Component({
  selector: 'app-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule, EmptyState, Skeleton, NgTemplateOutlet],
  template: `
    <div class="overflow-auto overscroll-x-contain -mx-4 px-4 sm:mx-0 sm:px-0" [style.maxHeight]="maxHeight()">
      <table class="w-full min-w-[640px] border-collapse text-sm sm:min-w-0">
        <thead>
          <tr>
            @for (column of columns(); track column.key) {
              <th
                [style.width]="column.width"
                [class]="headerClass(column)"
                class="sticky top-0 z-10 border-b border-gray-200 bg-card px-3 py-3 font-semibold text-gray-600 sm:px-4 whitespace-nowrap"
              >
                @if (column.sortable) {
                  <button
                    type="button"
                    class="inline-flex items-center gap-1.5 hover:text-gray-900"
                    (click)="toggleSort(column)"
                  >
                    <span>{{ column.header }}</span>
                    <lucide-icon
                      [img]="resolveIcon(sortIcon(column.key))"
                      [size]="14"
                      class="shrink-0"
                    />
                  </button>
                } @else {
                  <span>{{ column.header }}</span>
                }
              </th>
            }
          </tr>
        </thead>

        <tbody>
          @if (loading()) {
            @for (row of skeletonRows; track $index) {
              <tr>
                @for (column of columns(); track column.key) {
                  <td class="px-4 py-3">
                    <app-skeleton height="1rem" [width]="skeletonWidth(column.key)" />
                  </td>
                }
              </tr>
            }
          } @else if (displayedRows().length === 0) {
            <tr>
              <td [attr.colspan]="columns().length" class="px-4 py-10">
                <app-empty-state
                  [icon]="emptyIcon()"
                  [title]="emptyTitle()"
                  [description]="emptyDescription()"
                />
              </td>
            </tr>
          } @else {
            @for (row of displayedRows(); track rowIndex(row)) {
              <tr
                class="border-b border-gray-100 transition-colors last:border-0 hover:bg-gold-container/15"
              >
                @for (column of columns(); track column.key) {
                  <td [class]="cellClass(column)" class="px-4 py-3">
                    @if (column.cellTemplate) {
                      <ng-container
                        [ngTemplateOutlet]="column.cellTemplate"
                        [ngTemplateOutletContext]="{ $implicit: row }"
                      />
                    } @else {
                      {{ column.cell ? column.cell(row) : '' }}
                    }
                  </td>
                }
              </tr>
            }
          }
        </tbody>
      </table>
    </div>
  `,
})
export class Table<T> {
  readonly columns = input.required<TableColumn<T>[]>();
  readonly rows = input<T[]>([]);
  readonly initialSort = input<SortState | null>(null);
  readonly loading = input(false);
  readonly maxHeight = input<string>('');
  readonly emptyIcon = input('inbox');
  readonly emptyTitle = input('لا توجد بيانات');
  readonly emptyDescription = input('');

  readonly sortChange = output<SortState | null>();

  private readonly override = signal<SortState | null>(null);

  readonly activeSort = computed(() => this.override() ?? this.initialSort());

  readonly displayedRows = computed(() => {
    const state = this.activeSort();
    if (state === null) {
      return this.rows();
    }
    const column = this.columns().find((candidate) => candidate.key === state.key);
    if (!column) {
      return this.rows();
    }
    const valueOf = column.sortValue ?? column.cell ?? (() => '');
    const direction = state.direction === 'asc' ? 1 : -1;
    return [...this.rows()].sort((a, b) => {
      const first = valueOf(a);
      const second = valueOf(b);
      if (typeof first === 'number' && typeof second === 'number') {
        return (first - second) * direction;
      }
      return String(first ?? '').localeCompare(String(second ?? ''), 'ar') * direction;
    });
  });

  readonly skeletonRows = [0, 1, 2, 3, 4];

  readonly resolveIcon = resolveIcon;

  headerClass(column: TableColumn<T>): string {
    if (column.numeric) {
      return 'text-start';
    }
    return ALIGN[column.align ?? 'start'];
  }

  cellClass(column: TableColumn<T>): string {
    const alignment = column.numeric ? 'text-start' : ALIGN[column.align ?? 'start'];
    return `${alignment}${column.numeric ? ' data-mono' : ''}`;
  }

  skeletonWidth(key: string): string {
    return `${70 + (key.length % 3) * 10}%`;
  }

  toggleSort(column: TableColumn<T>): void {
    const current = this.activeSort();
    const next: SortState =
      current?.key === column.key && current.direction === 'asc'
        ? { key: column.key, direction: 'desc' }
        : { key: column.key, direction: 'asc' };
    this.override.set(next);
    this.sortChange.emit(next);
  }

  sortIcon(key: string): string {
    const current = this.activeSort();
    if (current?.key !== key) {
      return 'chevrons-up-down';
    }
    return current.direction === 'asc' ? 'chevron-up' : 'chevron-down';
  }

  rowIndex(row: T): unknown {
    if (typeof row !== 'object' || row === null) {
      return row;
    }
    const record = row as Record<string, unknown>;
    return ('id' in record ? record['id'] : JSON.stringify(row)) ?? JSON.stringify(row);
  }
}
