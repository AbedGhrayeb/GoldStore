import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Temporary stand-in for feature pages. Replaced module by module in §P3.
 */
@Component({
  selector: 'app-placeholder-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rounded-lg bg-card p-6 shadow-sm">
      <h1 class="text-xl font-semibold">قيد التنفيذ</h1>
      <p class="mt-2 text-sm text-gray-500">هذه الصفحة ستُبنى ضمن مرحلة P3 من خطة الواجهة.</p>
    </div>
  `,
})
export class PlaceholderPage {}
