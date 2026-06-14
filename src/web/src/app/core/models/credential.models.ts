export type CredentialStatus = 'active' | 'revoked';

export type HolderCredentialIcon = 'degree' | 'certificate';

export interface IssuedCredential {
  readonly id: string;
  readonly student: string;
  readonly documentType: string;
  readonly issuedDate: string;
  readonly status: CredentialStatus;
}

export interface HolderCredential {
  readonly id: string;
  readonly title: string;
  readonly issuer: string;
  readonly issuedDate: string;
  readonly status: 'active';
  readonly icon: HolderCredentialIcon;
}

/** Signal Form model for the Issue Credential maintainer modal */
export interface IssueCredentialModel {
  student: string;
  documentType: string;
  issuedDate: string;
}

export const DOCUMENT_TYPE_OPTIONS = [
  'University Degree',
  'Grade Certificate',
  'Enrollment Certificate',
] as const satisfies ReadonlyArray<string>;

export type DocumentTypeOption = (typeof DOCUMENT_TYPE_OPTIONS)[number];

export const MOCK_ISSUED_CREDENTIALS = [
  {
    id: '1',
    student: 'María González',
    documentType: 'University Degree',
    issuedDate: '2025-11-15',
    status: 'active',
  },
  {
    id: '2',
    student: 'Carlos Ruiz',
    documentType: 'Grade Certificate',
    issuedDate: '2025-10-22',
    status: 'active',
  },
  {
    id: '3',
    student: 'Ana Martínez',
    documentType: 'Enrollment Certificate',
    issuedDate: '2025-09-08',
    status: 'revoked',
  },
] as const satisfies ReadonlyArray<IssuedCredential>;

export const MOCK_HOLDER_CREDENTIALS = [
  {
    id: '1',
    title: 'Título Universitario',
    issuer: 'Duoc UC',
    issuedDate: 'Issued on November 15, 2025',
    status: 'active',
    icon: 'degree',
  },
  {
    id: '2',
    title: 'Certificado de Notas',
    issuer: 'Duoc UC',
    issuedDate: 'Issued on October 22, 2025',
    status: 'active',
    icon: 'certificate',
  },
] as const satisfies ReadonlyArray<HolderCredential>;
