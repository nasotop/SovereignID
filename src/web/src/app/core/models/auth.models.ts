/** Response from GET /auth/nonce */
export interface NonceResponse {
  readonly nonce: string;
}

/** Request body for POST /auth/verify */
export interface VerifyRequest {
  readonly message: string;
  readonly signature: string;
}

/** Response from POST /auth/verify */
export interface VerifyResponse {
  readonly token: string;
}

/** User authentication state */
export interface AuthState {
  readonly isAuthenticated: boolean;
  readonly token: string | null;
  readonly address: string | null;
}

export enum StorageType {
  LOCAL = 'localStorage',
  SESSION = 'sessionStorage',
}
