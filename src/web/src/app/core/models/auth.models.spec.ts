import { describe, expect, it } from 'vitest';

import type { NonceResponse } from '../../api/auth/models/nonce-response';
import type { VerifyResponse } from '../../api/auth/models/verify-response';
import { isProblemDetails } from './problem-details.models';

import nonceFixture from '../../../../../../docs/contracts/fixtures/auth-nonce-200.json';
import verifyFixture from '../../../../../../docs/contracts/fixtures/auth-verify-200.json';
import unsupportedChainFixture from '../../../../../../docs/contracts/fixtures/auth-verify-400-unsupported-chain.json';

function assertNonceResponse(value: unknown): asserts value is NonceResponse {
  const response = value as NonceResponse;
  expect(response.nonce).toMatch(/^[0-9a-f]{32}$/);
  expect(response.expiresAt).toBeTruthy();
}

function assertVerifyResponse(value: unknown): asserts value is VerifyResponse {
  const response = value as VerifyResponse;
  expect(response.jwt).toBeTruthy();
  expect(response.address).toMatch(/^0x[0-9a-fA-F]{40}$/);
  expect(response.expiresAt).toBeTruthy();
}

describe('auth contract fixtures', () => {
  it('nonce fixture matches NonceResponse', () => {
    assertNonceResponse(nonceFixture);
  });

  it('verify fixture matches VerifyResponse', () => {
    assertVerifyResponse(verifyFixture);
  });

  it('error fixture matches Problem Details', () => {
    expect(isProblemDetails(unsupportedChainFixture)).toBe(true);
    if (isProblemDetails(unsupportedChainFixture)) {
      expect(unsupportedChainFixture.error).toBe('unsupported_chain');
      expect(unsupportedChainFixture.detail).toBeTruthy();
    }
  });
});
