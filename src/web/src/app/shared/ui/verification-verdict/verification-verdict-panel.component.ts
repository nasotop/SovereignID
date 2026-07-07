import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';

import { VerificationResponse } from '../../../api/bff/models/verification-response';
import { CredentialAnchorsPanelComponent } from '../credential-anchors';
import { CopyValueComponent } from '../copy-value/copy-value.component';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import {
  buildVerdictViewModel,
  VERIFIER_FULL_PRESENTATION,
  VerdictPresentation,
} from './verification-verdict-model.builder';

@Component({
  selector: 'app-verification-verdict-panel',
  standalone: true,
  imports: [
    CommonModule,
    CredentialAnchorsPanelComponent,
    CopyValueComponent,
    StatusBadgeComponent,
  ],
  template: `
    @if (viewModel(); as vm) {
      <section class="rounded-2xl bg-slate-800/50 border border-slate-700 p-6 space-y-6">
        <div class="flex items-center justify-between gap-4">
          <h2 class="text-lg font-semibold text-white">Verification result</h2>
          <app-status-badge [label]="vm.resultLabel" [tone]="vm.resultTone" />
        </div>

        @if (vm.evidenceBanner.visible) {
          <div
            class="rounded-lg border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-200"
            role="status"
          >
            {{ vm.evidenceBanner.message }}
          </div>
        }

        @for (group of vm.groups; track group.title) {
          <div>
            <h3 class="text-sm font-medium text-slate-300 mb-3">{{ group.title }}</h3>
            <ul class="space-y-2">
              @for (row of group.rows; track row.key) {
                <li
                  class="flex items-center justify-between rounded-lg bg-slate-900/60 px-4 py-3 text-sm"
                >
                  <span class="text-slate-300">{{ row.label }}</span>
                  <span [ngClass]="rowToneClass(row.tone)">{{ row.displayValue }}</span>
                </li>
              }
            </ul>
          </div>
        }

        @if (vm.credential; as credential) {
          <div>
            <h3 class="text-sm font-medium text-slate-300 mb-3">Credential</h3>
            <div class="rounded-lg bg-slate-900/60 p-4 space-y-4">
              <dl class="grid gap-3 text-sm">
                <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
                  <dt class="text-slate-500">ID</dt>
                  <dd>
                    <app-copy-value [value]="credential.id" />
                  </dd>
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
                    {{ credential.issuer.displayName }} ({{ credential.issuer.code }})
                  </dd>
                </div>
                <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
                  <dt class="text-slate-500">Issuer DID</dt>
                  <dd>
                    <app-copy-value [value]="credential.issuer.did" />
                  </dd>
                </div>
                <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
                  <dt class="text-slate-500">Subject DID</dt>
                  <dd>
                    <app-copy-value [value]="credential.subjectDid" />
                  </dd>
                </div>
                <div class="grid grid-cols-[8rem_1fr] gap-2">
                  <dt class="text-slate-500">Issued at</dt>
                  <dd class="text-slate-200">{{ credential.issuedAt }}</dd>
                </div>
                <div class="grid grid-cols-[8rem_1fr] gap-2">
                  <dt class="text-slate-500">Expires at</dt>
                  <dd class="text-slate-200">{{ credential.expiresAt ?? '—' }}</dd>
                </div>
              </dl>

              <div>
                <h4 class="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-2">
                  Anclas
                </h4>
                <app-credential-anchors-panel [anchors]="credential.anchors" />
              </div>
            </div>
          </div>
        }
      </section>
    }
  `,
})
export class VerificationVerdictPanelComponent {
  readonly response = input.required<VerificationResponse>();
  readonly presentation = input<VerdictPresentation>(VERIFIER_FULL_PRESENTATION);

  readonly viewModel = computed(() =>
    buildVerdictViewModel(this.response(), this.presentation()),
  );

  rowToneClass(tone: 'success' | 'danger' | 'neutral' | 'muted'): string {
    switch (tone) {
      case 'success':
        return 'text-emerald-400 font-medium';
      case 'danger':
        return 'text-red-400 font-medium';
      case 'muted':
        return 'text-slate-400 italic';
      default:
        return 'text-slate-300';
    }
  }
}
