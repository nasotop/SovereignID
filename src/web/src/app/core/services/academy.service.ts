import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

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
import { BFF_API_BASE } from '../constants/api.constants';
import {
  AddStudentWalletPayload,
  CareerSummary,
  CreateCareerPayload,
  CreateStudentPayload,
  InstitutionSummary as AcademyInstitutionSummary,
  InstitutionUserSummary,
  InviteInstitutionUserPayload,
  LinkInstitutionIssuerWalletPayload,
  StudentSummary,
  StudentWalletSummary,
  UpdateCareerPayload,
  UpdateInstitutionPayload,
  UpdateInstitutionUserRolePayload,
} from '../models/academy.models';
import { toHttpErrorMessage, toThrownError } from '../utils/error.utils';
import { AuthService } from './auth.service';

export class PlatformUnauthenticatedError extends Error {
  constructor(message = 'No hay sesion activa. Inicia sesion para continuar.') {
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
  private readonly http = inject(HttpClient);
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
      throw this.mapApiError(error, 'No se pudo crear la institucion');
    }
  }

  async getInstitution(institutionId: string): Promise<InstitutionSummary> {
    this.requireJwt();

    try {
      return await this.api.invoke(academyInstitutionsInstitutionIdGet, {
        institutionId,
      });
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo obtener la institucion');
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
      throw this.mapApiError(error, 'No se pudo crear la invitacion');
    }
  }

  async updateInstitution(
    institutionId: string,
    body: UpdateInstitutionPayload,
  ): Promise<AcademyInstitutionSummary> {
    this.requireJwt();

    return this.patchJson<AcademyInstitutionSummary>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}`,
      body,
      'No se pudo actualizar la institucion',
    );
  }

  async linkInstitutionIssuerWallet(
    institutionId: string,
    body: LinkInstitutionIssuerWalletPayload,
  ): Promise<void> {
    this.requireJwt();

    await this.postJson<unknown>(
      `${BFF_API_BASE}/issuer/institutions/${institutionId}/wallet`,
      body,
      'No se pudo vincular la wallet emisora',
    );
  }

  async acceptInvitationPublic(
    body: AcceptInstitutionInvitationRequest,
  ): Promise<InstitutionInvitationAccepted> {
    try {
      return await this.api.invoke(academyInvitationsAcceptPost, { body });
    } catch (error: unknown) {
      throw toThrownError(error, 'No se pudo aceptar la invitacion');
    }
  }

  async listInstitutions(): Promise<readonly AcademyInstitutionSummary[]> {
    this.requireJwt();

    return this.getJson<readonly AcademyInstitutionSummary[]>(
      `${BFF_API_BASE}/academy/institutions`,
      'No se pudieron listar las instituciones',
    );
  }

  async listStudents(institutionId: string): Promise<readonly StudentSummary[]> {
    this.requireJwt();

    return this.getJson<readonly StudentSummary[]>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/students`,
      'No se pudieron listar los estudiantes',
    );
  }

  async listCareers(institutionId: string): Promise<readonly CareerSummary[]> {
    this.requireJwt();

    return this.getJson<readonly CareerSummary[]>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/careers`,
      'No se pudieron listar las carreras',
    );
  }

  async createCareer(
    institutionId: string,
    body: CreateCareerPayload,
  ): Promise<CareerSummary> {
    this.requireJwt();

    return this.postJson<CareerSummary>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/careers`,
      body,
      'No se pudo crear la carrera',
    );
  }

  async updateCareer(
    institutionId: string,
    careerId: string,
    body: UpdateCareerPayload,
  ): Promise<CareerSummary> {
    this.requireJwt();

    return this.patchJson<CareerSummary>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/careers/${careerId}`,
      body,
      'No se pudo actualizar la carrera',
    );
  }

  async deactivateCareer(
    institutionId: string,
    careerId: string,
  ): Promise<CareerSummary> {
    this.requireJwt();

    try {
      return await firstValueFrom(
        this.http.delete<CareerSummary>(
          `${BFF_API_BASE}/academy/institutions/${institutionId}/careers/${careerId}`,
        ),
      );
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo desactivar la carrera');
    }
  }

  async getStudent(
    institutionId: string,
    studentId: string,
  ): Promise<StudentSummary> {
    this.requireJwt();

    return this.getJson<StudentSummary>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/students/${studentId}`,
      'No se pudo obtener el estudiante',
    );
  }

  async createStudentDirect(
    institutionId: string,
    body: CreateStudentPayload,
  ): Promise<StudentSummary> {
    this.requireJwt();

    return this.postJson<StudentSummary>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/students`,
      body,
      'No se pudo crear el estudiante',
    );
  }

  async addStudentWallet(
    institutionId: string,
    studentId: string,
    body: AddStudentWalletPayload,
  ): Promise<StudentWalletSummary> {
    this.requireJwt();

    return this.postJson<StudentWalletSummary>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/students/${studentId}/wallets`,
      body,
      'No se pudo vincular la wallet del estudiante',
    );
  }

  async listInstitutionUsers(
    institutionId: string,
  ): Promise<readonly InstitutionUserSummary[]> {
    this.requireJwt();

    return this.getJson<readonly InstitutionUserSummary[]>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/users`,
      'No se pudieron listar los usuarios institucionales',
    );
  }

  async inviteInstitutionUser(
    institutionId: string,
    body: InviteInstitutionUserPayload,
  ): Promise<InstitutionInvitationCreated> {
    this.requireJwt();

    return this.postJson<InstitutionInvitationCreated>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/users/invitations`,
      body,
      'No se pudo invitar al usuario institucional',
    );
  }

  async updateInstitutionUserRole(
    institutionId: string,
    userId: string,
    body: UpdateInstitutionUserRolePayload,
  ): Promise<InstitutionUserSummary> {
    this.requireJwt();

    return this.patchJson<InstitutionUserSummary>(
      `${BFF_API_BASE}/academy/institutions/${institutionId}/users/${userId}/role`,
      body,
      'No se pudo cambiar el rol del usuario institucional',
    );
  }

  async revokeInstitutionUser(
    institutionId: string,
    userId: string,
  ): Promise<void> {
    this.requireJwt();

    try {
      await firstValueFrom(
        this.http.delete<void>(
          `${BFF_API_BASE}/academy/institutions/${institutionId}/users/${userId}`,
        ),
      );
    } catch (error: unknown) {
      throw this.mapApiError(error, 'No se pudo revocar el usuario institucional');
    }
  }

  private mapApiError(error: unknown, fallback: string): Error {
    if (error instanceof HttpErrorResponse && error.status === 401) {
      return new PlatformUnauthorizedError(toHttpErrorMessage(error, fallback));
    }

    return toThrownError(error, fallback);
  }

  private async getJson<T>(url: string, fallback: string): Promise<T> {
    try {
      return await firstValueFrom(this.http.get<T>(url));
    } catch (error: unknown) {
      throw this.mapApiError(error, fallback);
    }
  }

  private async postJson<T>(
    url: string,
    body: unknown,
    fallback: string,
  ): Promise<T> {
    try {
      return await firstValueFrom(this.http.post<T>(url, body));
    } catch (error: unknown) {
      throw this.mapApiError(error, fallback);
    }
  }

  private async patchJson<T>(
    url: string,
    body: unknown,
    fallback: string,
  ): Promise<T> {
    try {
      return await firstValueFrom(this.http.patch<T>(url, body));
    } catch (error: unknown) {
      throw this.mapApiError(error, fallback);
    }
  }
}
