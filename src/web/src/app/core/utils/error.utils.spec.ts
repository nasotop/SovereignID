import { describe, expect, it } from 'vitest';

import { isProblemDetails } from './problem-details.models';
import { toHttpErrorMessage } from '../utils/error.utils';
import { HttpErrorResponse } from '@angular/common/http';

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

  it('recognizes Problem Details shape', () => {
    expect(isProblemDetails(unsupportedChainFixture)).toBe(true);
    expect(isProblemDetails({ message: 'legacy' })).toBe(false);
  });
});
