import { Routes } from '@angular/router';

export default [
  { path: '', loadComponent: () => import('./pages/finance-overview/finance-overview.component').then(m => m.FinanceOverviewComponent) },
  { path: 'accounts', loadComponent: () => import('./pages/accounts/accounts.component').then(m => m.AccountsComponent) },
  { path: 'ledger', loadComponent: () => import('./pages/ledger/ledger.component').then(m => m.LedgerComponent) },
] satisfies Routes;
