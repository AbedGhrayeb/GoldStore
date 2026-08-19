import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { OfflineBanner } from './core/shell/offline-banner';
import { Toast } from './shared/ui';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, OfflineBanner],
  template: `
    <app-offline-banner />
    <app-toast />
    <router-outlet />
  `,
})
export class App {}
