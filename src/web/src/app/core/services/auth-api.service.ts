import { Injectable, inject } from '@angular/core';

import { Api } from '../../api/auth/api';
import { authNonceGet } from '../../api/auth/fn/auth/auth-nonce-get';
import { authVerifyPost } from '../../api/auth/fn/auth/auth-verify-post';
import { NonceResponse } from '../../api/auth/models/nonce-response';
import { VerifyRequest } from '../../api/auth/models/verify-request';
import { VerifyResponse } from '../../api/auth/models/verify-response';
import { toThrownError } from '../utils/error.utils';

/**
 * Fachada HTTP de auth (SIWE). Envuelve el cliente generado desde auth.openapi.json
 * y aplica el seam de Problem Details.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthApiService {
  private readonly api = inject(Api);

  async fetchNonce(): Promise<NonceResponse> {
    try {
      return await this.api.invoke(authNonceGet, {});
    } catch (error: unknown) {
      throw toThrownError(error, 'Failed to fetch nonce from server');
    }
  }

  async verifySignature(request: VerifyRequest): Promise<VerifyResponse> {
    try {
      return await this.api.invoke(authVerifyPost, { body: request });
    } catch (error: unknown) {
      throw toThrownError(error, 'Signature verification failed on server');
    }
  }
}
