import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/gold-price-dashboard/gold-price-dashboard.component').then(m => m.GoldPriceDashboardComponent) },
  { path: 'history', loadComponent: () => import('./pages/gold-price-history/gold-price-history.component').then(m => m.GoldPriceHistoryComponent) },
] satisfies Routes;
