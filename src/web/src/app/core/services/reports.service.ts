import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { Api, ApiFnRequired } from '../../api/bff/api';
import { reportsInstitutionsInstitutionIdCredentialReadsGet } from '../../api/bff/fn/reports/reports-institutions-institution-id-credential-reads-get';
import { reportsInstitutionsInstitutionIdCredentialsIssuedGet } from '../../api/bff/fn/reports/reports-institutions-institution-id-credentials-issued-get';
import { reportsInstitutionsInstitutionIdCredentialsPerStudentGet } from '../../api/bff/fn/reports/reports-institutions-institution-id-credentials-per-student-get';
import { reportsInstitutionsInstitutionIdCredentialsRevokedGet } from '../../api/bff/fn/reports/reports-institutions-institution-id-credentials-revoked-get';
import { reportsInstitutionsInstitutionIdVerificationOutcomesGet } from '../../api/bff/fn/reports/reports-institutions-institution-id-verification-outcomes-get';
import { reportsPlatformCredentialsByInstitutionGet } from '../../api/bff/fn/reports/reports-platform-credentials-by-institution-get';
import { reportsPlatformStudentsByInstitutionGet } from '../../api/bff/fn/reports/reports-platform-students-by-institution-get';
import {
  CredentialsPerStudentReport,
  PlatformCredentialsByInstitutionReport,
  PlatformStudentsByInstitutionReport,
  TimeSeriesReport,
  VerificationOutcomesReport,
} from '../models/reports.models';
import { toHttpErrorMessage, toThrownError } from '../utils/error.utils';
import { AuthService } from './auth.service';
import { PlatformUnauthenticatedError, PlatformUnauthorizedError } from './academy.service';

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private readonly api = inject(Api);
  private readonly authService = inject(AuthService);

  private requireJwt(): void {
    const jwt = this.authService.getJwt();
    if (!jwt || !this.authService.isAuthenticated()) {
      throw new PlatformUnauthenticatedError();
    }
  }

  /** R-I1 */
  async getCredentialsIssued(
    institutionId: string,
    from: string,
    to: string,
  ): Promise<TimeSeriesReport> {
    this.requireJwt();
    return this.invokeReport(
      reportsInstitutionsInstitutionIdCredentialsIssuedGet,
      { institutionId, from, to },
      'No se pudo obtener el reporte de emisiones',
    );
  }

  /** R-I2 */
  async getCredentialReads(
    institutionId: string,
    from: string,
    to: string,
  ): Promise<TimeSeriesReport> {
    this.requireJwt();
    return this.invokeReport(
      reportsInstitutionsInstitutionIdCredentialReadsGet,
      { institutionId, from, to },
      'No se pudo obtener el reporte de verificaciones',
    );
  }

  /** R-I3 */
  async getCredentialsPerStudent(
    institutionId: string,
    asOf: string,
  ): Promise<CredentialsPerStudentReport> {
    this.requireJwt();
    return this.invokeReport(
      reportsInstitutionsInstitutionIdCredentialsPerStudentGet,
      { institutionId, asOf },
      'No se pudo obtener el reporte de credenciales por alumno',
    );
  }

  /** R-I4 */
  async getCredentialsRevoked(
    institutionId: string,
    from: string,
    to: string,
  ): Promise<TimeSeriesReport> {
    this.requireJwt();
    return this.invokeReport(
      reportsInstitutionsInstitutionIdCredentialsRevokedGet,
      { institutionId, from, to },
      'No se pudo obtener el reporte de revocaciones',
    );
  }

  /** R-I5 */
  async getVerificationOutcomes(
    institutionId: string,
    from: string,
    to: string,
  ): Promise<VerificationOutcomesReport> {
    this.requireJwt();
    return this.invokeReport(
      reportsInstitutionsInstitutionIdVerificationOutcomesGet,
      { institutionId, from, to },
      'No se pudo obtener el reporte de resultados de verificacion',
    );
  }

  /** R-P1 */
  async getPlatformCredentialsByInstitution(
    from: string,
    to: string,
  ): Promise<PlatformCredentialsByInstitutionReport> {
    this.requireJwt();
    return this.invokeReport(
      reportsPlatformCredentialsByInstitutionGet,
      { from, to },
      'No se pudo obtener el ranking de credenciales por institucion',
    );
  }

  /** R-P2 */
  async getPlatformStudentsByInstitution(
    asOf: string,
  ): Promise<PlatformStudentsByInstitutionReport> {
    this.requireJwt();
    return this.invokeReport(
      reportsPlatformStudentsByInstitutionGet,
      { asOf },
      'No se pudo obtener el ranking de alumnos por institucion',
    );
  }

  private async invokeReport<P, R>(
    fn: ApiFnRequired<P, R>,
    params: P,
    fallback: string,
  ): Promise<R> {
    try {
      return await this.api.invoke(fn, params);
    } catch (error: unknown) {
      throw this.mapApiError(error, fallback);
    }
  }

  private mapApiError(error: unknown, fallback: string): Error {
    if (error instanceof HttpErrorResponse && error.status === 401) {
      return new PlatformUnauthorizedError(toHttpErrorMessage(error, fallback));
    }

    return toThrownError(error, fallback);
  }
}
