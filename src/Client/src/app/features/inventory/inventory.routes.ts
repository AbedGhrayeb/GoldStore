import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/inventory-list/inventory-list.component').then(m => m.InventoryListComponent) },
  { path: 'create', loadComponent: () => import('./pages/inventory-form/inventory-form.component').then(m => m.InventoryFormComponent) },
  { path: ':id', loadComponent: () => import('./pages/inventory-detail/inventory-detail.component').then(m => m.InventoryDetailComponent) },
  { path: ':id/edit', loadComponent: () => import('./pages/inventory-form/inventory-form.component').then(m => m.InventoryFormComponent) },
] satisfies Routes;
