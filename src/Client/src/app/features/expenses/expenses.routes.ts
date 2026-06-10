import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/expenses-list/expenses-list.component').then(m => m.ExpensesListComponent) },
  { path: 'create', loadComponent: () => import('./pages/create-expense/create-expense.component').then(m => m.CreateExpenseComponent) },
] satisfies Routes;
