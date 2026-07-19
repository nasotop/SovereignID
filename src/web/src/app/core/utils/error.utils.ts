import { HttpErrorResponse } from '@angular/common/http';

import { isProblemDetails } from '../models/problem-details.models';

/** Domain error codes → Spanish UI copy (issuer content anchor, etc.) */
const DOMAIN_ERROR_MESSAGES: Readonly<Record<string, string>> = {
  ipfs_not_configured: 'IPFS no configurado en el servidor emisor',
  content_anchor_failed: 'No se pudo anclar el documento en IPFS',
  invalid_anchor_document: 'Documento de credencial inválido',
};

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
      const mapped = body.error ? DOMAIN_ERROR_MESSAGES[body.error] : undefined;
      return mapped ?? body.detail;
    }

    return error.message || fallback;
  }

  return toErrorMessage(error) || fallback;
}

/** Wraps unknown errors into Error instances for consistent propagation */
export function toThrownError(error: unknown, fallback: string): Error {
  if (error instanceof HttpErrorResponse) {
    return new Error(toHttpErrorMessage(error, fallback));
  }

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
