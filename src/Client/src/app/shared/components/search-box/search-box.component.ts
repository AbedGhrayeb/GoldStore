import { Component, model, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-search-box',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative">
      <svg class="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-muted" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
      <input
        [ngModel]="searchTerm()"
        (ngModelChange)="searchTerm.set($event)"
        type="text"
        placeholder="{{ placeholder() }}"
        class="w-full py-2 pr-10 pl-4 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200 placeholder:text-text-muted"
      />
    </div>
  `,
})
export class SearchBoxComponent {
  readonly searchTerm = model('');
  readonly placeholder = model('بحث...');
}
