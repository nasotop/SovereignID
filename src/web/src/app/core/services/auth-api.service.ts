import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { catchError, firstValueFrom, throwError } from 'rxjs';

import {
  NonceResponse,
  VerifyRequest,
  VerifyResponse,
} from '../models/auth.models';
import { toThrownError } from '../utils/error.utils';

/**
 * Low-level auth HTTP layer for the imperative SIWE flow (nonce → sign → verify).
 *
 * One-shot requests use firstValueFrom — no rxResource here because the nonce
 * is fetched once per login attempt and does not need continuous reactive caching.
 * Reactive reads (e.g. credential lists) belong in dedicated services like CredentialService.
 *
 * POST /auth/verify stays imperative because `resource` is read-oriented and
 * auto-cancels in-flight requests — unsafe for signature verification mutations.
 */
@Service()
export class AuthApiService {
  private readonly http = inject(HttpClient);

  /** Fetches a fresh nonce for SIWE login (one-shot GET) */
  async fetchNonce(): Promise<NonceResponse> {
    return firstValueFrom(
      this.http.get<NonceResponse>('/auth/nonce').pipe(
        catchError((error: unknown) =>
          throwError(() =>
            toThrownError(error, 'Failed to fetch nonce from server'),
          ),
        ),
      ),
    );
  }

  /** Verifies the signed SIWE message and returns a JWT (one-shot POST) */
  async verifySignature(request: VerifyRequest): Promise<VerifyResponse> {
    return firstValueFrom(
      this.http.post<VerifyResponse>('/auth/verify', request).pipe(
        catchError((error: unknown) =>
          throwError(() =>
            toThrownError(error, 'Signature verification failed on server'),
          ),
        ),
      ),
    );
  }
}
