import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  signal,
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
        class="fixed inset-0 z-50 flex items-end justify-center bg-black/50 backdrop-blur-[2px] p-0 sm:items-center sm:p-4 sm:bg-black/40 animate-backdropIn"
        tabindex="-1"
        (click)="onBackdropClick($event)"
        (keydown.escape)="onEscape()"
      >
        <section
          role="dialog"
          aria-modal="true"
          [class]="dialogClass()"
          [style.transform]="dragStyle()"
          [style.opacity]="dragOpacity()"
          (transitionend)="onTransitionEnd($event)"
        >
          <!-- Native handle bar (mobile only) -->
          <div
            class="flex shrink-0 flex-col items-center gap-2 border-b border-gray-100 bg-card pt-2.5 sm:hidden touch-manipulation select-none"
            (touchstart)="onDragStart($event)"
            (touchmove)="onDragMove($event)"
            (touchend)="onDragEnd()"
          >
            <span class="h-1.5 w-10 rounded-full bg-gray-300"></span>
            <div class="h-1 w-full"></div>
          </div>

          <header
            class="flex shrink-0 items-start justify-between gap-3 border-b border-gold-container/50 bg-gradient-to-b from-gold-container/40 to-gold-container/20 px-4 py-4 sm:px-6 sm:py-5 touch-manipulation select-none"
            (touchstart)="onDragStart($event)"
            (touchmove)="onDragMove($event)"
            (touchend)="onDragEnd()"
          >
            <div class="flex min-w-0 flex-1 items-center gap-3">
              @if (headerIcon(); as icon) {
                <span
                  aria-hidden="true"
                  class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-card text-gold shadow-sm ring-1 ring-black/5 sm:h-11 sm:w-11"
                >
                  <lucide-icon [img]="icon" [size]="18" />
                </span>
              }
              <div class="min-w-0 flex-1">
                @if (subtitle(); as subtitle) {
                  <p class="truncate text-xs font-medium text-gray-500">{{ subtitle }}</p>
                }
                <h2 class="truncate text-[15px] font-bold text-gray-900 sm:text-base">{{ title() }}</h2>
              </div>
            </div>
            <button
              type="button"
              class="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-card text-gray-500 shadow-sm ring-1 ring-black/5 transition hover:bg-white hover:text-gray-700 active:scale-95 sm:h-10 sm:w-10"
              aria-label="إغلاق"
              (click)="close()"
            >
              <lucide-icon [img]="xIcon" [size]="18" />
            </button>
          </header>

          <div class="flex-1 overflow-y-auto overscroll-contain p-4 sm:p-6" style=" -webkit-overflow-scrolling: touch;">
            <ng-content />
          </div>

          @if (hasFooter()) {
            <footer
              class="shrink-0 flex flex-col-reverse sm:flex-row justify-end gap-2 border-t border-gray-100 bg-gray-50/80 px-4 py-3 backdrop-blur sm:bg-card sm:px-6 sm:py-4"
              style="padding-bottom: max(0.75rem, env(safe-area-inset-bottom))"
            >
              <ng-content select="[dialog-footer]" />
            </footer>
          }
        </section>
      </div>
    }
  `,
  styles: `
    @keyframes backdropIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes sheetIn {
      from { transform: translateY(100%); }
      to { transform: translateY(0); }
    }
    :host {
      --sheet-radius: 28px;
    }
    section {
      animation: sheetIn 0.34s cubic-bezier(0.32, 0.72, 0, 1);
      will-change: transform;
    }
    @media (min-width: 640px) {
      section {
        animation: sheetIn 0.24s cubic-bezier(0.32, 0.72, 0, 1);
      }
      @keyframes sheetIn {
        from { opacity: 0; transform: translateY(12px) scale(0.97); }
        to { opacity: 1; transform: translateY(0) scale(1); }
      }
    }
    .animate-backdropIn {
      animation: backdropIn 0.2s ease-out;
    }
    /* Native mobile footer: stack actions full-width */
    @media (max-width: 639px) {
      footer [dialog-footer] {
        display: flex !important;
        flex-direction: column-reverse !important;
        width: 100% !important;
        gap: 0.5rem !important;
        justify-content: stretch !important;
      }
      footer [dialog-footer] app-button,
      footer [dialog-footer] button {
        width: 100%;
      }
    }
  `,
})
export class Dialog {
  private readonly elementRef = inject(ElementRef);

  readonly open = input(false);
  readonly title = input('');
  readonly subtitle = input('');
  /** Lucide icon name rendered in a gold chip next to the title (see icon-registry). */
  readonly icon = input<string>();
  readonly dismissible = input(true);
  /** Tailwind max-width utility for the modal (default `max-w-lg`; e.g. `max-w-3xl` for dense forms). */
  readonly maxWidth = input('max-w-lg');

  readonly openChange = output<boolean>();

  readonly xIcon = resolveIcon('x');
  readonly headerIcon = computed(() => (this.icon() ? resolveIcon(this.icon()!) : undefined));
  readonly dialogClass = computed(
    () =>
      `w-full ${this.maxWidth()} max-h-[96dvh] sm:max-h-[88vh] overflow-hidden bg-card shadow-2xl flex flex-col rounded-t-[28px] sm:rounded-2xl will-change-transform`,
  );

  // Drag-to-dismiss (mobile sheet)
  private readonly dragY = signal(0);
  private readonly dragging = signal(false);
  private startY = 0;
  private startTime = 0;

  dragStyle = computed(() => {
    const y = this.dragY();
    return y ? `translateY(${y}px)` : '';
  });

  dragOpacity = computed(() => {
    const y = this.dragY();
    if (!y) return '';
    // fade backdrop slightly while dragging down
    return String(Math.max(0.3, 1 - y / 400));
  });

  onDragStart(event: TouchEvent): void {
    if (window.innerWidth >= 640) return;
    this.dragging.set(true);
    this.startY = event.touches[0].clientY;
    this.startTime = Date.now();
  }

  onDragMove(event: TouchEvent): void {
    if (!this.dragging()) return;
    const currentY = event.touches[0].clientY;
    const delta = currentY - this.startY;
    if (delta < 0) return; // only drag down
    // rubber band: resist after 120px
    const y = delta > 120 ? 120 + (delta - 120) * 0.3 : delta;
    this.dragY.set(y);
    // prevent scroll while dragging sheet
    if (y > 10) event.preventDefault();
  }

  onDragEnd(): void {
    if (!this.dragging()) return;
    this.dragging.set(false);
    const y = this.dragY();
    const elapsed = Date.now() - this.startTime;
    const velocity = y / Math.max(1, elapsed); // px/ms
    const shouldClose = y > 120 || (y > 60 && velocity > 0.4);
    if (shouldClose && this.dismissible()) {
      // haptic
      try {
        navigator.vibrate?.(10);
      } catch {}
      this.close();
    }
    this.dragY.set(0);
  }

  onTransitionEnd(_event: TransitionEvent): void {}

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
    this.dragY.set(0);
    this.dragging.set(false);
    this.openChange.emit(false);
  }

  hasFooter(): boolean {
    const host = this.elementRef.nativeElement as HTMLElement;
    return host.querySelector('[dialog-footer]') !== null;
  }
}
