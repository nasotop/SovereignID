/** Response from GET /auth/nonce — aligned with OpenAPI / AuthContracts */
export interface NonceResponse {
  readonly nonce: string;
  readonly expiresAt: string;
}

/** Request body for POST /auth/verify */
export interface VerifyRequest {
  readonly message: string;
  readonly signature: string;
}

/** Response from POST /auth/verify */
export interface VerifyResponse {
  readonly jwt: string;
  readonly address: string;
  readonly expiresAt: string;
}

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
