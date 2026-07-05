import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  InstitutionSummary,
  StudentSummary,
} from '../../../core/models/academy.models';
import {
  DOCUMENT_TYPE_OPTIONS,
  DocumentTypeOption,
  IssuedCredential,
} from '../../../core/models/credential.models';
import { CredentialService } from '../../../core/services/credential.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { CopyValueComponent } from '../../../shared/ui/copy-value/copy-value.component';
import { ModalComponent } from '../../../shared/ui/modal/modal.component';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge/status-badge.component';

type CredentialFilter = 'all' | 'active' | 'revoked' | 'expired';

@Component({
  selector: 'app-issuer-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
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
          <p class="text-xs font-medium uppercase text-blue-300">Modulo issuer</p>
          <h3 class="mt-1 text-2xl font-bold text-white">Emision de credenciales</h3>
          <p class="text-sm text-slate-400">
            Titulos y certificados verificables emitidos por la institucion activa.
          </p>
        </div>
      }

      <div class="flex flex-wrap justify-end gap-3">
        <button
          type="button"
          class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600 disabled:opacity-50"
          [disabled]="isBusy() || !institutionId()"
          (click)="refreshCredentials()"
        >
          {{ isBusy() ? 'Cargando...' : 'Refrescar' }}
        </button>
        @if (canIssue()) {
          <button
            type="button"
            class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50"
            [disabled]="!canOpenIssueModal()"
            (click)="openIssueModal()"
          >
            Emitir credencial
          </button>
        }
      </div>
    </section>

    <section class="grid min-h-0 flex-1 gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(360px,440px)]">
      <section class="flex min-h-0 flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-800">
        <div class="border-b border-slate-700 p-4">
          <div class="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h3 class="text-base font-semibold text-white">Credenciales emitidas</h3>
              <p class="text-xs text-slate-400">{{ filteredCredentials().length }} resultados</p>
            </div>
            <div class="grid gap-3 sm:grid-cols-[minmax(0,1fr)_10rem] lg:w-[32rem]">
              <input
                class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white outline-none focus:border-blue-500"
                placeholder="Buscar por estudiante, tipo o ID"
                [ngModel]="searchTerm()"
                (ngModelChange)="searchTerm.set($event)"
              />
              <select
                class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white outline-none focus:border-blue-500"
                [ngModel]="statusFilter()"
                (ngModelChange)="setStatusFilter($event)"
              >
                <option value="all">Todos</option>
                <option value="active">Activas</option>
                <option value="revoked">Revocadas</option>
                <option value="expired">Expiradas</option>
              </select>
            </div>
          </div>
        </div>

        <div class="min-h-0 flex-1 overflow-auto">
          <table class="w-full min-w-[820px] text-left">
            <thead class="sticky top-0 bg-slate-800 text-xs uppercase text-slate-400">
              <tr class="border-b border-slate-700">
                <th class="px-4 py-3">Estudiante</th>
                <th class="px-4 py-3">Tipo</th>
                <th class="px-4 py-3">Emitida</th>
                <th class="px-4 py-3">Estado</th>
                <th class="px-4 py-3">Credential ID</th>
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
                  <td colspan="5" class="px-4 py-12 text-center text-sm text-slate-400">
                    {{ isBusy() ? 'Cargando credenciales...' : 'No hay credenciales emitidas para este filtro.' }}
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
                <p class="text-xs font-medium uppercase text-blue-300">Credencial seleccionada</p>
                <h3 class="mt-1 truncate text-xl font-semibold text-white">
                  {{ selectedCredential()!.documentType }}
                </h3>
              </div>
              <button
                type="button"
                class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                aria-label="Cerrar detalle"
                (click)="selectedCredential.set(null)"
              >
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <dl class="space-y-4 text-sm">
              <div>
                <dt class="text-slate-500">Credential ID</dt>
                <dd><app-copy-value [value]="selectedCredential()!.credentialId" /></dd>
              </div>
              <div>
                <dt class="text-slate-500">Estudiante</dt>
                <dd class="text-slate-200">{{ selectedCredential()!.studentLabel }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">Estado</dt>
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
                Revocar credencial
              </button>
            }
          </div>
        } @else {
          <div class="space-y-6">
            <div>
              <p class="text-xs font-medium uppercase text-blue-300">Resumen issuer</p>
              <h3 class="mt-1 text-xl font-semibold text-white">Vista general</h3>
              <p class="mt-1 text-sm text-slate-400">
                Selecciona una credencial para ver detalle o revocarla.
              </p>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-xs text-slate-500">Total</p>
                <p class="mt-2 text-2xl font-bold text-white">{{ credentials().length }}</p>
              </article>
              <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-xs text-slate-500">Activas</p>
                <p class="mt-2 text-2xl font-bold text-emerald-300">{{ activeCount() }}</p>
              </article>
              <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-xs text-slate-500">Revocadas</p>
                <p class="mt-2 text-2xl font-bold text-red-300">{{ revokedCount() }}</p>
              </article>
              <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-xs text-slate-500">Estudiantes aptos</p>
                <p class="mt-2 text-2xl font-bold text-blue-300">{{ eligibleStudents().length }}</p>
              </article>
            </div>

            <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
              <p class="text-sm font-semibold text-white">Estado emisor</p>
              <dl class="mt-3 space-y-3 text-sm">
                <div>
                  <dt class="text-slate-500">Wallet emisora</dt>
                  <dd><app-copy-value [value]="institution()?.issuerWalletAddress" emptyLabel="Sin wallet emisora" /></dd>
                </div>
                <div>
                  <dt class="text-slate-500">DID institucion</dt>
                  <dd><app-copy-value [value]="institution()?.did" emptyLabel="Sin DID" /></dd>
                </div>
              </dl>
              @if (!canOpenIssueModal()) {
                <p class="mt-4 rounded-lg border border-amber-500/30 bg-amber-500/10 p-3 text-xs text-amber-100">
                  Para emitir, la institucion debe tener wallet/DID emisor y debe existir al menos un estudiante con wallet primaria.
                </p>
              }
            </div>
          </div>
        }
      </aside>
    </section>

    <app-modal
      [isOpen]="issueModalOpen()"
      title="Emitir credencial"
      description="Selecciona estudiante, tipo de documento y fecha de emision."
      size="lg"
      (closed)="closeIssueModal()"
    >
      <form class="grid gap-4" (submit)="handleIssueSubmit($event)">
        <label class="grid gap-1 text-sm text-slate-200">
          Estudiante
          <select
            class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
            [ngModel]="issueStudentId()"
            name="issueStudentId"
            (ngModelChange)="onIssueStudentChange($event)"
            required
          >
            <option value="">Seleccionar estudiante</option>
            @for (student of eligibleStudents(); track student.id) {
              <option [value]="student.id">
                {{ student.externalReference || student.id }} - {{ student.primaryWalletAddress }}
              </option>
            }
          </select>
        </label>

        <div class="grid gap-4 sm:grid-cols-2">
          <label class="grid gap-1 text-sm text-slate-200">
            Tipo
            <select
              class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
              [ngModel]="issueDocumentType()"
              name="issueDocumentType"
              (ngModelChange)="issueDocumentType.set($event)"
            >
              @for (type of documentTypes; track type.code) {
                <option [value]="type.code">{{ type.label }}</option>
              }
            </select>
          </label>

          <label class="grid gap-1 text-sm text-slate-200">
            Fecha emision
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
          Carrera ID opcional
          <input
            class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white"
            placeholder="UUID de carrera si aplica"
            [ngModel]="issueCareerId()"
            name="issueCareerId"
            (ngModelChange)="issueCareerId.set($event)"
          />
        </label>

        @if (selectedIssueStudent()) {
          <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4 text-sm">
            <p class="font-semibold text-white">Datos derivados</p>
            <dl class="mt-3 grid gap-3 sm:grid-cols-2">
              <div>
                <dt class="text-slate-500">Wallet estudiante</dt>
                <dd class="break-all font-mono text-xs text-slate-200">{{ selectedIssueStudent()!.primaryWalletAddress }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">DID estudiante</dt>
                <dd class="break-all font-mono text-xs text-slate-200">{{ selectedIssueStudent()!.primaryWalletDid }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">DID emisor</dt>
                <dd class="break-all font-mono text-xs text-slate-200">{{ issuerDid() }}</dd>
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
            Cancelar
          </button>
          <button
            type="submit"
            class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50"
            [disabled]="isBusy() || !canSubmitIssue()"
          >
            {{ isBusy() ? 'Emitiendo...' : 'Emitir credencial' }}
          </button>
        </div>
      </form>
    </app-modal>

    <app-modal
      [isOpen]="revokeModalOpen()"
      title="Revocar credencial"
      description="La revocacion quedara registrada en blockchain y en el servicio issuer."
      (closed)="closeRevokeModal()"
    >
      <form class="grid gap-4" (submit)="handleRevokeSubmit($event)">
        <label class="grid gap-1 text-sm text-slate-200">
          Motivo
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
            Cancelar
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
  readonly canIssue = input(true);
  readonly canRevoke = input(true);
  readonly showHeader = input(true);

  private readonly credentialService = inject(CredentialService);

  readonly documentTypes: readonly DocumentTypeOption[] = DOCUMENT_TYPE_OPTIONS;
  readonly selectedCredential = signal<IssuedCredential | null>(null);
  readonly searchTerm = signal('');
  readonly statusFilter = signal<CredentialFilter>('all');
  readonly isBusy = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly issueModalOpen = signal(false);
  readonly issueStudentId = signal('');
  readonly issueCareerId = signal('');
  readonly issueDocumentType = signal(DOCUMENT_TYPE_OPTIONS[0].code);
  readonly issueDate = signal(new Date().toISOString().slice(0, 10));
  readonly selectedIssueStudent = signal<StudentSummary | null>(null);

  readonly revokeModalOpen = signal(false);
  readonly revokeCredential = signal<IssuedCredential | null>(null);
  readonly revokeReason = signal('');

  readonly credentials = computed(() => this.credentialService.credentials());
  readonly activeCount = computed(() => this.credentialService.activeCount());
  readonly revokedCount = computed(() => this.credentialService.revokedCount());

  readonly eligibleStudents = computed(() =>
    this.students().filter((student) =>
      student.isActive
      && Boolean(student.primaryWalletAddress)
      && Boolean(student.primaryWalletDid)),
  );

  readonly filteredCredentials = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const filter = this.statusFilter();

    return this.credentials().filter((credential) => {
      const matchesStatus = filter === 'all' || credential.status === filter;
      const matchesTerm = !term
        || credential.studentLabel.toLowerCase().includes(term)
        || credential.documentType.toLowerCase().includes(term)
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
  }

  refreshCredentials(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.credentialService.credentialsResource.reload();
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
      && this.eligibleStudents().length > 0;
  }

  openIssueModal(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.issueDate.set(new Date().toISOString().slice(0, 10));
    this.issueDocumentType.set(DOCUMENT_TYPE_OPTIONS[0].code);
    this.issueCareerId.set('');
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
      && Boolean(this.issueDocumentType())
      && Boolean(this.issueDate());
  }

  async handleIssueSubmit(event: Event): Promise<void> {
    event.preventDefault();
    const student = this.selectedIssueStudent();
    if (!student || !this.canSubmitIssue()) {
      return;
    }

    this.isBusy.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      await this.credentialService.issueCredential({
        institutionId: this.institutionId(),
        studentId: student.id,
        careerId: this.issueCareerId().trim(),
        studentLabel: student.externalReference || student.id,
        documentType: this.issueDocumentType(),
        issuedDate: this.issueDate(),
        subjectWallet: student.primaryWalletAddress!,
        subjectDid: student.primaryWalletDid!,
        issuerDid: this.issuerDid(),
      });
      this.issueModalOpen.set(false);
      this.successMessage.set('Credencial emitida correctamente.');
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
      this.successMessage.set('Credencial revocada correctamente.');
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.isBusy.set(false);
    }
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'active':
        return 'Activa';
      case 'revoked':
        return 'Revocada';
      case 'expired':
        return 'Expirada';
      default:
        return status;
    }
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
