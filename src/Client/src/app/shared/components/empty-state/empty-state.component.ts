import { Component, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center py-16 text-center">
      <div class="w-16 h-16 rounded-full bg-gold-primary/10 flex items-center justify-center mb-4">
        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="text-gold-primary"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
      </div>
      <h3 class="text-base font-semibold text-text-primary mb-1">{{ title() }}</h3>
      <p class="text-sm text-text-muted">{{ message() }}</p>
    </div>
  `,
})
export class EmptyStateComponent {
  readonly title = input('لا توجد بيانات');
  readonly message = input('لم يتم العثور على أي عناصر بعد.');
}
