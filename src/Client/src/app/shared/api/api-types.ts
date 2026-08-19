/**
 * Hand-written types for API surfaces the OpenAPI document cannot express. The server's
 * GET /api/v1/auth/me returns an anonymous object, so its shape is pinned here to match
 * WebUI.Endpoints.AuthEndpoints.Me.
 */
export interface MeResponse {
  userId: string;
  email: string | null;
  tenantId: string | null;
  tenantKey: string | null;
  roles: string[];
  permissions: string[];
}

/** Feature permission keys as seeded in Domain.Tenants.Features (mirrors the server policy names). */
export type FeatureKey =
  | 'catalog'
  | 'suppliers'
  | 'inventory'
  | 'finance'
  | 'sales'
  | 'purchases'
  | 'hr'
  | 'expenses'
  | 'reports'
  | 'settings';
