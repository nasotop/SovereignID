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

    const localRows = viewModel?.groups.find((group) => group.title === 'Registro local')?.rows;
    expect(localRows?.some((row) => row.key === 'revocationSource')).toBe(true);
    expect(
      localRows?.find((row) => row.key === 'revocationSource')?.displayValue,
    ).toBe('On-chain');
  });

  it('hides revocation source for non-revoked results', () => {
    const viewModel = buildVerdictViewModel(
      makeResponse({
        result: 'valid',
        checks: { revocationSource: 'bd' },
      }),
    );

    const allRows = viewModel?.groups.flatMap((group) => group.rows) ?? [];
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

    expect(viewModel?.evidenceBanner.visible).toBe(true);
    expect(viewModel?.groups.find((group) => group.title === 'Evidencia on-chain')?.rows).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ displayValue: 'No evaluado' }),
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
      viewModel?.groups.find((group) => group.title === 'Evidencia on-chain')?.rows ?? [];
    const hashRow = evidenceRows.find((row) => row.key === 'hashMatches');

    expect(hashRow?.displayValue).toBe('No evaluado');
    expect(hashRow?.tone).toBe('muted');
  });
});
