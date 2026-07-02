export type CredentialStatus = 'active' | 'revoked' | 'expired';

export type HolderCredentialIcon = 'degree' | 'certificate';

export interface HolderCredential {
  readonly id: string;
  readonly title: string;
  readonly issuer: string;
  readonly issuedDate: string;
  readonly status: 'active';
  readonly icon: HolderCredentialIcon;
}

export interface IssuedCredential {
  readonly credentialId: string;
  readonly institutionId: string;
  readonly studentId: string;
  readonly studentLabel: string;
  readonly documentType: string;
  readonly issuedDate: string;
  readonly status: CredentialStatus;
  readonly ipfsGatewayUrl?: string;
}

export interface IssueCredentialModel {
  institutionId: string;
  studentId: string;
  careerId: string;
  studentLabel: string;
  documentType: string;
  issuedDate: string;
  subjectWallet: string;
  subjectDid: string;
  issuerDid: string;
}

export const DOCUMENT_TYPE_OPTIONS = [
  { label: 'University Degree', code: 'TITULO' },
  { label: 'Grade Certificate', code: 'NOTAS' },
  { label: 'Enrollment Certificate', code: 'CERTIFICACION' },
] as const;

export type DocumentTypeOption = (typeof DOCUMENT_TYPE_OPTIONS)[number];

export interface LinkStudentTitleRequest {
  credentialId?: string;
  careerId?: string;
  credentialTypeCode: string;
  ipfsCid: string;
  ipfsGatewayUrl: string;
  contentHash: string;
  transactionHash: string;
  blockNumber: number;
  chainId?: number;
  eip712Signature: string;
  expiresAt?: string | null;
  metadata?: Record<string, unknown>;
}

export interface CredentialSummaryResponse {
  credentialId: string;
  institutionId: string;
  studentId: string;
  careerId?: string;
  credentialTypeCode: string;
  subjectDid: string;
  issuerDid: string;
  status: string;
  ipfsCid: string;
  ipfsGatewayUrl: string;
  contentHash: string;
  transactionHash: string;
  issuedAt: string;
  revokedAt?: string;
  revocationReason?: string;
  studentLabel?: string;
}

export interface CredentialRevokedResponse {
  credentialId: string;
  institutionId: string;
  studentId: string;
  status: string;
  revokedAt: string;
  revocationReason?: string;
  revocationTxHash: string;
}

export interface VerifiableCredentialDocument {
  '@context': string[];
  type: string[];
  issuer: string;
  issuanceDate: string;
  credentialSubject: {
    id: string;
    degree?: string;
    studentLabel?: string;
    careerId?: string;
  };
  credentialSchema?: {
    id: string;
    type: string;
  };
}

export const MOCK_HOLDER_CREDENTIALS = [
  {
    id: '1',
    title: 'Título Universitario',
    issuer: 'Duoc UC',
    issuedDate: 'Issued on November 15, 2025',
    status: 'active' as const,
    icon: 'degree' as const,
  },
  {
    id: '2',
    title: 'Certificado de Notas',
    issuer: 'Duoc UC',
    issuedDate: 'Issued on October 22, 2025',
    status: 'active' as const,
    icon: 'certificate' as const,
  },
] as const;
