import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { form, FormField, required, submit } from '@angular/forms/signals';

import { AuthService } from '../../../core/services/auth.service';
import { CredentialService } from '../../../core/services/credential.service';
import {
  DOCUMENT_TYPE_OPTIONS,
  IssueCredentialModel,
} from '../../../core/models/credential.models';
import { ModalComponent } from '../../../shared/ui/modal/modal.component';
import { toHttpErrorMessage } from '../../../core/utils/error.utils';

const EMPTY_ISSUE_MODEL: IssueCredentialModel = {
  institutionId: '',
  studentId: '',
  careerId: '',
  studentLabel: '',
  documentType: DOCUMENT_TYPE_OPTIONS[0].code,
  issuedDate: '',
  subjectWallet: '',
  subjectDid: '',
  issuerDid: '',
};

@Component({
  selector: 'app-issuer',
  imports: [ModalComponent, FormField],
  template: `
    <div class="min-h-screen bg-slate-900">
      <nav
        class="border-b border-slate-700/60 bg-slate-800/80 backdrop-blur-sm"
      >
        <div
          class="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between"
        >
          <div>
            <h1 class="text-lg font-bold text-white tracking-tight">
              SovereignID
            </h1>
            <p class="text-xs text-slate-400">Issuer Portal</p>
          </div>
          <button
            type="button"
            class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-slate-300 hover:text-white bg-slate-700/50 hover:bg-slate-700 border border-slate-600 rounded-lg transition-colors"
            (click)="handleLogout()"
          >
            Logout
          </button>
        </div>
      </nav>

      <main class="max-w-7xl mx-auto px-6 py-8">
        <div class="mb-8">
          <h2 class="text-2xl font-bold text-white">Credential Management</h2>
          <p class="text-slate-400 mt-1">
            Issue, monitor and revoke blockchain-anchored academic credentials
          </p>
        </div>

        <div class="mb-6 bg-slate-800 border border-slate-700 rounded-xl p-4">
          <label
            for="institutionId"
            class="block text-sm font-medium text-slate-300 mb-1.5"
          >
            Institution ID
          </label>
          <div class="flex gap-3">
            <input
              id="institutionId"
              type="text"
              [value]="institutionIdInput()"
              (input)="onInstitutionInput($event)"
              placeholder="11111111-1111-1111-1111-111111111111"
              class="flex-1 px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            />
            <button
              type="button"
              class="px-4 py-2.5 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 rounded-lg"
              (click)="applyInstitutionId()"
            >
              Load
            </button>
          </div>
        </div>

        @if (errorMessage()) {
          <p class="mb-4 text-sm text-red-400">{{ errorMessage() }}</p>
        }

        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
          <div class="bg-slate-800 border border-slate-700 rounded-xl p-5">
            <p class="text-sm text-slate-400">Total Issued</p>
            <p class="text-2xl font-bold text-white">
              {{ credentialService.totalIssued() }}
            </p>
          </div>
          <div class="bg-slate-800 border border-slate-700 rounded-xl p-5">
            <p class="text-sm text-slate-400">Active</p>
            <p class="text-2xl font-bold text-emerald-400">
              {{ credentialService.activeCount() }}
            </p>
          </div>
          <div class="bg-slate-800 border border-slate-700 rounded-xl p-5">
            <p class="text-sm text-slate-400">Revoked</p>
            <p class="text-2xl font-bold text-red-400">
              {{ credentialService.revokedCount() }}
            </p>
          </div>
        </div>

        <div class="bg-slate-800 border border-slate-700 rounded-xl overflow-hidden">
          <div
            class="px-6 py-4 border-b border-slate-700 flex items-center justify-between"
          >
            <h3 class="text-lg font-semibold text-white">Issued Credentials</h3>
            <button
              type="button"
              class="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 rounded-lg"
              (click)="openIssueForm()"
            >
              Issue New Credential
            </button>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left">
              <thead>
                <tr class="border-b border-slate-700 text-xs uppercase text-slate-400">
                  <th class="px-6 py-3">Student</th>
                  <th class="px-6 py-3">Type</th>
                  <th class="px-6 py-3">Issued</th>
                  <th class="px-6 py-3">Status</th>
                  <th class="px-6 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-700/60">
                @for (
                  credential of credentialService.credentials();
                  track credential.credentialId
                ) {
                  <tr>
                    <td class="px-6 py-4 text-sm text-white">
                      {{ credential.studentLabel }}
                    </td>
                    <td class="px-6 py-4 text-sm text-slate-300">
                      {{ credential.documentType }}
                    </td>
                    <td class="px-6 py-4 text-sm text-slate-400">
                      {{ credential.issuedDate }}
                    </td>
                    <td class="px-6 py-4 text-sm">
                      {{ credential.status }}
                    </td>
                    <td class="px-6 py-4 text-right space-x-3">
                      @if (credential.ipfsGatewayUrl) {
                        <a
                          class="text-sm text-blue-400 hover:text-blue-300"
                          [href]="credential.ipfsGatewayUrl"
                          target="_blank"
                          rel="noopener noreferrer"
                        >
                          IPFS
                        </a>
                      }
                      @if (credential.status === 'active') {
                        <button
                          type="button"
                          class="text-sm text-red-400 hover:text-red-300"
                          (click)="handleRevoke(credential.credentialId)"
                          [disabled]="isSubmitting()"
                        >
                          Revoke
                        </button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </main>

      <app-modal
        [isOpen]="isFormOpen()"
        title="Issue New Credential"
        (closed)="closeIssueForm()"
      >
        <form (submit)="handleIssueSubmit($event)" class="space-y-4">
          <div>
            <label class="block text-sm text-slate-300 mb-1">Student ID (UUID)</label>
            <input
              type="text"
              [formField]="issueForm.studentId"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            />
          </div>
          <div>
            <label class="block text-sm text-slate-300 mb-1">Career ID (UUID)</label>
            <input
              type="text"
              [formField]="issueForm.careerId"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            />
          </div>
          <div>
            <label class="block text-sm text-slate-300 mb-1">Student label</label>
            <input
              type="text"
              [formField]="issueForm.studentLabel"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            />
          </div>
          <div>
            <label class="block text-sm text-slate-300 mb-1">Subject wallet (0x...)</label>
            <input
              type="text"
              [formField]="issueForm.subjectWallet"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            />
          </div>
          <div>
            <label class="block text-sm text-slate-300 mb-1">Subject DID</label>
            <input
              type="text"
              [formField]="issueForm.subjectDid"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            />
          </div>
          <div>
            <label class="block text-sm text-slate-300 mb-1">Issuer DID</label>
            <input
              type="text"
              [formField]="issueForm.issuerDid"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            />
          </div>
          <div>
            <label class="block text-sm text-slate-300 mb-1">Document type</label>
            <select
              [formField]="issueForm.documentType"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            >
              @for (type of documentTypes; track type.code) {
                <option [value]="type.code">{{ type.label }}</option>
              }
            </select>
          </div>
          <div>
            <label class="block text-sm text-slate-300 mb-1">Issued date</label>
            <input
              type="date"
              [formField]="issueForm.issuedDate"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white"
            />
          </div>
          <div class="flex gap-3 pt-2">
            <button
              type="button"
              class="flex-1 px-4 py-2.5 text-sm text-slate-300 bg-slate-700 rounded-lg"
              (click)="closeIssueForm()"
            >
              Cancel
            </button>
            <button
              type="submit"
              class="flex-1 px-4 py-2.5 text-sm font-semibold text-white bg-blue-600 rounded-lg disabled:opacity-50"
              [disabled]="issueForm().invalid() || isSubmitting()"
            >
              {{ isSubmitting() ? 'Processing...' : 'Issue on blockchain' }}
            </button>
          </div>
        </form>
      </app-modal>
    </div>
  `,
})
export class IssuerComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly credentialService = inject(CredentialService);

  readonly isFormOpen = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly institutionIdInput = signal(this.credentialService.institutionId());

  readonly documentTypes = DOCUMENT_TYPE_OPTIONS;

  readonly issueModel = signal<IssueCredentialModel>({ ...EMPTY_ISSUE_MODEL });

  readonly issueForm = form(this.issueModel, (fields) => {
    required(fields.institutionId);
    required(fields.studentId);
    required(fields.studentLabel);
    required(fields.documentType);
    required(fields.issuedDate);
    required(fields.subjectWallet);
    required(fields.subjectDid);
    required(fields.issuerDid);
  });

  onInstitutionInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.institutionIdInput.set(target.value);
  }

  applyInstitutionId(): void {
    this.errorMessage.set(null);
    this.credentialService.setInstitutionId(this.institutionIdInput().trim());
  }

  openIssueForm(): void {
    this.errorMessage.set(null);
    this.issueModel.set({
      ...EMPTY_ISSUE_MODEL,
      institutionId: this.credentialService.institutionId(),
      issuedDate: new Date().toISOString().slice(0, 10),
    });
    this.isFormOpen.set(true);
  }

  closeIssueForm(): void {
    this.isFormOpen.set(false);
  }

  async handleIssueSubmit(event: Event): Promise<void> {
    event.preventDefault();
    this.errorMessage.set(null);

    await submit(this.issueForm, async () => {
      this.isSubmitting.set(true);
      try {
        await this.credentialService.issueCredential(this.issueModel());
        this.closeIssueForm();
      } catch (error: unknown) {
        this.errorMessage.set(toHttpErrorMessage(error, 'Failed to issue credential.'));
      } finally {
        this.isSubmitting.set(false);
      }
    });
  }

  async handleRevoke(credentialId: string): Promise<void> {
    const reason = window.prompt('Revocation reason:');
    if (!reason?.trim()) {
      return;
    }

    this.errorMessage.set(null);
    this.isSubmitting.set(true);
    try {
      await this.credentialService.revokeCredential(credentialId, reason.trim());
    } catch (error: unknown) {
      this.errorMessage.set(toHttpErrorMessage(error, 'Failed to revoke credential.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  handleLogout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
