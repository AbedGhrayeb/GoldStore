import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="absolute inset-0 bg-black/40"></div>
        <div class="relative bg-surface-card rounded-xl shadow-modal border border-gold-border/20 w-full max-w-sm mx-4 p-6 text-center">
          <div class="w-12 h-12 rounded-full bg-error-bg flex items-center justify-center mx-auto mb-4">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="text-error"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
          </div>
          <h3 class="text-lg font-semibold text-text-primary mb-2">{{ title() }}</h3>
          <p class="text-sm text-text-secondary mb-6">{{ message() }}</p>
          <div class="flex items-center justify-center gap-3">
            <button (click)="cancel.emit()" class="px-4 py-2 text-sm font-medium text-text-secondary bg-surface-hover rounded-lg hover:bg-gray-200 transition-colors">إلغاء</button>
            <button (click)="confirm.emit()" class="px-4 py-2 text-sm font-medium text-white bg-error rounded-lg hover:bg-error/90 transition-colors">{{ confirmText() }}</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ConfirmationDialogComponent {
  readonly isOpen = input(false);
  readonly title = input('تأكيد الحذف');
  readonly message = input('هل أنت متأكد من حذف هذا العنصر؟');
  readonly confirmText = input('حذف');
  readonly confirm = output<void>();
  readonly cancel = output<void>();
}
