import type { Routes } from '@angular/router';

import { AppShell } from './core/shell/app-shell';
import { PlaceholderPage } from './core/shell/placeholder-page';

// Guards (P1.4) are applied here in P3.1, once /login exists as a shell sibling.
export const routes: Routes = [
  {
    path: '',
    component: AppShell,
    children: [
      { path: '', redirectTo: 'dashboard/store-operations', pathMatch: 'full' },
      { path: 'dashboard/store-operations', component: PlaceholderPage },
      { path: 'catalog', component: PlaceholderPage },
      { path: 'suppliers', component: PlaceholderPage },
      { path: 'inventory', component: PlaceholderPage },
      { path: 'sales', component: PlaceholderPage },
      { path: 'purchases', component: PlaceholderPage },
      { path: 'finance', component: PlaceholderPage },
      { path: 'expenses', component: PlaceholderPage },
      { path: 'hr', component: PlaceholderPage },
      { path: 'host/admin/tenants', component: PlaceholderPage },
    ],
  },
  { path: '**', redirectTo: '' },
];
