import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/sales-list/sales-list.component').then(m => m.SalesListComponent) },
  { path: 'create', loadComponent: () => import('./pages/create-sale/create-sale.component').then(m => m.CreateSaleComponent) },
  { path: ':id', loadComponent: () => import('./pages/sale-detail/sale-detail.component').then(m => m.SaleDetailComponent) },
] satisfies Routes;
