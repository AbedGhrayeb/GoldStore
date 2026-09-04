import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import { ToastStore, type ToastType } from '../../core/toast/toast-store';
import { resolveIcon } from './icon-registry';

const TOAST_STYLES: Readonly<Record<ToastType, string>> = {
  success: 'border-s-success bg-success/5',
  error: 'border-s-error bg-error/5',
  info: 'border-s-gold bg-gold-container/20',
};

const TOAST_ICONS: Readonly<Record<ToastType, string>> = {
  success: 'check-circle',
  error: 'x-circle',
  info: 'info',
};

@Component({
  selector: 'app-toast',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div
      class="pointer-events-none fixed inset-x-0 top-4 z-100 flex flex-col items-center gap-2 px-4"
    >
      @for (toast of toasts(); track toast.id) {
        <div
          class="pointer-events-auto flex w-full max-w-md items-center gap-3 rounded-lg border border-gray-200 border-s-4 bg-card px-4 py-3 shadow-lg"
          [class]="TOAST_STYLES[toast.type]"
          role="status"
        >
          <lucide-icon [img]="iconFor(toast.type)" [size]="18" class="shrink-0" />
          <p class="flex-1 text-sm text-gray-800">{{ toast.message }}</p>
          <button
            type="button"
            class="rounded-md p-1 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700"
            aria-label="إغلاق"
            (click)="dismiss(toast.id)"
          >
            <lucide-icon [img]="xIcon" [size]="14" />
          </button>
        </div>
      }
    </div>
  `,
})
export class Toast {
  private readonly store = inject(ToastStore);

  readonly toasts = this.store.toasts;
  readonly TOAST_STYLES = TOAST_STYLES;

  readonly xIcon = resolveIcon('x');

  iconFor(type: ToastType): ReturnType<typeof resolveIcon> {
    return resolveIcon(TOAST_ICONS[type]);
  }

  dismiss(id: number): void {
    this.store.dismiss(id);
  }
}
