import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/gold-purchases-list/gold-purchases-list.component').then(m => m.GoldPurchasesListComponent) },
  { path: 'create', loadComponent: () => import('./pages/gold-purchase-form/gold-purchase-form.component').then(m => m.GoldPurchaseFormComponent) },
] satisfies Routes;
