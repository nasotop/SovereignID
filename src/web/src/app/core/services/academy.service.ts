import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { Api } from '../../api/bff/api';
import { academyInvitationsAcceptPost } from '../../api/bff/fn/academy-invitations/academy-invitations-accept-post';
import { academyInstitutionsInstitutionIdGet } from '../../api/bff/fn/academy-institutions/academy-institutions-institution-id-get';
import { academyInstitutionsInstitutionIdInvitationsPost } from '../../api/bff/fn/academy-institutions/academy-institutions-institution-id-invitations-post';
import { academyInstitutionsPost } from '../../api/bff/fn/academy-institutions/academy-institutions-post';
import { AcceptInstitutionInvitationRequest } from '../../api/bff/models/accept-institution-invitation-request';
import { CreateInstitutionInvitationRequest } from '../../api/bff/models/create-institution-invitation-request';
import { CreateInstitutionRequest } from '../../api/bff/models/create-institution-request';
import { InstitutionCreated } from '../../api/bff/models/institution-created';
import { InstitutionInvitationAccepted } from '../../api/bff/models/institution-invitation-accepted';
import { InstitutionInvitationCreated } from '../../api/bff/models/institution-invitation-created';
import { InstitutionSummary } from '../../api/bff/models/institution-summary';
import { toHttpErrorMessage, toThrownError } from '../utils/error.utils';
import { AuthService } from './auth.service';

export class PlatformUnauthenticatedError extends Error {
  constructor(message = 'No hay sesión activa. Inicia sesión para continuar.') {
    super(message);
    this.name = 'PlatformUnauthenticatedError';
  }
}

export class PlatformUnauthorizedError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'PlatformUnauthorizedError';
  }
}

@Injectable({
  providedIn: 'root',
})
export class AcademyService {
  private readonly api = inject(Api);
  private readonly authService = inject(AuthService);

  private requireJwt(): void {
    const jwt = this.authService.getJwt();
    if (!jwt || !this.authService.isAuthenticated()) {
      throw new PlatformUnauthenticatedError();
    }
  }

  async createInstitution(
    body: CreateInstitutionRequest,
  ): Promise<InstitutionCreated> {
    this.requireJwt();

    try {
      return await this.api.invoke(academyInstitutionsPost, { body });
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo crear la institución');
    }
  }

  async getInstitution(institutionId: string): Promise<InstitutionSummary> {
    this.requireJwt();

    try {
      return await this.api.invoke(academyInstitutionsInstitutionIdGet, {
        institutionId,
      });
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo obtener la institución');
    }
  }

  async createInvitation(
    institutionId: string,
    body: CreateInstitutionInvitationRequest,
  ): Promise<InstitutionInvitationCreated> {
    this.requireJwt();

    try {
      return await this.api.invoke(
        academyInstitutionsInstitutionIdInvitationsPost,
        { institutionId, body },
      );
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo crear la invitación');
    }
  }

  async acceptInvitationPublic(
    body: AcceptInstitutionInvitationRequest,
  ): Promise<InstitutionInvitationAccepted> {
    try {
      return await this.api.invoke(academyInvitationsAcceptPost, { body });
    } catch (error: unknown) {
      throw toThrownError(error, 'No se pudo aceptar la invitación');
    }
  }

  private mapApiError(error: unknown, fallback: string): Error {
    if (error instanceof HttpErrorResponse && error.status === 401) {
      return new PlatformUnauthorizedError(toHttpErrorMessage(error, fallback));
    }

    return toThrownError(error, fallback);
  }
}
