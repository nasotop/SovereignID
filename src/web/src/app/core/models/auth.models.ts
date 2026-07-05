/** Institution membership from auth verify response */
export interface InstitutionMembership {
  readonly institutionId: string;
  readonly role: string;
}

/** Client-side session state after successful SIWE login */
export interface AuthState {
  readonly isAuthenticated: boolean;
  readonly jwt: string | null;
  readonly address: string | null;
  readonly expiresAt: string | null;
  readonly userId: string | null;
  readonly platformAdmin: boolean;
  readonly holder: boolean;
  readonly memberships: readonly InstitutionMembership[];
}

export enum StorageType {
  LOCAL = 'localStorage',
  SESSION = 'sessionStorage',
}

export interface RoleGuardOptions {
  readonly platformAdmin?: boolean;
  readonly holder?: boolean;
  readonly institutionRoles?: readonly string[];
  readonly mode?: 'all' | 'any';
}
