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
    );

    expect(viewModel.resultLabelKey).toBe('verificationVerdict.results.integrityFailed');
    expect(viewModel.resultTone).toBe('danger');
  });
});
