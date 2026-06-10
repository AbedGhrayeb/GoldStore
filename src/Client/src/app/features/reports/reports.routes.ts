import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/reports-list/reports-list.component').then(m => m.ReportsListComponent) },
] satisfies Routes;
