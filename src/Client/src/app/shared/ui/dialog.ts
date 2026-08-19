import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import { resolveIcon } from './icon-registry';

@Component({
  selector: 'app-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    @if (open()) {
      <div
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
        tabindex="-1"
        (click)="onBackdropClick($event)"
        (keydown.escape)="onEscape()"
      >
        <section
          role="dialog"
          aria-modal="true"
          class="w-full max-w-lg rounded-modal bg-card p-6 shadow-xl"
        >
          <header class="mb-4 flex items-center justify-between gap-4">
            <h2 class="text-base font-semibold text-gray-900">{{ title() }}</h2>
            <button
              type="button"
              class="rounded-md p-1 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700"
              aria-label="إغلاق"
              (click)="close()"
            >
              <lucide-icon [img]="xIcon" [size]="18" />
            </button>
          </header>
          <div class="max-h-[70vh] overflow-y-auto">
            <ng-content />
          </div>
          @if (hasFooter()) {
            <footer class="mt-6 flex justify-end gap-3">
              <ng-content select="[dialog-footer]" />
            </footer>
          }
        </section>
      </div>
    }
  `,
})
export class Dialog {
  private readonly elementRef = inject(ElementRef);

  readonly open = input(false);
  readonly title = input('');
  readonly dismissible = input(true);

  readonly openChange = output<boolean>();

  readonly xIcon = resolveIcon('x');

  onEscape(): void {
    if (this.dismissible()) {
      this.close();
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if (this.dismissible() && event.target === event.currentTarget) {
      this.close();
    }
  }

  close(): void {
    this.openChange.emit(false);
  }

  hasFooter(): boolean {
    const host = this.elementRef.nativeElement as HTMLElement;
    return host.querySelector('[dialog-footer]') !== null;
  }
}
