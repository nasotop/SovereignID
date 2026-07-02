/** Client-side session state after successful SIWE login */
export interface AuthState {
  readonly isAuthenticated: boolean;
  readonly jwt: string | null;
  readonly address: string | null;
  readonly expiresAt: string | null;
}

export enum StorageType {
  LOCAL = 'localStorage',
  SESSION = 'sessionStorage',
}
