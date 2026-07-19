import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import { isProblemDetails } from '../models/problem-details.models';
import { toHttpErrorMessage, toThrownError } from '../utils/error.utils';

import unsupportedChainFixture from '../../../../../../docs/contracts/fixtures/auth-verify-400-unsupported-chain.json';

describe('error.utils', () => {
  it('maps Problem Details detail to user message', () => {
    const error = new HttpErrorResponse({
      error: unsupportedChainFixture,
      status: 400,
      statusText: 'Bad Request',
    });

    expect(toHttpErrorMessage(error, 'fallback')).toBe(
      unsupportedChainFixture.detail,
    );
  });

  it('maps content-anchor domain error codes to Spanish copy', () => {
    const error = new HttpErrorResponse({
      error: {
        title: 'Service Unavailable',
        status: 503,
        detail: 'IPFS content anchoring is not configured.',
        error: 'ipfs_not_configured',
      },
      status: 503,
      statusText: 'Service Unavailable',
    });

    expect(toHttpErrorMessage(error, 'fallback')).toBe(
      'IPFS no configurado en el servidor emisor',
    );
    expect(toThrownError(error, 'fallback').message).toBe(
      'IPFS no configurado en el servidor emisor',
    );
  });

  it('recognizes Problem Details shape', () => {
    expect(isProblemDetails(unsupportedChainFixture)).toBe(true);
    expect(isProblemDetails({ message: 'legacy' })).toBe(false);
  });

  it('wraps HttpErrorResponse detail in a plain Error', () => {
    const error = new HttpErrorResponse({
      error: {
        title: 'Forbidden',
        status: 403,
        detail: 'Sin permisos para reportes',
      },
      status: 403,
      statusText: 'Forbidden',
    });

    expect(toThrownError(error, 'fallback').message).toBe('Sin permisos para reportes');
  });
});
