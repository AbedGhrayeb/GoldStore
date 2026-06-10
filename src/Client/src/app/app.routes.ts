import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { loginGuard } from './core/guards/login.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [loginGuard],
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layouts/main-layout/main-layout.component').then(m => m.MainLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/pages/dashboard-page/dashboard-page.component').then(m => m.DashboardPageComponent),
      },
      {
        path: 'inventory',
        loadChildren: () => import('./features/inventory/inventory.routes'),
      },
      {
        path: 'sales',
        loadChildren: () => import('./features/sales/sales.routes'),
      },
      {
        path: 'suppliers',
        loadChildren: () => import('./features/suppliers/suppliers.routes'),
      },
      {
        path: 'gold-purchases',
        loadChildren: () => import('./features/gold-purchases/gold-purchases.routes'),
      },
      {
        path: 'finance',
        loadChildren: () => import('./features/finance/finance.routes'),
      },
      {
        path: 'expenses',
        loadChildren: () => import('./features/expenses/expenses.routes'),
      },
      {
        path: 'hr',
        loadChildren: () => import('./features/hr/hr.routes'),
      },
      {
        path: 'gold-price',
        loadChildren: () => import('./features/gold-price/gold-price.routes'),
      },
      {
        path: 'reports',
        loadChildren: () => import('./features/reports/reports.routes'),
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes'),
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/settings/pages/settings-page/settings-page.component').then(m => m.SettingsPageComponent),
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/auth/profile/profile.component').then(m => m.ProfileComponent),
      },
      {
        path: 'change-password',
        loadComponent: () => import('./features/auth/change-password/change-password.component').then(m => m.ChangePasswordComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
