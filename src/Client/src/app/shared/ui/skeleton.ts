import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="block animate-pulse bg-gray-200"
      [style.width]="width()"
      [style.height]="height()"
      [class]="rounded()"
    ></span>
  `,
})
export class Skeleton {
  readonly width = input('100%');
  readonly height = input('1rem');
  readonly rounded = input('rounded-md');
}
