import { HttpErrorResponse } from '@angular/common/http';

import { isProblemDetails } from '../models/problem-details.models';

/** Narrows unknown catch/observable errors to a human-readable message */
export function toErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === 'string') {
    return error;
  }

  return 'Unknown error occurred';
}

/** Maps HTTP and generic errors to a stable user-facing message */
export function toHttpErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error;

    if (isProblemDetails(body)) {
      return body.detail;
    }

    return error.message || fallback;
  }

  return toErrorMessage(error) || fallback;
}

/** Wraps unknown errors into Error instances for consistent propagation */
export function toThrownError(error: unknown, fallback: string): Error {
  if (error instanceof Error) {
    return error;
  }

  return new Error(toHttpErrorMessage(error, fallback));
}

/** Stable machine-readable error code from Problem Details, if present */
export function toErrorCode(error: unknown): string | null {
  if (error instanceof HttpErrorResponse && isProblemDetails(error.error)) {
    return error.error.error ?? null;
  }

  return null;
}
