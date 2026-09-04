import { buildNavItems } from './nav-items';

const ALL_FEATURES = [
  'catalog',
  'suppliers',
  'inventory',
  'sales',
  'purchases',
  'finance',
  'expenses',
  'hr',
  'settings',
] as const;

describe('buildNavItems', () => {
  it('returns dashboard only when no claims exist', () => {
    expect(buildNavItems([]).map((item) => item.key)).toEqual(['dashboard', 'gold-prices']);
  });

  it('returns the item order defined by the plan', () => {
    const keys = buildNavItems([...ALL_FEATURES], true, ['store_admin']).map((item) => item.key);
    expect(keys).toEqual([
      'dashboard',
      'gold-prices',
      'catalog',
      'suppliers',
      'inventory',
      'sales',
      'purchases',
      'finance',
      'expenses',
      'hr',
      'settings',
      'permissions',
      'host-admin',
    ]);
  });

  it('drops the catalog item when the catalog claim is absent', () => {
    const keys = buildNavItems(
      ALL_FEATURES.filter((f) => f !== 'catalog'),
      true,
      ['store_admin'],
    ).map((item) => item.key);
    expect(keys).not.toContain('catalog');
    expect(keys).toContain('sales');
    expect(keys).toContain('host-admin');
  });

  it('never shows host-admin to non-host sessions regardless of claims', () => {
    const keys = buildNavItems([...ALL_FEATURES], false).map((item) => item.key);
    expect(keys).not.toContain('host-admin');
  });

  it('keeps dashboard visible to every authenticated user', () => {
    const keys = buildNavItems([], true).map((item) => item.key);
    expect(keys).toContain('dashboard');
  });

  it('exposes Arabic labels, icons and routes on every item', () => {
    for (const item of buildNavItems([...ALL_FEATURES], true, ['store_admin'])) {
      expect(item.label.length).toBeGreaterThan(0);
      expect(item.icon.length).toBeGreaterThan(0);
      expect(item.route.startsWith('/')).toBe(true);
    }
  });

  it('hides hr from non-store_admin even with hr feature', () => {
    const keys = buildNavItems([...ALL_FEATURES], false, []).map((item) => item.key);
    expect(keys).not.toContain('hr');
    expect(keys).not.toContain('permissions');
    const adminKeys = buildNavItems([...ALL_FEATURES], false, ['store_admin']).map((item) => item.key);
    expect(adminKeys).toContain('hr');
    expect(adminKeys).toContain('permissions');
  });
});
