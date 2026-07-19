import { HttpErrorResponse } from '@angular/common/http';
import { afterEach, describe, expect, it } from 'vitest';

import { isProblemDetails } from '../models/problem-details.models';
import { toHttpErrorMessage, toThrownError } from '../utils/error.utils';

import unsupportedChainFixture from '../../../../../../docs/contracts/fixtures/auth-verify-400-unsupported-chain.json';

describe('error.utils', () => {
  afterEach(() => {
    localStorage.removeItem('sovereignid.language');
  });

  it('maps Problem Details error code to a controlled user message', () => {
    const error = new HttpErrorResponse({
      error: unsupportedChainFixture,
      status: 400,
      statusText: 'Bad Request',
    });

    expect(toHttpErrorMessage(error, 'fallback')).toBe(
      'Switch your wallet to the supported network and try again.',
    );
  });

  it('maps Problem Details error code to the selected language', () => {
    localStorage.setItem('sovereignid.language', 'es');
    const error = new HttpErrorResponse({
      error: unsupportedChainFixture,
      status: 400,
      statusText: 'Bad Request',
    });

    expect(toHttpErrorMessage(error, 'fallback')).toBe(
      'Cambia tu wallet a la red soportada e intentalo nuevamente.',
    );
  });

  it('maps content-anchor domain error codes to Spanish copy', () => {
    localStorage.setItem('sovereignid.language', 'es');
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

  it('wraps HttpErrorResponse status in a plain Error without exposing detail', () => {
    const error = new HttpErrorResponse({
      error: {
        title: 'Forbidden',
        status: 403,
        detail: 'Sin permisos para reportes',
      },
      status: 403,
      statusText: 'Forbidden',
    });

    expect(toThrownError(error, 'fallback').message).toBe(
      'You do not have permission to perform this action.',
    );
  });
});
