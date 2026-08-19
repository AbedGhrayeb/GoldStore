import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Table, type SortState, type TableColumn } from './table';

interface SampleRow {
  id: number;
  name: string;
  weight: number;
}

const ROWS: SampleRow[] = [
  { id: 1, name: 'زبدة', weight: 12.5 },
  { id: 2, name: 'ذهب', weight: 3.25 },
  { id: 3, name: 'فضة', weight: 8.1 },
];

@Component({
  imports: [Table],
  template: `
    <app-table
      [columns]="columns"
      [rows]="rows"
      [initialSort]="initialSort"
      (sortChange)="onSortChange($event)"
    />
  `,
})
class HostComponent {
  columns: TableColumn<SampleRow>[] = [
    { key: 'id', header: 'الرقم', cell: (row) => row.id, numeric: true, sortable: true },
    { key: 'name', header: 'الاسم', cell: (row) => row.name, sortable: true },
    {
      key: 'weight',
      header: 'الوزن',
      cell: (row) => row.weight,
      numeric: true,
      sortable: true,
      sortValue: (row) => row.weight,
    },
    { key: 'note', header: 'ملاحظة', cell: () => '—' },
  ];
  rows: SampleRow[] = ROWS;
  initialSort: SortState | null = null;
  lastSort: SortState | null = null;

  onSortChange(sort: SortState | null): void {
    this.lastSort = sort;
  }
}

function createFixture(setup?: (host: HostComponent) => void): ComponentFixture<HostComponent> {
  const fixture = TestBed.createComponent(HostComponent);
  setup?.(fixture.componentInstance);
  fixture.detectChanges();
  return fixture;
}

function bodyIds(fixture: ComponentFixture<HostComponent>): string[] {
  const cells = fixture.nativeElement.querySelectorAll('tbody tr td:first-child');
  return [...cells].map((cell: HTMLElement) => cell.textContent?.trim() ?? '');
}

function clickHeader(fixture: ComponentFixture<HostComponent>, index: number): void {
  const button = fixture.nativeElement.querySelectorAll('thead th button')[index] as
    HTMLElement | undefined;
  button?.click();
  fixture.detectChanges();
}

describe('Table sorting', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('renders rows in the original order when no sort is active', () => {
    const fixture = createFixture();
    expect(bodyIds(fixture)).toEqual(['1', '2', '3']);
  });

  it('sorts a numeric column ascending on first click', () => {
    const fixture = createFixture();
    clickHeader(fixture, 0);
    expect(bodyIds(fixture)).toEqual(['1', '2', '3']);
    expect(fixture.componentInstance.lastSort).toEqual({ key: 'id', direction: 'asc' });
  });

  it('toggles the same column to descending on the second click', () => {
    const fixture = createFixture();
    clickHeader(fixture, 0);
    clickHeader(fixture, 0);
    expect(bodyIds(fixture)).toEqual(['3', '2', '1']);
    expect(fixture.componentInstance.lastSort).toEqual({ key: 'id', direction: 'desc' });
  });

  it('sorts a text column with Arabic-aware ordering', () => {
    const fixture = createFixture();
    clickHeader(fixture, 1);
    const names = [...fixture.nativeElement.querySelectorAll('tbody tr td:nth-child(2)')].map(
      (cell: HTMLElement) => cell.textContent?.trim() ?? '',
    );
    const expected = ['زبدة', 'ذهب', 'فضة'].sort((a, b) => a.localeCompare(b, 'ar'));
    expect(names).toEqual(expected);
  });

  it('sorts numbers by numeric value, not by string comparison', () => {
    const fixture = createFixture();
    clickHeader(fixture, 2);
    expect(bodyIds(fixture)).toEqual(['2', '3', '1']);
  });

  it('applies the initialSort at construction', () => {
    const fixture = createFixture((host) => {
      host.initialSort = { key: 'weight', direction: 'desc' };
    });
    expect(bodyIds(fixture)).toEqual(['1', '3', '2']);
  });

  it('ignores clicks on non-sortable headers', () => {
    const fixture = createFixture();
    clickHeader(fixture, 3);
    expect(bodyIds(fixture)).toEqual(['1', '2', '3']);
    expect(fixture.componentInstance.lastSort).toBeNull();
  });
});
