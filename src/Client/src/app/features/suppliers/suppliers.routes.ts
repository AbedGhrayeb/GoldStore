import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/suppliers-list/suppliers-list.component').then(m => m.SuppliersListComponent) },
  { path: ':id', loadComponent: () => import('./pages/supplier-detail/supplier-detail.component').then(m => m.SupplierDetailComponent) },
] satisfies Routes;
