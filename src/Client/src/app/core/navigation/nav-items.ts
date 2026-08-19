import type { FeatureKey } from '../../shared/api/api-types';

/** Feature permission keys required to see a nav item (bare claim keys, no `feature:` prefix). */
export type NavItemKey = 'dashboard' | 'host-admin' | FeatureKey;

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
    key: 'catalog',
    label: 'الكتالوج',
    icon: 'tags',
    route: '/catalog/categories',
    feature: 'catalog',
  },
  { key: 'suppliers', label: 'الموردون', icon: 'truck', route: '/suppliers', feature: 'suppliers' },
  { key: 'inventory', label: 'المخزون', icon: 'boxes', route: '/inventory', feature: 'inventory' },
  { key: 'sales', label: 'المبيعات', icon: 'receipt', route: '/sales', feature: 'sales' },
  {
    key: 'purchases',
    label: 'المشتريات',
    icon: 'shopping-bag',
    route: '/purchases',
    feature: 'purchases',
  },
  { key: 'finance', label: 'المالية', icon: 'wallet', route: '/finance', feature: 'finance' },
  {
    key: 'expenses',
    label: 'المصروفات',
    icon: 'trending-down',
    route: '/expenses',
    feature: 'expenses',
  },
  { key: 'hr', label: 'الموظفون', icon: 'users', route: '/hr', feature: 'hr' },
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
 * gated by the cookie session only.
 */
export function buildNavItems(permissions: readonly string[], isHostAdmin = false): NavItem[] {
  return DEFINITIONS.filter((item) =>
    item.hostAdmin === true
      ? isHostAdmin
      : item.feature === undefined || permissions.includes(item.feature),
  );
}
