import type { CategoryResponse } from './catalog-api.service';

/** Display row for the categories tree table (depth-first order, pre-sorted by Arabic name). */
export interface CategoryTreeRow {
  id: string;
  name: string;
  description: string | null;
  parentCategoryId: string | null;
  parentCategoryName: string | null;
  isActive: boolean;
  depth: number;
  hasChildren: boolean;
}

function byArabicName(a: CategoryResponse, b: CategoryResponse): number {
  return (a.name ?? '').localeCompare(b.name ?? '', 'ar');
}

/**
 * Flattens the parent-self-referencing category list into depth-first display rows.
 * Roots come first (sorted by Arabic name), each followed by its descendants.
 */
export function buildCategoryRows(categories: readonly CategoryResponse[]): CategoryTreeRow[] {
  const byParent = new Map<string | null, CategoryResponse[]>();
  for (const category of categories) {
    const parentId = category.parentCategoryId ?? null;
    const bucket = byParent.get(parentId);
    if (bucket !== undefined) {
      bucket.push(category);
    } else {
      byParent.set(parentId, [category]);
    }
  }

  const rows: CategoryTreeRow[] = [];
  const visit = (parentId: string | null, depth: number): void => {
    for (const category of (byParent.get(parentId) ?? []).sort(byArabicName)) {
      const id = category.id ?? '';
      rows.push({
        id,
        name: category.name ?? '',
        description: category.description ?? null,
        parentCategoryId: category.parentCategoryId ?? null,
        parentCategoryName: category.parentCategoryName ?? null,
        isActive: category.isActive ?? true,
        depth,
        hasChildren: (byParent.get(id) ?? []).length > 0,
      });
      visit(id, depth + 1);
    }
  };
  visit(null, 0);
  return rows;
}

/** Parent option for the create/edit form (depth-indented Arabic label). */
export interface CategoryParentOption {
  id: string;
  name: string;
  depth: number;
}

/**
 * Collects valid parent choices for the category form. When editing, the category itself and
 * all of its descendants are excluded (the server rejects circular references).
 */
export function buildParentOptions(
  categories: readonly CategoryResponse[],
  excludedId: string | null,
): CategoryParentOption[] {
  const excluded = new Set<string>();
  const collectDescendants = (parentId: string): void => {
    for (const category of categories) {
      if ((category.parentCategoryId ?? null) === parentId) {
        const id = category.id ?? '';
        if (excluded.add(id)) {
          collectDescendants(id);
        }
      }
    }
  };
  if (excludedId !== null) {
    excluded.add(excludedId);
    collectDescendants(excludedId);
  }

  const options: CategoryParentOption[] = [];
  const visit = (parentId: string | null, depth: number): void => {
    for (const category of categories
      .filter((candidate) => (candidate.parentCategoryId ?? null) === parentId)
      .filter((candidate) => !excluded.has(candidate.id ?? ''))
      .sort(byArabicName)) {
      const id = category.id ?? '';
      options.push({ id, name: category.name ?? '', depth });
      visit(id, depth + 1);
    }
  };
  visit(null, 0);
  return options;
}
