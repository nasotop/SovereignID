import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { Api } from '../../api/bff/api';
import { reportsInstitutionsInstitutionIdCredentialsIssuedGet } from '../../api/bff/fn/reports/reports-institutions-institution-id-credentials-issued-get';
import { reportsPlatformCredentialsByInstitutionGet } from '../../api/bff/fn/reports/reports-platform-credentials-by-institution-get';
import {
  PlatformUnauthorizedError,
} from './academy.service';
import { AuthService } from './auth.service';
import { ReportsService } from './reports.service';

describe('ReportsService', () => {
  let service: ReportsService;
  let apiInvoke: ReturnType<typeof vi.fn>;
  let authService: {
    getJwt: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authService = {
      getJwt: vi.fn().mockReturnValue('test-jwt'),
      isAuthenticated: vi.fn().mockReturnValue(true),
    };

    apiInvoke = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        ReportsService,
        { provide: AuthService, useValue: authService },
        { provide: Api, useValue: { invoke: apiInvoke } },
      ],
    });

    service = TestBed.inject(ReportsService);
  });

  it('should call credentials-issued route with date params', async () => {
    const response = {
      period: { from: '2026-06-06', to: '2026-07-05' },
      source: 'hybrid',
      total: 12,
      series: [{ date: '2026-07-05', value: 3 }],
    };
    apiInvoke.mockResolvedValue(response);

    const institutionId = '11111111-1111-1111-1111-111111111111';
    const result = await service.getCredentialsIssued(
      institutionId,
      '2026-06-06',
      '2026-07-05',
    );

    expect(apiInvoke).toHaveBeenCalledWith(
      reportsInstitutionsInstitutionIdCredentialsIssuedGet,
      {
        institutionId,
        from: '2026-06-06',
        to: '2026-07-05',
      },
    );
    expect(result.total).toBe(12);
  });

  it('should call platform credentials-by-institution route', async () => {
    apiInvoke.mockResolvedValue({
      period: { from: '2026-06-06', to: '2026-07-05' },
      source: 'live',
      items: [],
    });

    await service.getPlatformCredentialsByInstitution('2026-06-06', '2026-07-05');

    expect(apiInvoke).toHaveBeenCalledWith(
      reportsPlatformCredentialsByInstitutionGet,
      { from: '2026-06-06', to: '2026-07-05' },
    );
  });

  it('should map 401 responses to PlatformUnauthorizedError', async () => {
    apiInvoke.mockRejectedValue(
      new HttpErrorResponse({
        status: 401,
        statusText: 'Unauthorized',
        error: { detail: 'Sesion expirada' },
      }),
    );

    await expect(
      service.getCredentialsIssued(
        '11111111-1111-1111-1111-111111111111',
        '2026-06-06',
        '2026-07-05',
      ),
    ).rejects.toBeInstanceOf(PlatformUnauthorizedError);
  });

  it('should translate Problem Details detail on errors', async () => {
    apiInvoke.mockRejectedValue(
      new HttpErrorResponse({
        status: 403,
        statusText: 'Forbidden',
        error: { detail: 'Sin permisos para reportes' },
      }),
    );

    await expect(
      service.getPlatformStudentsByInstitution('2026-07-05'),
    ).rejects.toThrow('Sin permisos para reportes');
  });

  it('should require authentication before calling reports', async () => {
    authService.isAuthenticated.mockReturnValue(false);
    authService.getJwt.mockReturnValue(null);

    await expect(
      service.getCredentialsIssued(
        '11111111-1111-1111-1111-111111111111',
        '2026-06-06',
        '2026-07-05',
      ),
    ).rejects.toBeInstanceOf(Error);
  });
});
