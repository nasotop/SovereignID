import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { Api } from '../../api/bff/api';
import { academyInstitutionsPost } from '../../api/bff/fn/academy-institutions/academy-institutions-post';
import { InstitutionCreated } from '../../api/bff/models/institution-created';
import {
  AcademyService,
  PlatformUnauthorizedError,
} from './academy.service';
import { AuthService } from './auth.service';

describe('AcademyService', () => {
  let service: AcademyService;
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

    apiInvoke = vi.fn().mockResolvedValue({
      institution: { id: '11111111-1111-1111-1111-111111111111' },
    } satisfies InstitutionCreated);

    TestBed.configureTestingModule({
      providers: [
        AcademyService,
        { provide: AuthService, useValue: authService },
        {
          provide: Api,
          useValue: { invoke: apiInvoke },
        },
      ],
    });

    service = TestBed.inject(AcademyService);
  });

  it('should create institution via BFF client', async () => {
    const body = {
      code: 'TEST',
      legalName: 'Test Legal',
      displayName: 'Test',
      contactEmail: 'admin@test.cl',
      countryCode: 'CL',
    };

    const result = await service.createInstitution(body);

    expect(apiInvoke).toHaveBeenCalledWith(academyInstitutionsPost, { body });
    expect(result.institution?.id).toBe('11111111-1111-1111-1111-111111111111');
  });

  it('should map 401 responses to PlatformUnauthorizedError', async () => {
    apiInvoke.mockRejectedValue(
      new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' }),
    );

    await expect(
      service.createInstitution({
        code: 'TEST',
        legalName: 'Test',
        displayName: 'Test',
        contactEmail: 'admin@test.cl',
      }),
    ).rejects.toBeInstanceOf(PlatformUnauthorizedError);
  });

  it('should accept invitation without requiring JWT', async () => {
    authService.isAuthenticated.mockReturnValue(false);
    authService.getJwt.mockReturnValue(null);
    apiInvoke.mockResolvedValue({
      institutionId: '11111111-1111-1111-1111-111111111111',
      role: 'admin',
    });

    const result = await service.acceptInvitationPublic({
      token: 'invite-token',
      walletAddress: '0x1111111111111111111111111111111111111111',
    });

    expect(result.role).toBe('admin');
    expect(authService.getJwt).not.toHaveBeenCalled();
  });
});
