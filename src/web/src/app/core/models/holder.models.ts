export interface HolderProfile {
  walletAddress: string;
  did: string;
  displayName?: string | null;
  fullName?: string | null;
  birthDate?: string | null;
  contactEmail?: string | null;
  countryCode?: string | null;
  phoneNumber?: string | null;
  updatedAt?: string | null;
}

export interface HolderInstitutionSummary {
  institutionId: string;
  institutionCode: string;
  institutionName: string;
  studentId: string;
  externalReference?: string | null;
  enrollmentYear?: number | null;
  walletAddress: string;
  did: string;
  isPrimary: boolean;
  linkedAt: string;
}

export interface HolderDashboard {
  profile: HolderProfile;
  institutions: readonly HolderInstitutionSummary[];
}

export interface UpdateHolderProfilePayload {
  displayName?: string | null;
  fullName?: string | null;
  birthDate?: string | null;
  contactEmail?: string | null;
  countryCode?: string | null;
  phoneNumber?: string | null;
}
