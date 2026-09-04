import type { FeatureKey } from '../../shared/api/api-types';

/** Feature permission keys required to see a nav item (bare claim keys, no `feature:` prefix). */
export type NavItemKey = 'dashboard' | 'gold-prices' | 'host-admin' | 'permissions' | FeatureKey;

export interface NavItem {
  key: NavItemKey;
  /** Arabic label rendered in the RTL sidebar. */
  label: string;
  /** Lucide icon name; resolved to a component by the shell (P2.1). */
  icon: string;
  /** First-level route of the feature (see §P3). */
  route: string;
}

interface NavItemDefinition extends NavItem {
  /** Bare feature claim required; undefined = auth only. */
  feature?: FeatureKey;
  /** Role required (e.g. store_admin); undefined = no role check. */
  requiredRole?: string;
  /** Permission required (e.g. sales.view); undefined = no permission check. */
  requiredPermission?: string;
  /** Whether to disable instead of hide when permission missing (default: hide) */
  disableInsteadOfHide?: boolean;
  /** Visible only to host admins (cookie session), never via claims. */
  hostAdmin?: boolean;
}

const DEFINITIONS: readonly NavItemDefinition[] = [
  {
    key: 'dashboard',
    label: 'لوحة التحكم',
    icon: 'layout-dashboard',
    route: '/dashboard/store-operations',
  },
  {
    key: 'gold-prices',
    label: 'أسعار الذهب',
    icon: 'coins',
    route: '/gold-prices',
  },
  {
    key: 'catalog',
    label: 'الكتالوج',
    icon: 'tags',
    route: '/catalog/categories',
    feature: 'catalog',
    requiredPermission: 'inventory.view',
  },
  { key: 'suppliers', label: 'الموردون', icon: 'truck', route: '/suppliers', feature: 'suppliers', requiredPermission: 'suppliers.view' },
  { key: 'inventory', label: 'المخزون', icon: 'boxes', route: '/inventory', feature: 'inventory', requiredPermission: 'inventory.view' },
  { key: 'sales', label: 'المبيعات', icon: 'receipt', route: '/sales', feature: 'sales', requiredPermission: 'sales.view' },
  {
    key: 'purchases',
    label: 'المشتريات',
    icon: 'shopping-bag',
    route: '/purchases',
    feature: 'purchases',
    requiredPermission: 'purchases.view',
  },
  { key: 'finance', label: 'المالية', icon: 'wallet', route: '/finance', feature: 'finance', requiredPermission: 'finance.view' },
  {
    key: 'expenses',
    label: 'المصروفات',
    icon: 'trending-down',
    route: '/expenses',
    feature: 'expenses',
    requiredPermission: 'expenses.view',
  },
  { key: 'hr', label: 'الموظفون', icon: 'users', route: '/hr', feature: 'hr', requiredPermission: 'employees.view', requiredRole: 'store_admin' },
  {
    key: 'settings',
    label: 'الإعدادات',
    icon: 'settings',
    route: '/settings/users',
    feature: 'settings',
    requiredPermission: 'settings.view',
  },
  {
    key: 'permissions',
    label: 'إدارة الصلاحيات',
    icon: 'shield',
    route: '/settings/permissions',
    feature: 'settings',
    requiredPermission: 'settings.manage',
    requiredRole: 'store_admin',
  },
  {
    key: 'host-admin',
    label: 'إدارة المنصة',
    icon: 'server',
    route: '/host/admin/tenants',
    hostAdmin: true,
  },
];

/**
 * Builds the sidebar navigation from the current session. Items without a `feature` (dashboard)
 * appear for any authenticated user; feature items require the matching claim; host-admin is
 * gated by the cookie session only. If `requiredRole` is set, the user must also hold that role.
 */
export function buildNavItems(
  permissions: readonly string[],
  isHostAdmin = false,
  roles: readonly string[] = [],
): NavItem[] {
  return DEFINITIONS.filter((item) => {
    if (item.hostAdmin === true) return isHostAdmin;
    if (item.feature !== undefined && !permissions.includes(item.feature)) return false;
    if (item.requiredRole !== undefined && !roles.includes(item.requiredRole)) return false;
    if (item.requiredPermission !== undefined && !permissions.includes(item.requiredPermission)) return false;
    return true;
  });
}

/** Returns true if item is visible but should be disabled due to missing permission (for future disable vs hide) */
export function isNavItemDisabled(
  item: NavItemDefinition,
  permissions: readonly string[],
): boolean {
  if (item.requiredPermission !== undefined && !permissions.includes(item.requiredPermission)) return true;
  return false;
}
