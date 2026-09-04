import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule, Download } from 'lucide-angular';

import { PwaInstallService } from './pwa-install.service';

@Component({
  selector: 'app-pwa-install-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    @if (installer.canInstall()) {
      <button
        type="button"
        class="flex items-center gap-2 rounded-full border border-gold/30 bg-card px-3 py-1.5 text-xs font-medium text-gray-700 transition hover:bg-gold-container/40"
        (click)="install()"
      >
        <lucide-icon [img]="downloadIcon" [size]="14" />
        تثبيت التطبيق
      </button>
    }
  `,
})
export class PwaInstallButton {
  readonly installer = inject(PwaInstallService);
  readonly downloadIcon = Download;

  async install(): Promise<void> {
    await this.installer.promptInstall();
  }
}
