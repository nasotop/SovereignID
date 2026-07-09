import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

import { Api } from '../../api/bff/api';
import { verificationsPost } from '../../api/bff/fn/verifications/verifications-post';
import { VerificationResponse } from '../../api/bff/models/verification-response';
import {
  toErrorCode,
  toHttpErrorMessage,
  toThrownError,
} from '../utils/error.utils';

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export class InvalidCredentialIdFormatError extends Error {
  constructor() {
    super('El credentialId no es un UUID válido.');
    this.name = 'InvalidCredentialIdFormatError';
  }
}

export class RateLimitExceededError extends Error {
  constructor(message = 'Demasiadas solicitudes. Espera un momento e inténtalo de nuevo.') {
    super(message);
    this.name = 'RateLimitExceededError';
  }
}

@Injectable({
  providedIn: 'root',
})
export class VerifierService {
  private readonly api = inject(Api);

  isValidCredentialId(credentialId: string): boolean {
    const trimmed = credentialId.trim();
    return trimmed.length > 0 && UUID_PATTERN.test(trimmed);
  }

  async verifyCredential(credentialId: string): Promise<VerificationResponse> {
    const trimmed = credentialId.trim();
    if (!this.isValidCredentialId(trimmed)) {
      throw new InvalidCredentialIdFormatError();
    }

    try {
      return await this.api.invoke(verificationsPost, {
        body: { credentialId: trimmed },
      });
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse && error.status === 429) {
        if (toErrorCode(error) === 'rate_limit_exceeded') {
          throw new RateLimitExceededError(
            toHttpErrorMessage(error, 'Demasiadas solicitudes. Espera un momento e inténtalo de nuevo.'),
          );
        }
      }

      throw toThrownError(error, 'Verification failed');
    }
  }
}
