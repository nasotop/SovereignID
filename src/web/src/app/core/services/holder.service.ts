import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { Api } from '../../api/bff/api';
import { issuerHoldersMeCredentialsCredentialIdGet } from '../../api/bff/fn/holder-credentials/issuer-holders-me-credentials-credential-id-get';
import { issuerHoldersMeCredentialsGet } from '../../api/bff/fn/holder-credentials/issuer-holders-me-credentials-get';
import { HolderCredentialDetail } from '../../api/bff/models/holder-credential-detail';
import { HolderCredentialSummary } from '../../api/bff/models/holder-credential-summary';
import { toHttpErrorMessage, toThrownError } from '../utils/error.utils';
import { AuthService } from './auth.service';

export class HolderUnauthenticatedError extends Error {
  constructor(message = 'No hay sesión activa. Inicia sesión para continuar.') {
    super(message);
    this.name = 'HolderUnauthenticatedError';
  }
}

export class HolderUnauthorizedError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'HolderUnauthorizedError';
  }
}

@Injectable({
  providedIn: 'root',
})
export class HolderService {
  private readonly api = inject(Api);
  private readonly authService = inject(AuthService);

  private requireJwt(): string {
    const jwt = this.authService.getJwt();
    if (!jwt || !this.authService.isAuthenticated()) {
      throw new HolderUnauthenticatedError();
    }

    return jwt;
  }

  async listMyCredentials(): Promise<HolderCredentialSummary[]> {
    this.requireJwt();

    try {
      return await this.api.invoke(issuerHoldersMeCredentialsGet, {});
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudieron cargar las credenciales');
    }
  }

  async getMyCredential(credentialId: string): Promise<HolderCredentialDetail> {
    this.requireJwt();

    try {
      return await this.api.invoke(issuerHoldersMeCredentialsCredentialIdGet, {
        credentialId,
      });
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo obtener el detalle de la credencial');
    }
  }

  downloadCredentialJson(detail: HolderCredentialDetail): void {
    const blob = new Blob([JSON.stringify(detail, null, 2)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `credential-${detail.id}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  async shareCredentialId(id: string): Promise<void> {
    if (!navigator.clipboard?.writeText) {
      throw new Error('El portapapeles no está disponible en este navegador.');
    }

    await navigator.clipboard.writeText(id);
  }

  isDegreeType(typeCode: string): boolean {
    return typeCode === 'TITULO';
  }

  private mapApiError(error: unknown, fallback: string): Error {
    if (error instanceof HttpErrorResponse && error.status === 401) {
      return new HolderUnauthorizedError(toHttpErrorMessage(error, fallback));
    }

    return toThrownError(error, fallback);
  }
}
