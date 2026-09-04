import { environment } from '../environment/environment';

/** Resolves an API path against the environment base URL so MSW handlers match ApiClient requests. */
export function apiUrl(path: string): string {
  return `${environment.API_BASE_URL}${path}`;
}
