import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="absolute inset-0 bg-black/40" (click)="close.emit()"></div>
        <div class="relative bg-surface-card rounded-xl shadow-modal border border-gold-border/20 w-full max-w-lg mx-4 max-h-[85vh] overflow-y-auto">
          @if (title()) {
            <div class="flex items-center justify-between px-6 py-4 border-b border-gold-border/20">
              <h3 class="text-lg font-semibold text-text-primary">{{ title() }}</h3>
              <button (click)="close.emit()" class="p-1 rounded-lg hover:bg-surface-hover text-text-muted transition-colors">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
              </button>
            </div>
          }
          <div class="p-6">
            <ng-content />
          </div>
        </div>
      </div>
    }
  `,
})
export class ModalComponent {
  readonly isOpen = input(false);
  readonly title = input<string>();
  readonly close = output<void>();
}
