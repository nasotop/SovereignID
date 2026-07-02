import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { VerificationChecksResponse } from '../../../api/bff/models/verification-checks-response';
import { VerificationResponse } from '../../../api/bff/models/verification-response';
import { VerifierService } from '../../../core/services/verifier.service';
import { toErrorMessage } from '../../../core/utils/error.utils';

type VerifierState = 'idle' | 'loading' | 'result' | 'error';

type CheckKey = keyof VerificationChecksResponse;

const CHECK_LABELS: Record<CheckKey, string> = {
  found: 'Encontrada en registro',
  notRevoked: 'No revocada',
  notExpired: 'No expirada',
  hashMatches: 'Hash coincide',
  onChainExists: 'Existe on-chain',
  signatureValid: 'Firma válida',
};

const RESULT_LABELS: Record<VerificationResponse['result'], string> = {
  valid: 'Credencial válida',
  revoked: 'Credencial revocada',
  expired: 'Credencial expirada',
  not_found: 'Credencial inexistente',
};

const RESULT_BADGE_CLASSES: Record<VerificationResponse['result'], string> = {
  valid: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40',
  revoked: 'bg-red-500/20 text-red-300 border-red-500/40',
  expired: 'bg-amber-500/20 text-amber-300 border-amber-500/40',
  not_found: 'bg-slate-500/20 text-slate-300 border-slate-500/40',
};

@Component({
  selector: 'app-verifier',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen bg-slate-900 flex flex-col">
      <header class="pt-12 pb-8 px-6 text-center">
        <div
          class="w-16 h-16 rounded-2xl bg-blue-600 flex items-center justify-center mx-auto mb-5"
        >
          <svg
            class="w-9 h-9 text-white"
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
        <h1 class="text-3xl font-bold text-white mb-2">SovereignID</h1>
        <p class="text-slate-400 text-lg">Credential Verifier</p>
        <p class="text-slate-500 text-sm mt-2 max-w-md mx-auto">
          Public verification service — no account required
        </p>
      </header>

      <main class="flex-1 flex items-start justify-center px-6 pb-16">
        <div class="w-full max-w-2xl space-y-6">
          <section class="rounded-2xl bg-slate-800/50 border border-slate-700 p-6">
            <label
              for="credentialId"
              class="block text-sm font-medium text-slate-300 mb-2"
            >
              Credential ID (UUID)
            </label>
            <input
              id="credentialId"
              type="text"
              name="credentialId"
              autocomplete="off"
              spellcheck="false"
              placeholder="00000000-0000-0000-0000-000000000000"
              class="w-full rounded-xl border border-slate-600 bg-slate-900 px-4 py-3 text-white placeholder:text-slate-500 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30"
              [ngModel]="credentialId()"
              (ngModelChange)="onCredentialIdChange($event)"
              [disabled]="state() === 'loading'"
            />

            @if (validationError()) {
              <p class="mt-3 text-sm text-red-400" role="alert">
                {{ validationError() }}
              </p>
            }

            <button
              type="button"
              class="w-full mt-4 py-4 px-6 text-base font-semibold text-white bg-blue-600 hover:bg-blue-500 disabled:bg-blue-600/50 disabled:cursor-not-allowed rounded-xl transition-colors flex items-center justify-center gap-3"
              [disabled]="!canSubmit() || state() === 'loading'"
              (click)="handleVerify()"
            >
              @if (state() === 'loading') {
                <svg
                  class="w-5 h-5 animate-spin"
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <circle
                    class="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    stroke-width="4"
                  ></circle>
                  <path
                    class="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  ></path>
                </svg>
                <span>Verifying...</span>
              } @else {
                <svg
                  class="w-5 h-5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  aria-hidden="true"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                <span>Verify Credential</span>
              }
            </button>
          </section>

          @if (state() === 'result' && verificationResult(); as result) {
            <section class="rounded-2xl bg-slate-800/50 border border-slate-700 p-6 space-y-6">
              <div class="flex items-center justify-between gap-4">
                <h2 class="text-lg font-semibold text-white">Verification result</h2>
                <span
                  class="inline-flex items-center rounded-full border px-3 py-1 text-sm font-medium"
                  [ngClass]="resultBadgeClass(result.result)"
                >
                  {{ resultLabel(result.result) }}
                </span>
              </div>

              <div>
                <h3 class="text-sm font-medium text-slate-300 mb-3">Checks</h3>
                <ul class="space-y-2">
                  @for (check of checkEntries; track check.key) {
                    <li
                      class="flex items-center justify-between rounded-lg bg-slate-900/60 px-4 py-3 text-sm"
                    >
                      <span class="text-slate-300">{{ check.label }}</span>
                      <span [ngClass]="checkValueClass(result.checks[check.key])">
                        {{ formatCheckValue(result.checks[check.key]) }}
                      </span>
                    </li>
                  }
                </ul>
              </div>

              @if (result.credential; as credential) {
                <div>
                  <h3 class="text-sm font-medium text-slate-300 mb-3">Credential</h3>
                  <dl class="grid gap-3 rounded-lg bg-slate-900/60 p-4 text-sm">
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">ID</dt>
                      <dd class="text-slate-200 break-all">{{ credential.id }}</dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Type</dt>
                      <dd class="text-slate-200">{{ credential.type }}</dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Status</dt>
                      <dd class="text-slate-200">{{ credential.status }}</dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Issuer</dt>
                      <dd class="text-slate-200">
                        {{ credential.issuer.displayName }}
                        ({{ credential.issuer.code }})
                      </dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Issuer DID</dt>
                      <dd class="text-slate-200 break-all">{{ credential.issuer.did }}</dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Subject DID</dt>
                      <dd class="text-slate-200 break-all">{{ credential.subjectDid }}</dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Issued at</dt>
                      <dd class="text-slate-200">{{ credential.issuedAt }}</dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Expires at</dt>
                      <dd class="text-slate-200">
                        {{ credential.expiresAt ?? '—' }}
                      </dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">IPFS CID</dt>
                      <dd class="text-slate-200 break-all">
                        {{ credential.anchors.ipfsCid }}
                      </dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Content hash</dt>
                      <dd class="text-slate-200 break-all">
                        {{ credential.anchors.contentHash }}
                      </dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Transaction</dt>
                      <dd class="text-slate-200 break-all">
                        {{ credential.anchors.transactionHash }}
                      </dd>
                    </div>
                    <div class="grid grid-cols-[8rem_1fr] gap-2">
                      <dt class="text-slate-500">Chain ID</dt>
                      <dd class="text-slate-200">{{ credential.anchors.chainId }}</dd>
                    </div>
                  </dl>
                </div>
              }
            </section>
          }

          @if (state() === 'error') {
            <section
              class="rounded-2xl bg-red-950/30 border border-red-800/60 p-6 text-center"
            >
              <p class="text-red-300 font-semibold mb-2">Verification failed</p>
              <p class="text-slate-300 text-sm mb-4">
                {{ errorMessage() || 'Please try again' }}
              </p>
              <button
                type="button"
                class="w-full py-3 px-4 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 rounded-xl transition-colors"
                (click)="resetError()"
              >
                Try again
              </button>
            </section>
          }

          <p class="text-center text-slate-500 text-xs">
            Verification is performed against the blockchain-anchored issuer registry
          </p>
        </div>
      </main>
    </div>
  `,
})
export class VerifierComponent {
  private readonly verifierService = inject(VerifierService);

  readonly credentialId = signal('');
  readonly state = signal<VerifierState>('idle');
  readonly validationError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly verificationResult = signal<VerificationResponse | null>(null);

  readonly canSubmit = computed(() =>
    this.verifierService.isValidCredentialId(this.credentialId()),
  );

  readonly checkEntries = (
    Object.entries(CHECK_LABELS) as [CheckKey, string][]
  ).map(([key, label]) => ({ key, label }));

  onCredentialIdChange(value: string): void {
    this.credentialId.set(value);
    if (this.validationError() && this.verifierService.isValidCredentialId(value)) {
      this.validationError.set(null);
    }
  }

  async handleVerify(): Promise<void> {
    if (!this.verifierService.isValidCredentialId(this.credentialId())) {
      this.validationError.set('El credentialId no es un UUID válido.');
      return;
    }

    this.validationError.set(null);
    this.errorMessage.set(null);
    this.verificationResult.set(null);
    this.state.set('loading');

    try {
      const result = await this.verifierService.verifyCredential(this.credentialId());
      this.verificationResult.set(result);
      this.state.set('result');
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
      this.state.set('error');
    }
  }

  resetError(): void {
    this.state.set('idle');
    this.errorMessage.set(null);
  }

  resultLabel(result: VerificationResponse['result']): string {
    return RESULT_LABELS[result];
  }

  resultBadgeClass(result: VerificationResponse['result']): string {
    return RESULT_BADGE_CLASSES[result];
  }

  formatCheckValue(value: boolean | null): string {
    if (value === null) {
      return 'No evaluado';
    }

    return value ? 'Sí' : 'No';
  }

  checkValueClass(value: boolean | null): string {
    if (value === null) {
      return 'text-slate-400 italic';
    }

    return value ? 'text-emerald-400 font-medium' : 'text-red-400 font-medium';
  }
}
