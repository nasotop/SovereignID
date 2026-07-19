import { describe, expect, it } from 'vitest';

import { VerificationChecksResponse } from '../../../api/bff/models/verification-checks-response';
import { VerificationResponse } from '../../../api/bff/models/verification-response';
import { buildVerdictViewModel } from './verification-verdict-model.builder';

function makeResponse(overrides: {
  result?: VerificationResponse['result'];
  credential?: VerificationResponse['credential'];
  checks?: Partial<VerificationChecksResponse>;
} = {}): VerificationResponse {
  const baseChecks: VerificationChecksResponse = {
    found: true,
    notRevoked: true,
    notExpired: true,
    hashMatches: true,
    onChainExists: true,
    signatureValid: true,
    validationSource: 'on_chain',
    revocationSource: null,
  };

  return {
    result: overrides.result ?? 'valid',
    credential: overrides.credential ?? null,
    checks: { ...baseChecks, ...overrides.checks },
  };
}

const LOCALE = 'en-US';

describe('buildVerdictViewModel', () => {
  it('shows revocation source only when result is revoked', () => {
    const viewModel = buildVerdictViewModel(
      makeResponse({
        result: 'revoked',
        checks: {
          notRevoked: false,
          revocationSource: 'on_chain',
        },
      }),
      LOCALE,
    );

    const localRows = viewModel.groups.find((group) => group.titleKey === 'verificationVerdict.groups.localRegistry')?.rows;
    expect(localRows?.some((row) => row.key === 'revocationSource')).toBe(true);
    expect(
      localRows?.find((row) => row.key === 'revocationSource')?.displayValueKey,
    ).toBe('verificationVerdict.sources.onChain');
  });

  it('hides revocation source for non-revoked results', () => {
    const viewModel = buildVerdictViewModel(
      makeResponse({
        result: 'valid',
        checks: { revocationSource: 'bd' },
      }),
      LOCALE,
    );

    const allRows = viewModel.groups.flatMap((group) => group.rows);
    expect(allRows.some((row) => row.key === 'revocationSource')).toBe(false);
  });

  it('shows evidence disabled banner when on-chain checks are not evaluated', () => {
    const viewModel = buildVerdictViewModel(
      makeResponse({
        checks: {
          onChainExists: null,
          hashMatches: null,
          signatureValid: null,
          validationSource: 'not_evaluated',
        },
      }),
      LOCALE,
    );

    expect(viewModel.evidenceBanner.visible).toBe(true);
    expect(viewModel.groups.find((group) => group.titleKey === 'verificationVerdict.groups.onChainEvidence')?.rows).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ displayValueKey: 'verificationVerdict.values.notEvaluated' }),
      ]),
    );
  });

  it('formats null boolean checks as No evaluado', () => {
    const viewModel = buildVerdictViewModel(
      makeResponse({
        checks: {
          hashMatches: null,
          validationSource: 'not_evaluated',
          onChainExists: null,
          signatureValid: null,
        },
      }),
      LOCALE,
    );

    const evidenceRows =
      viewModel.groups.find((group) => group.titleKey === 'verificationVerdict.groups.onChainEvidence')?.rows ?? [];
    const hashRow = evidenceRows.find((row) => row.key === 'hashMatches');

    expect(hashRow?.displayValueKey).toBe('verificationVerdict.values.notEvaluated');
    expect(hashRow?.tone).toBe('muted');
  });

  it('uses danger tone for integrity_failed result', () => {
    const viewModel = buildVerdictViewModel(
      makeResponse({
        result: 'integrity_failed',
        checks: {
          hashMatches: false,
          onChainExists: true,
          signatureValid: false,
          validationSource: 'on_chain',
        },
      }),
      LOCALE,
    );

    expect(viewModel.resultLabelKey).toBe('verificationVerdict.results.integrityFailed');
    expect(viewModel.resultTone).toBe('danger');
  });

  it('maps known credential status to i18n key and formats dates with locale', () => {
    const viewModel = buildVerdictViewModel(
      makeResponse({
        credential: {
          id: '11111111-1111-1111-1111-111111111111',
          type: 'TITULO',
          status: 'active',
          issuer: { code: 'DUOC', displayName: 'Duoc UC', did: 'did:example:issuer' },
          subjectDid: 'did:example:subject',
          issuedAt: '2024-06-15T12:00:00.000Z',
          expiresAt: null,
          anchors: {
            ipfsCid: 'bafy',
            contentHash: '0xabc',
            transactionHash: '0xdef',
            chainId: 11155111,
          },
        },
      }),
      'en-US',
    );

    expect(viewModel.credential?.statusKey).toBe('common.status.active');
    expect(viewModel.credential?.issuedAtDisplay).not.toBe('2024-06-15T12:00:00.000Z');
    expect(viewModel.credential?.expiresAtDisplay).toBe('—');
  });

  it('falls back to raw status when unknown', () => {
    const viewModel = buildVerdictViewModel(
      makeResponse({
        credential: {
          id: '11111111-1111-1111-1111-111111111111',
          type: 'TITULO',
          status: 'pending_review',
          issuer: { code: 'DUOC', displayName: 'Duoc UC', did: 'did:example:issuer' },
          subjectDid: 'did:example:subject',
          issuedAt: '2024-06-15T12:00:00.000Z',
          expiresAt: '2026-06-15T12:00:00.000Z',
          anchors: {
            ipfsCid: 'bafy',
            contentHash: '0xabc',
            transactionHash: '0xdef',
            chainId: 11155111,
          },
        },
      }),
      'es-CL',
    );

    expect(viewModel.credential?.statusKey).toBeNull();
    expect(viewModel.credential?.statusFallback).toBe('pending_review');
    expect(viewModel.credential?.expiresAtDisplay).not.toBe('2026-06-15T12:00:00.000Z');
  });
});
