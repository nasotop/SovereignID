import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { PlatformComponent } from './platform.component';
import { AcademyService } from '../../../core/services/academy.service';
import { AuthService } from '../../../core/services/auth.service';

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
            createInvitation: vi.fn(),
            inviteInstitutionUser: vi.fn(),
            listInstitutions: vi.fn().mockResolvedValue([]),
          },
        },
        {
          provide: AuthService,
          useValue: {
            logout: vi.fn().mockResolvedValue(undefined),
            isAuthenticated: () => true,
            getJwt: () => 'test-jwt',
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

  it('should render institution actions without an inline create form', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Crear institucion');
    expect(compiled.textContent).toContain('Vista general');
    expect(compiled.querySelector('dialog[open] #code')).toBeFalsy();
    expect(compiled.querySelector('dialog[open] #contactEmail')).toBeFalsy();
  });
});
