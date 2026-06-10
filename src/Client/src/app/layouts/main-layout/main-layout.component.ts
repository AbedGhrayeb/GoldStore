import { Component, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, HeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-sidebar [isOpen]="sidebarOpen()" (closeMenu)="sidebarOpen.set(false)" />
    <div class="lg:mr-[280px] min-h-screen bg-surface-base flex flex-col">
      <app-header (toggleMenu)="sidebarOpen.set(!sidebarOpen())" />
      <main class="flex-1">
        <router-outlet />
      </main>
    </div>
  `,
})
export class MainLayoutComponent {
  protected readonly sidebarOpen = signal(false);
}
