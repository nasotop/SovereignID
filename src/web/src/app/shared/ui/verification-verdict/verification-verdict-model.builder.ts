import { VerificationChecksResponse } from '../../../api/bff/models/verification-checks-response';
import { VerificationResponse } from '../../../api/bff/models/verification-response';

export type VerdictPresentationPreset = 'verifierFull';

export interface VerdictPresentation {
  preset: VerdictPresentationPreset;
}

export const VERIFIER_FULL_PRESENTATION: VerdictPresentation = {
  preset: 'verifierFull',
};

export type CheckRowTone = 'success' | 'danger' | 'neutral' | 'muted';

export interface CheckRowViewModel {
  key: string;
  label: string;
  displayValue: string;
  tone: CheckRowTone;
}

export interface CheckGroupViewModel {
  title: string;
  rows: CheckRowViewModel[];
}

export interface EvidenceBannerViewModel {
  visible: boolean;
  message: string;
}

export type VerdictResultTone = 'success' | 'warning' | 'danger' | 'neutral';

export interface VerdictViewModel {
  resultLabel: string;
  resultTone: VerdictResultTone;
  groups: CheckGroupViewModel[];
  evidenceBanner: EvidenceBannerViewModel;
  credential: VerificationResponse['credential'];
}

type BooleanCheckKey =
  | 'found'
  | 'notRevoked'
  | 'notExpired'
  | 'onChainExists'
  | 'hashMatches'
  | 'signatureValid';

const LOCAL_CHECK_KEYS: BooleanCheckKey[] = ['found', 'notRevoked', 'notExpired'];
const EVIDENCE_CHECK_KEYS: BooleanCheckKey[] = [
  'onChainExists',
  'hashMatches',
  'signatureValid',
];

const BOOLEAN_CHECK_LABELS: Record<BooleanCheckKey, string> = {
  found: 'Encontrada en registro',
  notRevoked: 'No revocada',
  notExpired: 'No expirada',
  onChainExists: 'Existe on-chain',
  hashMatches: 'Hash coincide',
  signatureValid: 'Firma válida',
};

const VALIDATION_SOURCE_LABELS: Record<
  NonNullable<VerificationChecksResponse['validationSource']>,
  string
> = {
  on_chain: 'On-chain',
  bd_fallback_inconclusive: 'BD (inconcluso)',
  bd_fallback_rejected: 'BD (rechazado)',
  not_evaluated: 'No evaluado',
};

const REVOCATION_SOURCE_LABELS: Record<
  NonNullable<VerificationChecksResponse['revocationSource']>,
  string
> = {
  bd: 'Base de datos',
  on_chain: 'On-chain',
  both: 'BD y on-chain',
};

const RESULT_LABELS: Record<VerificationResponse['result'], string> = {
  valid: 'Credencial válida',
  revoked: 'Credencial revocada',
  expired: 'Credencial expirada',
  not_found: 'Credencial inexistente',
  integrity_failed: 'Integridad comprometida',
};

const RESULT_TONES: Record<VerificationResponse['result'], VerdictResultTone> = {
  valid: 'success',
  revoked: 'danger',
  expired: 'warning',
  not_found: 'neutral',
  integrity_failed: 'danger',
};

const EVIDENCE_DISABLED_BANNER_MESSAGE =
  'La verificación on-chain/IPFS no está habilitada en este entorno.';

function formatBooleanCheck(value: boolean | null | undefined): {
  displayValue: string;
  tone: CheckRowTone;
} {
  if (value === null || value === undefined) {
    return { displayValue: 'No evaluado', tone: 'muted' };
  }

  return value
    ? { displayValue: 'Sí', tone: 'success' }
    : { displayValue: 'No', tone: 'danger' };
}

function formatValidationSource(
  value: VerificationChecksResponse['validationSource'],
): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return VALIDATION_SOURCE_LABELS[value];
}

function formatRevocationSource(
  value: VerificationChecksResponse['revocationSource'],
): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return REVOCATION_SOURCE_LABELS[value];
}

function isEvidenceDisabled(checks: VerificationChecksResponse): boolean {
  return (
    checks.onChainExists === null &&
    checks.hashMatches === null &&
    checks.signatureValid === null &&
    checks.validationSource === 'not_evaluated'
  );
}

function buildBooleanRows(
  checks: VerificationChecksResponse,
  keys: BooleanCheckKey[],
): CheckRowViewModel[] {
  return keys.map((key) => {
    const formatted = formatBooleanCheck(checks[key]);
    return {
      key,
      label: BOOLEAN_CHECK_LABELS[key],
      displayValue: formatted.displayValue,
      tone: formatted.tone,
    };
  });
}

export function buildVerdictViewModel(
  response: VerificationResponse,
  presentation: VerdictPresentation = VERIFIER_FULL_PRESENTATION,
): VerdictViewModel {
  if (presentation.preset !== 'verifierFull') {
    throw new Error(`Unsupported verdict presentation preset: ${presentation.preset}`);
  }

  const { checks } = response;
  const localRows = buildBooleanRows(checks, LOCAL_CHECK_KEYS);
  const evidenceRows = buildBooleanRows(checks, EVIDENCE_CHECK_KEYS);

  evidenceRows.push({
    key: 'validationSource',
    label: 'Fuente de validación de firma',
    displayValue: formatValidationSource(checks.validationSource),
    tone: 'neutral',
  });

  const groups: CheckGroupViewModel[] = [
    { title: 'Registro local', rows: localRows },
    { title: 'Evidencia on-chain', rows: evidenceRows },
  ];

  if (response.result === 'revoked') {
    groups[0].rows.push({
      key: 'revocationSource',
      label: 'Fuente de revocación',
      displayValue: formatRevocationSource(checks.revocationSource),
      tone: 'neutral',
    });
  }

  return {
    resultLabel: RESULT_LABELS[response.result],
    resultTone: RESULT_TONES[response.result],
    groups,
    evidenceBanner: {
      visible: isEvidenceDisabled(checks),
      message: EVIDENCE_DISABLED_BANNER_MESSAGE,
    },
    credential: response.credential,
  };
}
