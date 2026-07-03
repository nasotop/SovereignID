import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AcceptInvitationComponent } from './accept-invitation.component';
import { AcademyService } from '../../core/services/academy.service';
import { Web3Service } from '../../core/services/web3.service';

describe('AcceptInvitationComponent', () => {
  let fixture: ComponentFixture<AcceptInvitationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcceptInvitationComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: (key: string) => (key === 'token' ? 'test-token' : null),
              },
            },
          },
        },
        {
          provide: AcademyService,
          useValue: {
            acceptInvitationPublic: vi.fn().mockResolvedValue({}),
          },
        },
        {
          provide: Web3Service,
          useValue: {
            isMetaMaskAvailable: () => true,
            connectWallet: vi
              .fn()
              .mockResolvedValue('0x1111111111111111111111111111111111111111'),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AcceptInvitationComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should read invitation token from query params', () => {
    expect(fixture.componentInstance.invitationToken()).toBe('test-token');
  });
});
