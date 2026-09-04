import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { OfflineBanner } from './core/shell/offline-banner';
import { PwaUpdateBanner } from './core/pwa/update-banner';
import { PwaUpdateService } from './core/pwa/pwa-update.service';
import { Toast } from './shared/ui';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, OfflineBanner, PwaUpdateBanner],
  template: `
    <app-pwa-update-banner />
    <app-offline-banner />
    <app-toast />
    <router-outlet />
  `,
})
export class App {
  // Warm up SwUpdate so versionUpdates subscription + periodic checks start
  private readonly _pwa = inject(PwaUpdateService);
}
