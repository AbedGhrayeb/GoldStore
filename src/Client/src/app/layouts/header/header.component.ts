import { Component, inject, output, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="h-16 bg-surface-card border-b border-gold-border/20 flex items-center justify-between px-4 lg:px-6 sticky top-0 z-10">
      <div class="flex items-center gap-3">
        <button (click)="toggleMenu.emit()" class="lg:hidden p-2 rounded-lg hover:bg-surface-hover text-text-secondary transition-colors">
          <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="18" x2="21" y2="18"/></svg>
        </button>
        <h2 class="text-base font-semibold text-text-primary"><ng-content /></h2>
      </div>
      <div class="flex items-center gap-2">
        <a routerLink="/profile" class="flex items-center gap-2 px-3 py-1.5 rounded-lg hover:bg-surface-hover transition-colors">
          <div class="w-8 h-8 rounded-full bg-gold-primary/20 flex items-center justify-center">
            <span class="text-xs font-bold text-gold-primary-dark">{{ userInitial() }}</span>
          </div>
          <div class="text-right">
            <p class="text-sm font-medium text-text-primary leading-tight">{{ auth.user()?.name || 'مستخدم' }}</p>
            <p class="text-xs text-text-muted leading-tight">{{ auth.user()?.role || '' }}</p>
          </div>
        </a>
        <button (click)="auth.logout()" class="p-2 rounded-lg hover:bg-surface-hover text-text-muted hover:text-error transition-colors" title="تسجيل خروج">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
        </button>
      </div>
    </header>
  `,
})
export class HeaderComponent {
  protected readonly auth = inject(AuthService);
  readonly toggleMenu = output<void>();

  protected userInitial(): string {
    const name = this.auth.user()?.name;
    return name ? name.charAt(0) : 'م';
  }
}
