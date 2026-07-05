export type InstitutionRole = 'admin' | 'issuer' | 'viewer';

export interface InstitutionSummary {
  readonly id: string;
  readonly code: string;
  readonly legalName: string;
  readonly displayName: string;
  readonly did: string | null;
  readonly issuerWalletAddress: string | null;
  readonly countryCode: string;
  readonly websiteUrl: string | null;
  readonly isActive: boolean;
  readonly registeredAt: string;
}

export interface StudentSummary {
  readonly id: string;
  readonly institutionId: string;
  readonly externalReference: string | null;
  readonly enrollmentYear: number | null;
  readonly primaryWalletId: string | null;
  readonly primaryWalletAddress: string | null;
  readonly primaryWalletDid: string | null;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface StudentWalletSummary {
  readonly id: string;
  readonly studentId: string;
  readonly walletAddress: string;
  readonly did: string;
  readonly status: string;
  readonly isPrimary: boolean;
  readonly activatedAt: string;
}

export interface InstitutionUserSummary {
  readonly id: string;
  readonly institutionId: string;
  readonly userId: string;
  readonly walletAddress: string;
  readonly did: string;
  readonly email: string | null;
  readonly displayName: string | null;
  readonly role: InstitutionRole;
  readonly grantedAt: string;
  readonly revokedAt: string | null;
}

export interface CreateStudentPayload {
  readonly externalReference: string | null;
  readonly enrollmentYear: number | null;
  readonly walletAddress: string | null;
}

export interface AddStudentWalletPayload {
  readonly walletAddress: string;
  readonly makePrimary: boolean;
}

export interface InviteInstitutionUserPayload {
  readonly email: string;
  readonly role: InstitutionRole;
}

export interface UpdateInstitutionUserRolePayload {
  readonly role: InstitutionRole;
}
