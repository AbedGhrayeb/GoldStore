import { HttpErrorResponse } from '@angular/common/http';

/** Normalized error emitted by the error interceptor, mapped from RFC 9457 ProblemDetails. */
export interface ApiError {
  status: number;
  title: string | null;
  detail: string | null;
  validation: Record<string, string[]> | null;
}

export function isApiError(error: unknown): error is ApiError {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    'title' in error &&
    'detail' in error &&
    'validation' in error
  );
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as {
      title?: string | null;
      detail?: string | null;
      errors?: Record<string, string[]> | null;
    } | null;
    return {
      status: error.status,
      title: body?.title ?? null,
      detail: body?.detail ?? null,
      validation: body?.errors ?? null,
    };
  }
  return { status: 0, title: null, detail: 'Network error', validation: null };
}
