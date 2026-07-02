import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { HolderCredentialSummary } from '../../../api/bff/models/holder-credential-summary';
import {
  HolderService,
  HolderUnauthorizedError,
} from '../../../core/services/holder.service';
import { AuthService } from '../../../core/services/auth.service';
import { toErrorMessage } from '../../../core/utils/error.utils';

type HolderLoadState = 'loading' | 'loaded' | 'empty' | 'error';

const STATUS_LABELS: Record<HolderCredentialSummary['status'], string> = {
  active: 'Active',
  revoked: 'Revoked',
  expired: 'Expired',
};

const STATUS_BADGE_CLASSES: Record<HolderCredentialSummary['status'], string> = {
  active: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30',
  revoked: 'bg-red-500/15 text-red-400 border-red-500/30',
  expired: 'bg-amber-500/15 text-amber-400 border-amber-500/30',
};

const STATUS_DOT_CLASSES: Record<HolderCredentialSummary['status'], string> = {
  active: 'bg-emerald-400',
  revoked: 'bg-red-400',
  expired: 'bg-amber-400',
};

@Component({
  selector: 'app-holder',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-slate-900">
      <nav
        class="border-b border-slate-700/60 bg-slate-800/80 backdrop-blur-sm"
      >
        <div
          class="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between"
        >
          <div class="flex items-center gap-3">
            <div
              class="w-9 h-9 rounded-lg bg-blue-600 flex items-center justify-center"
            >
              <svg
                class="w-5 h-5 text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                aria-hidden="true"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                />
              </svg>
            </div>
            <div>
              <h1 class="text-lg font-bold text-white tracking-tight">
                SovereignID
              </h1>
              <p class="text-xs text-slate-400">Holder Portal</p>
            </div>
          </div>
          <button
            type="button"
            class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-slate-300 hover:text-white bg-slate-700/50 hover:bg-slate-700 border border-slate-600 rounded-lg transition-colors"
            (click)="handleLogout()"
          >
            <svg
              class="w-4 h-4"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
              />
            </svg>
            Logout
          </button>
        </div>
      </nav>

      <main class="max-w-7xl mx-auto px-6 py-8">
        <div class="mb-8">
          <h2 class="text-2xl font-bold text-white">My Credentials</h2>
          <p class="text-slate-400 mt-1">
            Your verifiable digital credentials issued by trusted institutions
          </p>
        </div>

        @if (loadState() === 'loading') {
          <div
            class="rounded-xl border border-slate-700 bg-slate-800/50 p-10 text-center text-slate-300"
          >
            Loading credentials...
          </div>
        }

        @if (loadState() === 'empty') {
          <div
            class="rounded-xl border border-slate-700 bg-slate-800/50 p-10 text-center"
          >
            <p class="text-white font-medium mb-2">No credentials yet</p>
            <p class="text-slate-400 text-sm">
              You do not have any issued credentials for this account.
            </p>
          </div>
        }

        @if (loadState() === 'error') {
          <div
            class="rounded-xl border border-red-800/60 bg-red-950/30 p-8 text-center"
          >
            <p class="text-red-300 font-semibold mb-2">
              {{ isUnauthorized() ? 'Session expired' : 'Failed to load credentials' }}
            </p>
            <p class="text-slate-300 text-sm mb-4">
              {{ errorMessage() || 'Please try again' }}
            </p>
            @if (isUnauthorized()) {
              <button
                type="button"
                class="px-4 py-2.5 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 rounded-lg transition-colors"
                (click)="goToLogin()"
              >
                Go to login
              </button>
            } @else {
              <button
                type="button"
                class="px-4 py-2.5 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 rounded-lg transition-colors"
                (click)="loadCredentials()"
              >
                Try again
              </button>
            }
          </div>
        }

        @if (loadState() === 'loaded') {
          @if (shareFeedback()) {
            <p class="mb-4 text-sm text-emerald-400" role="status">
              {{ shareFeedback() }}
            </p>
          }

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            @for (credential of credentials(); track credential.id) {
              <div
                class="bg-slate-800 border border-slate-700 rounded-xl p-6 flex flex-col hover:border-slate-600 transition-colors"
              >
                <div class="flex items-start justify-between mb-5">
                  <div
                    class="w-14 h-14 rounded-xl bg-blue-600/20 flex items-center justify-center"
                  >
                    @if (isDegreeType(credential.typeCode)) {
                      <svg
                        class="w-7 h-7 text-blue-400"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                        aria-hidden="true"
                      >
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="1.5"
                          d="M12 14l9-5-9-5-9 5 9 5zm0 0l6.16-3.422a12.083 12.083 0 01.665 6.479A11.952 11.952 0 0012 20.055a11.952 11.952 0 00-6.824-2.998 12.078 12.078 0 01.665-6.479L12 14zm-4 6v-7.5l4-2.222"
                        />
                      </svg>
                    } @else {
                      <svg
                        class="w-7 h-7 text-blue-400"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                        aria-hidden="true"
                      >
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="1.5"
                          d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                        />
                      </svg>
                    }
                  </div>
                  <span
                    class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border"
                    [ngClass]="statusBadgeClass(credential.status)"
                  >
                    <span
                      class="w-1.5 h-1.5 rounded-full"
                      [ngClass]="statusDotClass(credential.status)"
                    ></span>
                    {{ statusLabel(credential.status) }}
                  </span>
                </div>

                <h3 class="text-lg font-semibold text-white mb-1">
                  {{ credential.title }}
                </h3>
                <p class="text-sm text-slate-400 mb-1">
                  Issued by {{ credential.issuerName }}
                </p>
                <p class="text-xs text-slate-500 mb-6">
                  {{ formatIssuedAt(credential.issuedAt) }}
                </p>

                <div class="flex gap-3 mt-auto">
                  <button
                    type="button"
                    class="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-medium text-white bg-slate-700 hover:bg-slate-600 border border-slate-600 rounded-lg transition-colors disabled:opacity-50"
                    [disabled]="actionCredentialId() === credential.id"
                    (click)="handleDownload(credential.id)"
                  >
                    Download JSON
                  </button>
                  <button
                    type="button"
                    class="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-500 rounded-lg transition-colors"
                    (click)="handleShare(credential.id)"
                  >
                    Share QR
                  </button>
                </div>
              </div>
            }
          </div>
        }
      </main>
    </div>
  `,
})
export class HolderComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly holderService = inject(HolderService);
  private readonly router = inject(Router);

  readonly credentials = signal<ReadonlyArray<HolderCredentialSummary>>([]);
  readonly loadState = signal<HolderLoadState>('loading');
  readonly errorMessage = signal<string | null>(null);
  readonly isUnauthorized = signal(false);
  readonly shareFeedback = signal<string | null>(null);
  readonly actionCredentialId = signal<string | null>(null);

  ngOnInit(): void {
    void this.loadCredentials();
  }

  async loadCredentials(): Promise<void> {
    this.loadState.set('loading');
    this.errorMessage.set(null);
    this.isUnauthorized.set(false);
    this.shareFeedback.set(null);

    try {
      const items = await this.holderService.listMyCredentials();
      this.credentials.set(items);

      if (items.length === 0) {
        this.loadState.set('empty');
        return;
      }

      this.loadState.set('loaded');
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
      this.isUnauthorized.set(error instanceof HolderUnauthorizedError);
      this.loadState.set('error');
    }
  }

  async handleDownload(credentialId: string): Promise<void> {
    this.actionCredentialId.set(credentialId);
    this.shareFeedback.set(null);

    try {
      const detail = await this.holderService.getMyCredential(credentialId);
      this.holderService.downloadCredentialJson(detail);
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
      this.isUnauthorized.set(error instanceof HolderUnauthorizedError);
      this.loadState.set('error');
    } finally {
      this.actionCredentialId.set(null);
    }
  }

  async handleShare(credentialId: string): Promise<void> {
    this.shareFeedback.set(null);

    try {
      await this.holderService.shareCredentialId(credentialId);
      this.shareFeedback.set(`Credential ID copied: ${credentialId}`);
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
      this.loadState.set('error');
    }
  }

  handleLogout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  goToLogin(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  isDegreeType(typeCode: string): boolean {
    return this.holderService.isDegreeType(typeCode);
  }

  statusLabel(status: HolderCredentialSummary['status']): string {
    return STATUS_LABELS[status];
  }

  statusBadgeClass(status: HolderCredentialSummary['status']): string {
    return STATUS_BADGE_CLASSES[status];
  }

  statusDotClass(status: HolderCredentialSummary['status']): string {
    return STATUS_DOT_CLASSES[status];
  }

  formatIssuedAt(issuedAt: string): string {
    const date = new Date(issuedAt);
    if (Number.isNaN(date.getTime())) {
      return issuedAt;
    }

    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }
}
