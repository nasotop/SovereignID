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
  labelKey: string;
  displayValue?: string;
  displayValueKey?: string;
  tone: CheckRowTone;
}

export interface CheckGroupViewModel {
  titleKey: string;
  rows: CheckRowViewModel[];
}

export interface EvidenceBannerViewModel {
  visible: boolean;
  messageKey: string;
}

export type VerdictResultTone = 'success' | 'warning' | 'danger' | 'neutral';

export interface VerdictCredentialViewModel {
  raw: NonNullable<VerificationResponse['credential']>;
  /** i18n key when status is a known credential status; otherwise null and use statusFallback. */
  statusKey: string | null;
  statusFallback: string;
  issuedAtDisplay: string;
  expiresAtDisplay: string;
}

export interface VerdictViewModel {
  resultLabelKey: string;
  resultTone: VerdictResultTone;
  groups: CheckGroupViewModel[];
  evidenceBanner: EvidenceBannerViewModel;
  credential: VerdictCredentialViewModel | null;
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

const BOOLEAN_CHECK_LABEL_KEYS: Record<BooleanCheckKey, string> = {
  found: 'verificationVerdict.checks.found',
  notRevoked: 'verificationVerdict.checks.notRevoked',
  notExpired: 'verificationVerdict.checks.notExpired',
  onChainExists: 'verificationVerdict.checks.onChainExists',
  hashMatches: 'verificationVerdict.checks.hashMatches',
  signatureValid: 'verificationVerdict.checks.signatureValid',
};

const VALIDATION_SOURCE_LABEL_KEYS: Record<
  NonNullable<VerificationChecksResponse['validationSource']>,
  string
> = {
  on_chain: 'verificationVerdict.sources.onChain',
  bd_fallback_inconclusive: 'verificationVerdict.sources.dbFallbackInconclusive',
  bd_fallback_rejected: 'verificationVerdict.sources.dbFallbackRejected',
  not_evaluated: 'verificationVerdict.values.notEvaluated',
};

const REVOCATION_SOURCE_LABEL_KEYS: Record<
  NonNullable<VerificationChecksResponse['revocationSource']>,
  string
> = {
  bd: 'verificationVerdict.sources.database',
  on_chain: 'verificationVerdict.sources.onChain',
  both: 'verificationVerdict.sources.databaseAndOnChain',
};

const RESULT_LABEL_KEYS: Record<VerificationResponse['result'], string> = {
  valid: 'verificationVerdict.results.valid',
  revoked: 'verificationVerdict.results.revoked',
  expired: 'verificationVerdict.results.expired',
  not_found: 'verificationVerdict.results.notFound',
  integrity_failed: 'verificationVerdict.results.integrityFailed',
};

const RESULT_TONES: Record<VerificationResponse['result'], VerdictResultTone> = {
  valid: 'success',
  revoked: 'danger',
  expired: 'warning',
  not_found: 'neutral',
  integrity_failed: 'danger',
};

const CREDENTIAL_STATUS_KEYS: Record<string, string> = {
  active: 'common.status.active',
  revoked: 'common.status.revoked',
  expired: 'common.status.expired',
};

function formatDateTime(value: string | null | undefined, locale: string): string {
  if (value === null || value === undefined || value === '') {
    return '—';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(locale, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date);
}

function buildCredentialViewModel(
  credential: NonNullable<VerificationResponse['credential']>,
  locale: string,
): VerdictCredentialViewModel {
  return {
    raw: credential,
    statusKey: CREDENTIAL_STATUS_KEYS[credential.status] ?? null,
    statusFallback: credential.status,
    issuedAtDisplay: formatDateTime(credential.issuedAt, locale),
    expiresAtDisplay: formatDateTime(credential.expiresAt, locale),
  };
}

function formatBooleanCheck(value: boolean | null | undefined): {
  displayValueKey: string;
  tone: CheckRowTone;
} {
  if (value === null || value === undefined) {
    return { displayValueKey: 'verificationVerdict.values.notEvaluated', tone: 'muted' };
  }

  return value
    ? { displayValueKey: 'verificationVerdict.values.yes', tone: 'success' }
    : { displayValueKey: 'verificationVerdict.values.no', tone: 'danger' };
}

function formatValidationSource(
  value: VerificationChecksResponse['validationSource'],
): string | undefined {
  if (value === null || value === undefined) {
    return undefined;
  }

  return VALIDATION_SOURCE_LABEL_KEYS[value];
}

function formatRevocationSource(
  value: VerificationChecksResponse['revocationSource'],
): string | undefined {
  if (value === null || value === undefined) {
    return undefined;
  }

  return REVOCATION_SOURCE_LABEL_KEYS[value];
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
      labelKey: BOOLEAN_CHECK_LABEL_KEYS[key],
      displayValueKey: formatted.displayValueKey,
      tone: formatted.tone,
    };
  });
}

export function buildVerdictViewModel(
  response: VerificationResponse,
  locale: string,
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
    labelKey: 'verificationVerdict.validationSource',
    displayValueKey: formatValidationSource(checks.validationSource),
    displayValue: formatValidationSource(checks.validationSource) ? undefined : '-',
    tone: 'neutral',
  });

  const groups: CheckGroupViewModel[] = [
    { titleKey: 'verificationVerdict.groups.localRegistry', rows: localRows },
    { titleKey: 'verificationVerdict.groups.onChainEvidence', rows: evidenceRows },
  ];

  if (response.result === 'revoked') {
    const displayValueKey = formatRevocationSource(checks.revocationSource);
    groups[0].rows.push({
      key: 'revocationSource',
      labelKey: 'verificationVerdict.revocationSource',
      displayValueKey,
      displayValue: displayValueKey ? undefined : '-',
      tone: 'neutral',
    });
  }

  return {
    resultLabelKey: RESULT_LABEL_KEYS[response.result],
    resultTone: RESULT_TONES[response.result],
    groups,
    evidenceBanner: {
      visible: isEvidenceDisabled(checks),
      messageKey: 'verificationVerdict.evidenceDisabled',
    },
    credential: response.credential ? buildCredentialViewModel(response.credential, locale) : null,
  };
}
