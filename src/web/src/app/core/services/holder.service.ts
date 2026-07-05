import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { Api } from '../../api/bff/api';
import { issuerHoldersMeCredentialsCredentialIdGet } from '../../api/bff/fn/holder-credentials/issuer-holders-me-credentials-credential-id-get';
import { issuerHoldersMeCredentialsGet } from '../../api/bff/fn/holder-credentials/issuer-holders-me-credentials-get';
import { HolderCredentialDetail } from '../../api/bff/models/holder-credential-detail';
import { HolderCredentialSummary } from '../../api/bff/models/holder-credential-summary';
import { BFF_API_BASE } from '../constants/api.constants';
import {
  HolderDashboard,
  HolderProfile,
  UpdateHolderProfilePayload,
} from '../models/holder.models';
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
  private readonly http = inject(HttpClient);
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

  async getMyDashboard(): Promise<HolderDashboard> {
    this.requireJwt();

    try {
      return await firstValueFrom(
        this.http.get<HolderDashboard>(`${BFF_API_BASE}/academy/holders/me`),
      );
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo cargar el perfil del holder');
    }
  }

  async updateMyProfile(
    body: UpdateHolderProfilePayload,
  ): Promise<HolderProfile> {
    this.requireJwt();

    try {
      return await firstValueFrom(
        this.http.put<HolderProfile>(
          `${BFF_API_BASE}/academy/holders/me/profile`,
          body,
        ),
      );
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo actualizar el perfil');
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
