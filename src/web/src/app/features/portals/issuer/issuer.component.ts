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

const EMPTY_ISSUE_MODEL: IssueCredentialModel = {
  student: '',
  documentType: DOCUMENT_TYPE_OPTIONS[0],
  issuedDate: '',
};

@Component({
  selector: 'app-issuer',
  imports: [ModalComponent, FormField],
  template: `
    <div class="min-h-screen bg-slate-900">
      <!-- Navbar -->
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
              <p class="text-xs text-slate-400">Issuer Portal</p>
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

      <!-- Main Content -->
      <main class="max-w-7xl mx-auto px-6 py-8">
        <div class="mb-8">
          <h2 class="text-2xl font-bold text-white">Credential Management</h2>
          <p class="text-slate-400 mt-1">
            Issue, monitor and revoke verifiable digital credentials
          </p>
        </div>

        <!-- Metrics -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
          <div
            class="bg-slate-800 border border-slate-700 rounded-xl p-5 flex items-center gap-4"
          >
            <div
              class="w-12 h-12 rounded-lg bg-blue-600/20 flex items-center justify-center"
            >
              <svg
                class="w-6 h-6 text-blue-400"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                aria-hidden="true"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
            </div>
            <div>
              <p class="text-sm text-slate-400">Total Issued</p>
              <p class="text-2xl font-bold text-white">
                {{ credentialService.totalIssued() }}
              </p>
            </div>
          </div>

          <div
            class="bg-slate-800 border border-slate-700 rounded-xl p-5 flex items-center gap-4"
          >
            <div
              class="w-12 h-12 rounded-lg bg-emerald-600/20 flex items-center justify-center"
            >
              <svg
                class="w-6 h-6 text-emerald-400"
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
            </div>
            <div>
              <p class="text-sm text-slate-400">Active</p>
              <p class="text-2xl font-bold text-emerald-400">
                {{ credentialService.activeCount() }}
              </p>
            </div>
          </div>

          <div
            class="bg-slate-800 border border-slate-700 rounded-xl p-5 flex items-center gap-4"
          >
            <div
              class="w-12 h-12 rounded-lg bg-red-600/20 flex items-center justify-center"
            >
              <svg
                class="w-6 h-6 text-red-400"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                aria-hidden="true"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"
                />
              </svg>
            </div>
            <div>
              <p class="text-sm text-slate-400">Revoked</p>
              <p class="text-2xl font-bold text-red-400">
                {{ credentialService.revokedCount() }}
              </p>
            </div>
          </div>
        </div>

        <!-- Data Table -->
        <div
          class="bg-slate-800 border border-slate-700 rounded-xl overflow-hidden"
        >
          <div
            class="px-6 py-4 border-b border-slate-700 flex items-center justify-between"
          >
            <h3 class="text-lg font-semibold text-white">Issued Credentials</h3>
            <button
              type="button"
              class="flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 rounded-lg transition-colors"
              (click)="openIssueForm()"
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
                  d="M12 4v16m8-8H4"
                />
              </svg>
              Issue New Credential
            </button>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left">
              <thead>
                <tr
                  class="border-b border-slate-700 text-xs uppercase tracking-wider text-slate-400"
                >
                  <th class="px-6 py-3 font-medium">Student</th>
                  <th class="px-6 py-3 font-medium">Document Type</th>
                  <th class="px-6 py-3 font-medium">Issued Date</th>
                  <th class="px-6 py-3 font-medium">Status</th>
                  <th class="px-6 py-3 font-medium text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-700/60">
                @for (
                  credential of credentialService.credentials();
                  track credential.id
                ) {
                  <tr class="hover:bg-slate-700/30 transition-colors">
                    <td class="px-6 py-4">
                      <div class="flex items-center gap-3">
                        <div
                          class="w-8 h-8 rounded-full bg-slate-600 flex items-center justify-center text-xs font-bold text-slate-200"
                        >
                          {{ credential.student.charAt(0) }}
                        </div>
                        <span class="text-sm font-medium text-white">
                          {{ credential.student }}
                        </span>
                      </div>
                    </td>
                    <td class="px-6 py-4 text-sm text-slate-300">
                      {{ credential.documentType }}
                    </td>
                    <td class="px-6 py-4 text-sm text-slate-400">
                      {{ credential.issuedDate }}
                    </td>
                    <td class="px-6 py-4">
                      @if (credential.status === 'active') {
                        <span
                          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-500/15 text-emerald-400 border border-emerald-500/30"
                        >
                          <span
                            class="w-1.5 h-1.5 rounded-full bg-emerald-400"
                          ></span>
                          Active
                        </span>
                      } @else {
                        <span
                          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-red-500/15 text-red-400 border border-red-500/30"
                        >
                          <span
                            class="w-1.5 h-1.5 rounded-full bg-red-400"
                          ></span>
                          Revoked
                        </span>
                      }
                    </td>
                    <td class="px-6 py-4 text-right">
                      <button
                        type="button"
                        class="text-sm text-slate-400 hover:text-white transition-colors"
                      >
                        View
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </main>

      <!-- Issue Credential Modal (Signal Forms skeleton) -->
      <app-modal
        [isOpen]="isFormOpen()"
        title="Issue New Credential"
        (closed)="closeIssueForm()"
      >
        <form (submit)="handleIssueSubmit($event)" class="space-y-5">
          <div>
            <label
              for="student"
              class="block text-sm font-medium text-slate-300 mb-1.5"
            >
              Student Name
            </label>
            <input
              id="student"
              type="text"
              [formField]="issueForm.student"
              placeholder="e.g. María González"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            @if (issueForm.student().touched() && issueForm.student().invalid()) {
              <p class="mt-1 text-xs text-red-400">Student name is required</p>
            }
          </div>

          <div>
            <label
              for="documentType"
              class="block text-sm font-medium text-slate-300 mb-1.5"
            >
              Document Type
            </label>
            <select
              id="documentType"
              [formField]="issueForm.documentType"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              @for (type of documentTypes; track type) {
                <option [value]="type">{{ type }}</option>
              }
            </select>
            @if (
              issueForm.documentType().touched() &&
              issueForm.documentType().invalid()
            ) {
              <p class="mt-1 text-xs text-red-400">Document type is required</p>
            }
          </div>

          <div>
            <label
              for="issuedDate"
              class="block text-sm font-medium text-slate-300 mb-1.5"
            >
              Issued Date
            </label>
            <input
              id="issuedDate"
              type="date"
              [formField]="issueForm.issuedDate"
              class="w-full px-3 py-2.5 bg-slate-900 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            @if (
              issueForm.issuedDate().touched() && issueForm.issuedDate().invalid()
            ) {
              <p class="mt-1 text-xs text-red-400">Issued date is required</p>
            }
          </div>

          <div class="flex gap-3 pt-2">
            <button
              type="button"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-slate-300 bg-slate-700 hover:bg-slate-600 border border-slate-600 rounded-lg transition-colors"
              (click)="closeIssueForm()"
            >
              Cancel
            </button>
            <button
              type="submit"
              class="flex-1 px-4 py-2.5 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              [disabled]="issueForm().invalid()"
            >
              Issue Credential
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

  readonly isFormOpen = signal<boolean>(false);

  readonly documentTypes = DOCUMENT_TYPE_OPTIONS;

  readonly issueModel = signal<IssueCredentialModel>({ ...EMPTY_ISSUE_MODEL });

  readonly issueForm = form(this.issueModel, (fields) => {
    required(fields.student);
    required(fields.documentType);
    required(fields.issuedDate);
  });

  openIssueForm(): void {
    this.issueModel.set({ ...EMPTY_ISSUE_MODEL });
    this.isFormOpen.set(true);
  }

  closeIssueForm(): void {
    this.isFormOpen.set(false);
  }

  async handleIssueSubmit(event: Event): Promise<void> {
    event.preventDefault();

    await submit(this.issueForm, async () => {
      // Future: POST via CredentialService HTTP endpoint
      this.credentialService.addCredential(this.issueModel());
      this.closeIssueForm();
    });
  }

  handleLogout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
