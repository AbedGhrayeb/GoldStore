import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { ApiRequestOptions } from '../../core/http/api-client.service';
import { ApiClient } from '../../core/http/api-client.service';
import type { components } from '../../shared/api/schema';

export type CategoryResponse = components['schemas']['CategoryResponse'];
export type CreateCategoryRequest = components['schemas']['CreateCategoryRequest'];
export type UpdateCategoryRequest = components['schemas']['UpdateCategoryRequest'];

/** Server-side category payload shared by create and update (schema-optional fields are required here). */
export interface CategoryInput {
  name: string;
  description: string | null;
  parentCategoryId: string | null;
  isActive: boolean;
  weightInGrams: number;
  karat: number;
}

/**
 * Typed transport for the categories group (`/api/v1/categories`, feature: catalog).
 * Components never call this directly — they go through {@link CatalogStore}.
 */
@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly api = inject(ApiClient);
  private static readonly base = '/api/v1/categories';

  getCategories(options?: ApiRequestOptions): Observable<CategoryResponse[]> {
    return this.api.get<CategoryResponse[]>(CatalogApi.base, options);
  }

  createCategory(input: CategoryInput, options?: ApiRequestOptions): Observable<string> {
    return this.api.post<string>(CatalogApi.base, input, options);
  }

  updateCategory(
    id: string,
    input: CategoryInput,
    options?: ApiRequestOptions,
  ): Observable<Record<string, never>> {
    return this.api.put<Record<string, never>>(
      `${CatalogApi.base}/${encodeURIComponent(id)}`,
      input,
      options,
    );
  }

  toggleActive(id: string): Observable<Record<string, never>> {
    return this.api.post<Record<string, never>>(
      `${CatalogApi.base}/${encodeURIComponent(id)}/toggle-active`,
    );
  }

  deleteCategory(id: string): Observable<Record<string, never>> {
    return this.api.delete<Record<string, never>>(`${CatalogApi.base}/${encodeURIComponent(id)}`);
  }
}
