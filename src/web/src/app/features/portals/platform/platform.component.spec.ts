import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { PlatformComponent } from './platform.component';
import { AcademyService } from '../../../core/services/academy.service';
import { AuthService } from '../../../core/services/auth.service';
import { ReportsService } from '../../../core/services/reports.service';

describe('PlatformComponent', () => {
  let fixture: ComponentFixture<PlatformComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlatformComponent],
      providers: [
        provideRouter([]),
        {
          provide: AcademyService,
          useValue: {
            createInstitution: vi.fn(),
            getInstitution: vi.fn(),
            inviteInstitutionUser: vi.fn(),
            listInstitutions: vi.fn().mockResolvedValue([]),
          },
        },
        {
          provide: ReportsService,
          useValue: {
            getPlatformCredentialsByInstitution: vi.fn().mockResolvedValue({
              period: { from: '2026-06-06', to: '2026-07-05' },
              source: 'live',
              items: [],
            }),
            getPlatformStudentsByInstitution: vi.fn().mockResolvedValue({
              asOf: '2026-07-05',
              items: [],
            }),
          },
        },
        {
          provide: AuthService,
          useValue: {
            logout: vi.fn().mockResolvedValue(undefined),
            isAuthenticated: () => true,
            getJwt: () => 'test-jwt',
            getAddress: () => '0x0000000000000000000000000000000000000000',
            getShortAddress: () => '0x0000...0000',
            getUserDisplayName: () => '0x0000...0000',
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PlatformComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render platform tabs', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Instituciones');
    expect(compiled.textContent).toContain('Reportes');
  });

  it('should show institutions tab by default', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Crear institucion');
    expect(compiled.querySelector('app-platform-reports-tab')).toBeFalsy();
  });

  it('should mount reports tab when selected', () => {
    fixture.componentInstance.setActiveTab('reports');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-platform-reports-tab')).toBeTruthy();
    expect(compiled.querySelector('app-platform-institutions-tab')).toBeFalsy();
  });
});
