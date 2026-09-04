import type { Routes } from '@angular/router';

import { AppShell } from './core/shell/app-shell';
import { authGuard } from './core/guards/auth.guard';
import { featureGuard } from './core/guards/feature.guard';
import { hostGuard } from './core/guards/host.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'تسجيل الدخول | بريق',
    loadComponent: () =>
      import('./features/auth/tenant-login-page').then((page) => page.TenantLoginPage),
  },
  {
    path: 'enroll-phone',
    title: 'تفعيل المصادقة الثنائية | بريق',
    loadComponent: () =>
      import('./features/auth/enroll-phone-page').then((page) => page.EnrollPhonePage),
  },
  {
    path: 'verify-2fa',
    title: 'التحقق بخطوتين | بريق',
    loadComponent: () =>
      import('./features/auth/verify-2fa-page').then((page) => page.Verify2faPage),
  },
  {
    path: 'forgot-password',
    title: 'نسيت كلمة المرور | بريق',
    loadComponent: () =>
      import('./features/auth/forgot-password-page').then((page) => page.ForgotPasswordPage),
  },
  {
    path: 'host/login',
    title: 'دخول إدارة المنصة | بريق',
    loadComponent: () =>
      import('./features/auth/host-login-page').then((page) => page.HostLoginPage),
  },
  {
    path: 'host/admin/tenants',
    title: 'إدارة المنصة | بريق',
    canActivate: [hostGuard],
    loadComponent: () =>
      import('./features/host-admin/host-admin-page').then((page) => page.HostAdminPage),
  },
  {
    path: '403',
    title: 'غير مصرح | بريق',
    loadComponent: () => import('./core/shell/forbidden-page').then((m) => m.ForbiddenPage),
  },
  {
    path: '',
    component: AppShell,
    canActivateChild: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard/store-operations', pathMatch: 'full' },
      {
        path: 'gold-prices',
        title: 'أسعار الذهب | بريق',
        loadComponent: () =>
          import('./features/gold-prices/gold-prices-page').then((page) => page.GoldPricesPage),
      },
      {
        path: 'dashboard/store-operations',
        title: 'عمليات المتجر | بريق',
        loadComponent: () =>
          import('./features/dashboard/store-operations-page').then(
            (page) => page.StoreOperationsPage,
          ),
      },
      {
        path: 'catalog/categories',
        title: 'الكتالوج | بريق',
        canActivate: [featureGuard('catalog')],
        loadComponent: () =>
          import('./features/catalog/categories-page').then((page) => page.CategoriesPage),
      },
      {
        path: 'suppliers',
        title: 'الموردون | بريق',
        canActivate: [featureGuard('suppliers')],
        loadComponent: () =>
          import('./features/suppliers/suppliers-page').then((page) => page.SuppliersPage),
      },
      {
        path: 'inventory',
        title: 'المخزون | بريق',
        canActivate: [featureGuard('inventory')],
        loadComponent: () =>
          import('./features/inventory/inventory-page').then((page) => page.InventoryPage),
      },
      {
        path: 'sales',
        title: 'المبيعات | بريق',
        canActivate: [featureGuard('sales')],
        loadComponent: () =>
          import('./features/sales/sales-page').then((page) => page.SalesPage),
      },
      {
        path: 'purchases',
        title: 'مشتريات الذهب | بريق',
        canActivate: [featureGuard('purchases')],
        loadComponent: () =>
          import('./features/purchases/purchases-page').then((page) => page.PurchasesPage),
      },
      {
        path: 'finance',
        title: 'المالية | بريق',
        canActivate: [featureGuard('finance')],
        loadComponent: () =>
          import('./features/finance/finance-page').then((page) => page.FinancePage),
      },
      {
        path: 'expenses',
        title: 'المصروفات | بريق',
        canActivate: [featureGuard('expenses')],
        loadComponent: () =>
          import('./features/expenses/expenses-page').then((page) => page.ExpensesPage),
      },
      {
        path: 'hr',
        title: 'الموظفون | بريق',
        canActivate: [featureGuard('hr'), roleGuard('store_admin')],
        loadComponent: () => import('./features/hr/hr-page').then((page) => page.HrPage),
      },
      {
        path: 'settings/users',
        title: 'المستخدمون والإعدادات | بريق',
        canActivate: [featureGuard('settings')],
        loadComponent: () =>
          import('./features/settings/users-page').then((page) => page.UsersPage),
      },
      {
        path: 'settings/permissions',
        title: 'إدارة الصلاحيات | بريق',
        canActivate: [featureGuard('settings'), roleGuard('store_admin')],
        loadComponent: () =>
          import('./features/settings/permissions-page').then((page) => page.PermissionsPage),
      },
    ],
  },
  {
    path: '**',
    title: 'غير موجود | بريق',
    loadComponent: () => import('./core/shell/not-found-page').then((m) => m.NotFoundPage),
  },
];
