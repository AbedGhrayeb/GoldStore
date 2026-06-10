import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/users-list/users-list.component').then(m => m.UsersListComponent) },
] satisfies Routes;
