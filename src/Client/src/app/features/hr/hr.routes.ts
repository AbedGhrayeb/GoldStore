import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/employees-list/employees-list.component').then(m => m.EmployeesListComponent) },
  { path: ':id', loadComponent: () => import('./pages/employee-detail/employee-detail.component').then(m => m.EmployeeDetailComponent) },
  { path: 'create', loadComponent: () => import('./pages/employee-form/employee-form.component').then(m => m.EmployeeFormComponent) },
  { path: ':id/edit', loadComponent: () => import('./pages/employee-form/employee-form.component').then(m => m.EmployeeFormComponent) },
] satisfies Routes;
