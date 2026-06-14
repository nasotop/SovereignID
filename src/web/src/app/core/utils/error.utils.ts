import { HttpErrorResponse } from '@angular/common/http';

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

    if (
      typeof body === 'object' &&
      body !== null &&
      'message' in body &&
      typeof body.message === 'string'
    ) {
      return body.message;
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
