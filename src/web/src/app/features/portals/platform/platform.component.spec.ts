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

  it('should render institution create form', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Crear institución');
    expect(compiled.querySelector('#code')).toBeTruthy();
    expect(compiled.querySelector('#contactEmail')).toBeTruthy();
  });
});
