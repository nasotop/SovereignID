import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import {
  CareerSummary,
  InstitutionSummary,
  StudentSummary,
} from '../../../core/models/academy.models';
import {
  IssuedCredential,
} from '../../../core/models/credential.models';
import { CredentialService } from '../../../core/services/credential.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { CopyValueComponent } from '../../../shared/ui/copy-value/copy-value.component';
import { HexLoaderComponent } from '../../../shared/ui/hex-loader/hex-loader.component';
import { ModalComponent } from '../../../shared/ui/modal/modal.component';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge/status-badge.component';

type CredentialFilter = 'all' | 'active' | 'revoked' | 'expired';

@Component({
  selector: 'app-issuer-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    HexLoaderComponent,
    ModalComponent,
    StatusBadgeComponent,
    CopyValueComponent,
  ],
  template: `
    @if (errorMessage()) {
      <div class="mb-4 rounded-lg border border-red-700 bg-red-900/40 p-4 text-sm text-red-100">
        {{ errorMessage() }}
      </div>
    }
    @if (successMessage()) {
      <div class="mb-4 rounded-lg border border-emerald-500/40 bg-emerald-500/10 p-4 text-sm text-emerald-100">
        {{ successMessage() }}
      </div>
    }

    <section
      class="shrink-0"
      [ngClass]="showHeader()
        ? 'mb-6 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between'
        : 'mb-4 flex justify-end'"
    >
      @if (showHeader()) {
        <div class="min-w-0">
          <p class="text-xs font-medium uppercase text-blue-300">{{ 'issuer.headerTitle' | translate }}</p>
          <h3 class="mt-1 text-2xl font-bold text-white">{{ 'issuer.title' | translate }}</h3>
          <p class="text-sm text-slate-400">
            {{ 'issuer.subtitle' | translate }}
          </p>
        </div>
      }

      <div class="flex flex-wrap justify-end gap-3">
        <button
          type="button"
          class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600 disabled:opacity-50"
          [disabled]="isBusy() || isCredentialsLoading() || !institutionId()"
          (click)="refreshCredentials()"
        >
          {{ isBusy() || isCredentialsLoading() ? ('common.loading' | translate) : ('common.refresh' | translate) }}
        </button>
        @if (canIssue()) {
          <button
            type="button"
            class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50"
            [disabled]="!canOpenIssueModal()"
            (click)="openIssueModal()"
          >
            {{ 'issuer.issue' | translate }}
          </button>
        }
      </div>
    </section>

    <section class="grid min-h-0 flex-1 gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(360px,440px)]">
      <section class="flex min-h-0 flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-800">
        <div class="border-b border-slate-700 p-4">
          <div class="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h3 class="text-base font-semibold text-white">{{ 'issuer.issuedList' | translate }}</h3>
              <p class="text-xs text-slate-400">{{ 'issuer.results' | translate: { count: filteredCredentials().length } }}</p>
            </div>
            <div class="grid gap-3 sm:grid-cols-[minmax(0,1fr)_10rem] lg:w-[32rem]">
              <input
                class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white outline-none focus:border-blue-500"
                [placeholder]="'issuer.search' | translate"
                [ngModel]="searchTerm()"
                (ngModelChange)="searchTerm.set($event)"
              />
              <select
                class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white outline-none focus:border-blue-500"
                [ngModel]="statusFilter()"
                (ngModelChange)="setStatusFilter($event)"
              >
                <option value="all">{{ 'common.all' | translate }}</option>
                <option value="active">{{ 'common.status.active' | translate }}</option>
                <option value="revoked">{{ 'common.status.revoked' | translate }}</option>
                <option value="expired">{{ 'common.status.expired' | translate }}</option>
              </select>
            </div>
          </div>
        </div>

        <div class="min-h-0 flex-1 overflow-auto">
          <table class="w-full min-w-[820px] text-left">
            <thead class="sticky top-0 bg-slate-800 text-xs uppercase text-slate-400">
              <tr class="border-b border-slate-700">
                <th class="px-4 py-3">{{ 'issuer.student' | translate }}</th>
                <th class="px-4 py-3">{{ 'issuer.career' | translate }}</th>
                <th class="px-4 py-3">{{ 'issuer.type' | translate }}</th>
                <th class="px-4 py-3">{{ 'issuer.issued' | translate }}</th>
                <th class="px-4 py-3">{{ 'common.status.active' | translate }}</th>
                <th class="px-4 py-3">{{ 'issuer.credentialId' | translate }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-700/70">
              @for (credential of filteredCredentials(); track credential.credentialId) {
                <tr
                  class="cursor-pointer transition hover:bg-slate-700/40"
                  [class.bg-slate-700]="selectedCredential()?.credentialId === credential.credentialId"
                  (click)="selectCredential(credential)"
                >
                  <td class="px-4 py-4">
                    <p class="text-sm font-semibold text-white">{{ credential.studentLabel }}</p>
                    <p class="font-mono text-xs text-slate-500">{{ credential.studentId }}</p>
                  </td>
                  <td class="px-4 py-4">
                    <p class="text-sm text-slate-200">{{ careerNameForCredential(credential) }}</p>
                    <p class="font-mono text-xs text-slate-500">{{ credential.careerId || '-' }}</p>
                  </td>
                  <td class="px-4 py-4 text-sm text-slate-200">{{ credential.documentType }}</td>
                  <td class="px-4 py-4 text-sm text-slate-400">{{ credential.issuedDate }}</td>
                  <td class="px-4 py-4">
                    <app-status-badge
                      [label]="statusLabel(credential.status)"
                      [tone]="statusTone(credential.status)"
                    />
                  </td>
                  <td class="px-4 py-4 font-mono text-xs text-slate-500">{{ credential.credentialId }}</td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="6" class="px-4 py-12 text-center text-sm text-slate-400">
                    @if (isCredentialsLoading()) {
                      <div class="flex flex-col items-center justify-center gap-4">
                        <app-hex-loader [label]="'common.loading' | translate" />
                        <span>{{ 'common.loading' | translate }}</span>
                      </div>
                    } @else {
                      {{ 'issuer.issuedList' | translate }}: 0
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </section>

      <aside class="min-h-0 overflow-auto rounded-lg border border-slate-700 bg-slate-800 p-6">
        @if (selectedCredential()) {
          <div class="space-y-6">
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0">
                <p class="text-xs font-medium uppercase text-blue-300">{{ 'issuer.selectedCredential' | translate }}</p>
                <h3 class="mt-1 truncate text-xl font-semibold text-white">
                  {{ selectedCredential()!.documentType }}
                </h3>
              </div>
              <button
                type="button"
                class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                [attr.aria-label]="'common.close' | translate"
                (click)="selectedCredential.set(null)"
              >
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <dl class="space-y-4 text-sm">
              <div>
                <dt class="text-slate-500">{{ 'issuer.credentialId' | translate }}</dt>
                <dd><app-copy-value [value]="selectedCredential()!.credentialId" /></dd>
              </div>
              <div>
                <dt class="text-slate-500">{{ 'issuer.student' | translate }}</dt>
                <dd class="text-slate-200">{{ selectedCredential()!.studentLabel }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">{{ 'issuer.career' | translate }}</dt>
                <dd class="text-slate-200">{{ careerNameForCredential(selectedCredential()!) }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">{{ 'common.status.active' | translate }}</dt>
                <dd class="mt-1">
                  <app-status-badge
                    [label]="statusLabel(selectedCredential()!.status)"
                    [tone]="statusTone(selectedCredential()!.status)"
                  />
                </dd>
              </div>
              <div>
                <dt class="text-slate-500">IPFS</dt>
                <dd>
                  @if (selectedCredential()!.ipfsGatewayUrl) {
                    <a
                      class="break-all text-blue-300 hover:text-blue-200"
                      [href]="selectedCredential()!.ipfsGatewayUrl"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {{ selectedCredential()!.ipfsGatewayUrl }}
                    </a>
                  } @else {
                    <span class="text-slate-400">-</span>
                  }
                </dd>
              </div>
            </dl>

            @if (canRevoke() && selectedCredential()!.status === 'active') {
              <button
                type="button"
                class="w-full rounded-lg border border-red-500/40 px-4 py-2.5 text-sm font-semibold text-red-300 hover:bg-red-500/10 disabled:opacity-50"
                [disabled]="isBusy()"
                (click)="openRevokeModal(selectedCredential()!)"
              >
                {{ 'issuer.revokeTitle' | translate }}
              </button>
            }
          </div>
        } @else {
          <div class="space-y-6">
            <div>
              <p class="text-xs font-medium uppercase text-blue-300">{{ 'issuer.summary' | translate }}</p>
              <h3 class="mt-1 text-xl font-semibold text-white">{{ 'issuer.overview' | translate }}</h3>
              <p class="mt-1 text-sm text-slate-400">
                {{ 'issuer.selectedCredential' | translate }}
              </p>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-xs text-slate-500">{{ 'common.total' | translate }}</p>
                <p class="mt-2 text-2xl font-bold text-white">{{ credentials().length }}</p>
              </article>
              <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-xs text-slate-500">{{ 'common.status.active' | translate }}</p>
                <p class="mt-2 text-2xl font-bold text-emerald-300">{{ activeCount() }}</p>
              </article>
              <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-xs text-slate-500">{{ 'common.status.revoked' | translate }}</p>
                <p class="mt-2 text-2xl font-bold text-red-300">{{ revokedCount() }}</p>
              </article>
              <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-xs text-slate-500">{{ 'issuer.eligibleStudents' | translate }}</p>
                <p class="mt-2 text-2xl font-bold text-blue-300">{{ eligibleStudents().length }}</p>
              </article>
            </div>

            <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
              <p class="text-sm font-semibold text-white">{{ 'issuer.issuerDid' | translate }}</p>
              <dl class="mt-3 space-y-3 text-sm">
                <div>
                  <dt class="text-slate-500">{{ 'issuer.issuerWallet' | translate }}</dt>
                  <dd><app-copy-value [value]="institution()?.issuerWalletAddress" [emptyLabel]="'issuer.noIssuerWallet' | translate" /></dd>
                </div>
                <div>
                  <dt class="text-slate-500">DID</dt>
                  <dd><app-copy-value [value]="institution()?.did" emptyLabel="-" /></dd>
                </div>
              </dl>
              @if (!canOpenIssueModal()) {
                <p class="mt-4 rounded-lg border border-amber-500/30 bg-amber-500/10 p-3 text-xs text-amber-100">
                  {{ 'issuer.noIssuerWallet' | translate }}
                </p>
              }
            </div>
          </div>
        }
      </aside>
    </section>

    <app-modal
      [isOpen]="issueModalOpen()"
      [title]="'issuer.issueModalTitle' | translate"
      [description]="'issuer.issueModalSubtitle' | translate"
      size="lg"
      (closed)="closeIssueModal()"
    >
      <form class="grid gap-4" (submit)="handleIssueSubmit($event)">
        <label class="grid gap-1 text-sm text-slate-200">
          {{ 'issuer.student' | translate }}
          <select
            class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
            [ngModel]="issueStudentId()"
            name="issueStudentId"
            (ngModelChange)="onIssueStudentChange($event)"
            required
          >
            <option value="">{{ 'issuer.selectStudent' | translate }}</option>
            @for (student of eligibleStudents(); track student.id) {
              <option [value]="student.id">
                {{ student.externalReference || student.id }} - {{ student.primaryWalletAddress }}
              </option>
            }
          </select>
        </label>

        <div class="grid gap-4 sm:grid-cols-2">
          <label class="grid gap-1 text-sm text-slate-200">
            {{ 'issuer.type' | translate }}
            <select
              class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
              [ngModel]="issueDocumentType()"
              name="issueDocumentType"
              (ngModelChange)="issueDocumentType.set($event)"
            >
              @if (isCredentialTypesLoading()) {
                <option value="" disabled>{{ 'issuer.credentialTypesLoading' | translate }}</option>
              } @else if (!credentialTypes().length) {
                <option value="" disabled>{{ 'issuer.noCredentialTypes' | translate }}</option>
              }
              @for (type of credentialTypes(); track type.code) {
                <option [value]="type.code">{{ type.name }}</option>
              }
            </select>
          </label>

          <label class="grid gap-1 text-sm text-slate-200">
            {{ 'issuer.issueDate' | translate }}
            <input
              type="date"
              class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
              [ngModel]="issueDate()"
              name="issueDate"
              (ngModelChange)="issueDate.set($event)"
              required
            />
          </label>
        </div>

        <label class="grid gap-1 text-sm text-slate-200">
          {{ 'issuer.career' | translate }}
          <select
            class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
            [ngModel]="issueCareerId()"
            name="issueCareerId"
            (ngModelChange)="issueCareerId.set($event)"
            required
          >
            <option value="">{{ 'issuer.selectCareer' | translate }}</option>
            @for (career of activeCareers(); track career.id) {
              <option [value]="career.id">{{ career.code }} - {{ career.name }}</option>
            }
          </select>
        </label>

        @if (selectedIssueStudent()) {
          <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4 text-sm">
            <p class="font-semibold text-white">{{ 'issuer.derivedData' | translate }}</p>
            <dl class="mt-3 grid gap-3 sm:grid-cols-2">
              <div>
                <dt class="text-slate-500">{{ 'issuer.studentWallet' | translate }}</dt>
                <dd class="break-all font-mono text-xs text-slate-200">{{ selectedIssueStudent()!.primaryWalletAddress }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">{{ 'issuer.studentDid' | translate }}</dt>
                <dd class="break-all font-mono text-xs text-slate-200">{{ selectedIssueStudent()!.primaryWalletDid }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">{{ 'issuer.issuerDid' | translate }}</dt>
                <dd class="break-all font-mono text-xs text-slate-200">{{ issuerDid() }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">{{ 'issuer.selectedCareer' | translate }}</dt>
                <dd class="text-slate-200">{{ selectedIssueCareer()?.name || '-' }}</dd>
              </div>
            </dl>
          </div>
        }

        <div class="flex justify-end gap-3 pt-2">
          <button
            type="button"
            class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
            (click)="closeIssueModal()"
          >
            {{ 'common.cancel' | translate }}
          </button>
          <button
            type="submit"
            class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50"
            [disabled]="isBusy() || !canSubmitIssue()"
          >
            {{ isBusy() ? ('issuer.issuing' | translate) : ('issuer.issue' | translate) }}
          </button>
        </div>
      </form>
    </app-modal>

    <app-modal
      [isOpen]="revokeModalOpen()"
      [title]="'issuer.revokeTitle' | translate"
      description="La revocacion quedara registrada en blockchain y en el servicio issuer."
      (closed)="closeRevokeModal()"
    >
      <form class="grid gap-4" (submit)="handleRevokeSubmit($event)">
        <label class="grid gap-1 text-sm text-slate-200">
          {{ 'issuer.revokeReason' | translate }}
          <textarea
            class="min-h-28 rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
            [ngModel]="revokeReason()"
            name="revokeReason"
            (ngModelChange)="revokeReason.set($event)"
            required
          ></textarea>
        </label>
        <div class="flex justify-end gap-3">
          <button
            type="button"
            class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
            (click)="closeRevokeModal()"
          >
            {{ 'common.cancel' | translate }}
          </button>
          <button
            type="submit"
            class="rounded-lg border border-red-500/40 px-4 py-2.5 text-sm font-semibold text-red-300 hover:bg-red-500/10 disabled:opacity-50"
            [disabled]="isBusy() || !revokeReason().trim()"
          >
            {{ isBusy() ? 'Revocando...' : 'Confirmar revocacion' }}
          </button>
        </div>
      </form>
    </app-modal>
  `,
})
export class IssuerTabComponent {
  readonly institutionId = input.required<string>();
  readonly institution = input<InstitutionSummary | null>(null);
  readonly students = input<readonly StudentSummary[]>([]);
  readonly careers = input<readonly CareerSummary[]>([]);
  readonly canIssue = input(true);
  readonly canRevoke = input(true);
  readonly showHeader = input(true);

  private readonly credentialService = inject(CredentialService);
  private readonly translate = inject(TranslateService);

  readonly selectedCredential = signal<IssuedCredential | null>(null);
  readonly searchTerm = signal('');
  readonly statusFilter = signal<CredentialFilter>('all');
  readonly isBusy = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly issueModalOpen = signal(false);
  readonly issueStudentId = signal('');
  readonly issueCareerId = signal('');
  readonly issueDocumentType = signal('');
  readonly issueDate = signal(new Date().toISOString().slice(0, 10));
  readonly selectedIssueStudent = signal<StudentSummary | null>(null);

  readonly revokeModalOpen = signal(false);
  readonly revokeCredential = signal<IssuedCredential | null>(null);
  readonly revokeReason = signal('');

  readonly credentials = computed(() => this.credentialService.credentials());
  readonly credentialTypes = computed(() => this.credentialService.credentialTypes());
  readonly activeCount = computed(() => this.credentialService.activeCount());
  readonly revokedCount = computed(() => this.credentialService.revokedCount());

  readonly eligibleStudents = computed(() =>
    this.students().filter((student) =>
      student.isActive
      && Boolean(student.primaryWalletAddress)
      && Boolean(student.primaryWalletDid)),
  );

  readonly activeCareers = computed(() =>
    this.careers().filter((career) => career.isActive),
  );

  readonly selectedIssueCareer = computed(() =>
    this.activeCareers().find((career) => career.id === this.issueCareerId()) ?? null,
  );

  readonly selectedCredentialType = computed(() =>
    this.credentialTypes().find((type) => type.code === this.issueDocumentType()) ?? null,
  );

  readonly filteredCredentials = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const filter = this.statusFilter();

    return this.credentials().filter((credential) => {
      const matchesStatus = filter === 'all' || credential.status === filter;
      const matchesTerm = !term
        || credential.studentLabel.toLowerCase().includes(term)
        || credential.documentType.toLowerCase().includes(term)
        || this.careerNameForCredential(credential).toLowerCase().includes(term)
        || credential.credentialId.toLowerCase().includes(term)
        || credential.studentId.toLowerCase().includes(term);

      return matchesStatus && matchesTerm;
    });
  });

  readonly issuerDid = computed(() =>
    this.institution()?.did
    ?? (this.institution()?.issuerWalletAddress
      ? `did:ethr:sepolia:${this.institution()!.issuerWalletAddress!.toLowerCase()}`
      : ''),
  );

  constructor() {
    effect(() => {
      const institutionId = this.institutionId();
      if (institutionId && institutionId !== this.credentialService.institutionId()) {
        this.credentialService.setInstitutionId(institutionId);
      }
    });

    effect(() => {
      const selectedType = this.issueDocumentType();
      const defaultType = this.credentialTypes()[0]?.code ?? '';
      if (!selectedType && defaultType) {
        this.issueDocumentType.set(defaultType);
      }
    });
  }

  refreshCredentials(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.credentialService.credentialsResource.reload();
  }

  isCredentialsLoading(): boolean {
    return this.credentialService.credentialsResource.isLoading();
  }

  isCredentialTypesLoading(): boolean {
    return this.credentialService.credentialTypesResource.isLoading();
  }

  selectCredential(credential: IssuedCredential): void {
    if (this.selectedCredential()?.credentialId === credential.credentialId) {
      this.selectedCredential.set(null);
      return;
    }

    this.selectedCredential.set(credential);
  }

  setStatusFilter(value: string): void {
    this.statusFilter.set(
      ['active', 'revoked', 'expired'].includes(value)
        ? value as CredentialFilter
        : 'all',
    );
  }

  canOpenIssueModal(): boolean {
    return Boolean(this.institutionId())
      && Boolean(this.issuerDid())
      && Boolean(this.institution()?.issuerWalletAddress)
      && this.eligibleStudents().length > 0
      && this.activeCareers().length > 0
      && this.credentialTypes().length > 0;
  }

  openIssueModal(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.issueDate.set(new Date().toISOString().slice(0, 10));
    this.issueDocumentType.set(this.credentialTypes()[0]?.code ?? '');
    this.issueCareerId.set(this.activeCareers()[0]?.id ?? '');
    const firstStudent = this.eligibleStudents()[0] ?? null;
    this.selectedIssueStudent.set(firstStudent);
    this.issueStudentId.set(firstStudent?.id ?? '');
    this.issueModalOpen.set(true);
  }

  closeIssueModal(): void {
    if (!this.isBusy()) {
      this.issueModalOpen.set(false);
    }
  }

  onIssueStudentChange(studentId: string): void {
    this.issueStudentId.set(studentId);
    this.selectedIssueStudent.set(
      this.eligibleStudents().find((student) => student.id === studentId) ?? null,
    );
  }

  canSubmitIssue(): boolean {
    const student = this.selectedIssueStudent();
    return Boolean(this.institutionId())
      && Boolean(student?.primaryWalletAddress)
      && Boolean(student?.primaryWalletDid)
      && Boolean(this.issuerDid())
      && Boolean(this.selectedIssueCareer())
      && Boolean(this.selectedCredentialType())
      && Boolean(this.issueDate());
  }

  async handleIssueSubmit(event: Event): Promise<void> {
    event.preventDefault();
    const student = this.selectedIssueStudent();
    const career = this.selectedIssueCareer();
    if (!student || !career || !this.canSubmitIssue()) {
      return;
    }

    this.isBusy.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      await this.credentialService.issueCredential({
        institutionId: this.institutionId(),
        studentId: student.id,
        careerId: career.id,
        careerName: career.name,
        studentLabel: student.externalReference || student.id,
        documentType: this.issueDocumentType(),
        issuedDate: this.issueDate(),
        subjectWallet: student.primaryWalletAddress!,
        subjectDid: student.primaryWalletDid!,
        issuerDid: this.issuerDid(),
      });
      this.issueModalOpen.set(false);
      this.successMessage.set(this.translate.instant('issuer.issuedSuccess', {
        student: student.externalReference || student.id,
        career: career.name,
      }));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.isBusy.set(false);
    }
  }

  openRevokeModal(credential: IssuedCredential): void {
    this.revokeCredential.set(credential);
    this.revokeReason.set('');
    this.revokeModalOpen.set(true);
  }

  closeRevokeModal(): void {
    if (!this.isBusy()) {
      this.revokeModalOpen.set(false);
    }
  }

  async handleRevokeSubmit(event: Event): Promise<void> {
    event.preventDefault();
    const credential = this.revokeCredential();
    const reason = this.revokeReason().trim();
    if (!credential || !reason) {
      return;
    }

    this.isBusy.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      await this.credentialService.revokeCredential(credential.credentialId, reason);
      this.revokeModalOpen.set(false);
      this.selectedCredential.set(null);
      this.successMessage.set(this.translate.instant('issuer.revoked'));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.isBusy.set(false);
    }
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'active':
        return this.translate.instant('common.status.active');
      case 'revoked':
        return this.translate.instant('common.status.revoked');
      case 'expired':
        return this.translate.instant('common.status.expired');
      default:
        return status;
    }
  }

  careerNameForCredential(credential: IssuedCredential): string {
    if (!credential.careerId) {
      return this.translate.instant('issuer.noCareer');
    }

    return this.careers().find((career) => career.id === credential.careerId)?.name
      ?? credential.careerId;
  }

  statusTone(status: string): 'success' | 'danger' | 'warning' | 'info' {
    switch (status) {
      case 'active':
        return 'success';
      case 'revoked':
        return 'danger';
      case 'expired':
        return 'warning';
      default:
        return 'info';
    }
  }
}
