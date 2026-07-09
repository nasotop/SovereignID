import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { HolderCredentialDetail } from '../../../api/bff/models/holder-credential-detail';
import { HolderCredentialSummary } from '../../../api/bff/models/holder-credential-summary';
import { HolderInstitutionSummary, HolderProfile } from '../../../core/models/holder.models';
import {
  HolderService,
  HolderUnauthorizedError,
} from '../../../core/services/holder.service';
import { AuthService } from '../../../core/services/auth.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { ModalComponent } from '../../../shared/ui/modal/modal.component';
import { CredentialAnchorsPanelComponent } from '../../../shared/ui/credential-anchors';
import { PortalShellComponent } from '../../../shared/ui/portal-shell/portal-shell.component';

type HolderLoadState = 'loading' | 'loaded' | 'error';

interface AnchorsModalModel {
  open: boolean;
  state: 'idle' | 'loading' | 'loaded' | 'error';
  credentialId: string | null;
  detail: HolderCredentialDetail | null;
  error: string | null;
}

const CLOSED_ANCHORS_MODAL: AnchorsModalModel = {
  open: false,
  state: 'idle',
  credentialId: null,
  detail: null,
  error: null,
};

const STATUS_LABELS: Record<HolderCredentialSummary['status'], string> = {
  active: 'Activa',
  revoked: 'Revocada',
  expired: 'Expirada',
};

@Component({
  selector: 'app-holder',
  standalone: true,
  host: {
    class: 'block h-full',
  },
  imports: [CommonModule, CredentialAnchorsPanelComponent, ModalComponent, PortalShellComponent, ReactiveFormsModule],
  template: `
    <app-portal-shell
      portalLabel="Holder Portal"
      title="Mi identidad"
      subtitle="Perfil personal, instituciones vinculadas y credenciales verificables."
      accent="blue"
      layoutWidth="full"
      [userName]="displayName()"
      userRole="holder"
      (logout)="handleLogout()"
    >
      @if (loadState() === 'loading') {
        <section class="rounded-lg border border-slate-700 bg-slate-800 p-8 text-slate-300">
          Cargando informacion del holder...
        </section>
      }

      @if (loadState() === 'error') {
        <section class="rounded-lg border border-red-800/60 bg-red-950/30 p-8">
          <h3 class="text-lg font-semibold text-red-200">
            {{ isUnauthorized() ? 'Sesion no autorizada' : 'No se pudo cargar el portal' }}
          </h3>
          <p class="mt-2 text-sm text-slate-300">{{ errorMessage() }}</p>
          <button
            type="button"
            class="mt-4 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500"
            (click)="isUnauthorized() ? goToLogin() : loadHolder()"
          >
            {{ isUnauthorized() ? 'Ir al login' : 'Reintentar' }}
          </button>
        </section>
      }

      @if (loadState() === 'loaded' && profile()) {
        <div class="flex min-h-0 flex-1 flex-col overflow-hidden">
        @if (feedback()) {
          <p
            class="mb-4 shrink-0 rounded-lg border px-4 py-3 text-sm"
            [ngClass]="feedbackType() === 'success'
              ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-200'
              : 'border-red-500/30 bg-red-500/10 text-red-200'"
            role="status"
          >
            {{ feedback() }}
          </p>
        }

        <section class="grid min-h-0 flex-1 grid-cols-[minmax(0,1fr)_24rem] gap-6 overflow-hidden">
          <div class="flex min-h-0 flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-800">
            <div class="flex shrink-0 items-center justify-between border-b border-slate-700 px-5 py-4">
              <div>
                <h3 class="text-lg font-semibold text-white">Perfil holder</h3>
                <p class="text-sm text-slate-400">
                  Datos personales off-chain asociados a tu wallet.
                </p>
              </div>
              <button
                type="button"
                class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500"
                (click)="openProfileModal()"
              >
                Editar perfil
              </button>
            </div>

            <div class="min-h-0 flex-1 overflow-y-auto overscroll-y-contain">
            <div class="grid gap-5 p-5 lg:grid-cols-2">
              <article class="rounded-lg border border-slate-700 bg-slate-900/50 p-5">
                <p class="text-xs font-semibold uppercase text-blue-300">Identidad</p>
                <h4 class="mt-2 text-xl font-semibold text-white">
                  {{ displayName() || 'Holder sin nombre visible' }}
                </h4>
                <dl class="mt-5 space-y-4 text-sm">
                  <div>
                    <dt class="text-slate-500">Wallet</dt>
                    <dd class="mt-1 break-all font-mono text-slate-200">{{ profile()!.walletAddress }}</dd>
                  </div>
                  <div>
                    <dt class="text-slate-500">DID</dt>
                    <dd class="mt-1 break-all font-mono text-slate-200">{{ profile()!.did }}</dd>
                  </div>
                </dl>
              </article>

              <article class="rounded-lg border border-slate-700 bg-slate-900/50 p-5">
                <p class="text-xs font-semibold uppercase text-blue-300">Datos personales</p>
                <dl class="mt-4 grid gap-4 text-sm sm:grid-cols-2">
                  <div>
                    <dt class="text-slate-500">Nombre completo</dt>
                    <dd class="mt-1 text-slate-100">{{ profile()!.fullName || '-' }}</dd>
                  </div>
                  <div>
                    <dt class="text-slate-500">Fecha nacimiento</dt>
                    <dd class="mt-1 text-slate-100">{{ formatDate(profile()!.birthDate) }}</dd>
                  </div>
                  <div>
                    <dt class="text-slate-500">Email contacto</dt>
                    <dd class="mt-1 break-all text-slate-100">{{ profile()!.contactEmail || '-' }}</dd>
                  </div>
                  <div>
                    <dt class="text-slate-500">Pais</dt>
                    <dd class="mt-1 text-slate-100">{{ profile()!.countryCode || '-' }}</dd>
                  </div>
                  <div>
                    <dt class="text-slate-500">Telefono</dt>
                    <dd class="mt-1 text-slate-100">{{ profile()!.phoneNumber || '-' }}</dd>
                  </div>
                  <div>
                    <dt class="text-slate-500">Actualizado</dt>
                    <dd class="mt-1 text-slate-100">{{ formatDateTime(profile()!.updatedAt) }}</dd>
                  </div>
                </dl>
              </article>
            </div>

            <div class="grid min-h-0 gap-5 px-5 pb-5 lg:grid-cols-2">
              <article class="min-h-0 rounded-lg border border-slate-700 bg-slate-900/50">
                <div class="border-b border-slate-700 px-4 py-3">
                  <h4 class="font-semibold text-white">Instituciones vinculadas</h4>
                  <p class="text-sm text-slate-400">
                    {{ institutions().length }} relacion(es) encontradas por wallet.
                  </p>
                </div>
                <div class="max-h-72 overflow-auto">
                  @if (institutions().length === 0) {
                    <p class="p-4 text-sm text-slate-400">
                      Esta wallet aun no esta vinculada a estudiantes institucionales.
                    </p>
                  }

                  @for (institution of institutions(); track institution.studentId) {
                    <div class="border-b border-slate-700 px-4 py-4 last:border-b-0">
                      <div class="flex items-start justify-between gap-4">
                        <div>
                          <p class="font-semibold text-white">{{ institution.institutionName }}</p>
                          <p class="text-xs text-slate-500">{{ institution.institutionId }}</p>
                        </div>
                        <span class="rounded-full border border-emerald-500/30 bg-emerald-500/10 px-2 py-1 text-xs text-emerald-300">
                          {{ institution.isPrimary ? 'Wallet primaria' : 'Wallet asociada' }}
                        </span>
                      </div>
                      <dl class="mt-3 grid grid-cols-2 gap-3 text-xs">
                        <div>
                          <dt class="text-slate-500">Codigo</dt>
                          <dd class="text-slate-200">{{ institution.institutionCode }}</dd>
                        </div>
                        <div>
                          <dt class="text-slate-500">Matricula</dt>
                          <dd class="text-slate-200">{{ institution.enrollmentYear || '-' }}</dd>
                        </div>
                        <div>
                          <dt class="text-slate-500">Referencia</dt>
                          <dd class="text-slate-200">{{ institution.externalReference || '-' }}</dd>
                        </div>
                        <div>
                          <dt class="text-slate-500">Vinculada</dt>
                          <dd class="text-slate-200">{{ formatDateTime(institution.linkedAt) }}</dd>
                        </div>
                      </dl>
                    </div>
                  }
                </div>
              </article>

              <article class="min-h-0 rounded-lg border border-slate-700 bg-slate-900/50">
                <div class="border-b border-slate-700 px-4 py-3">
                  <h4 class="font-semibold text-white">Credenciales</h4>
                  <p class="text-sm text-slate-400">
                    Titulos y certificados emitidos hacia tu DID/wallet.
                  </p>
                </div>
                <div class="max-h-72 overflow-auto">
                  @if (credentials().length === 0) {
                    <p class="p-4 text-sm text-slate-400">
                      Aun no tienes credenciales emitidas para esta cuenta.
                    </p>
                  }

                  @for (credential of credentials(); track credential.id) {
                    <div class="border-b border-slate-700 px-4 py-4 last:border-b-0">
                      <div class="flex items-start justify-between gap-4">
                        <div>
                          <p class="font-semibold text-white">{{ credential.title }}</p>
                          <p class="text-sm text-slate-400">{{ credential.issuerName }}</p>
                        </div>
                        <span class="rounded-full border px-2 py-1 text-xs" [ngClass]="statusClass(credential.status)">
                          {{ statusLabel(credential.status) }}
                        </span>
                      </div>
                      <div class="mt-3 flex flex-wrap gap-2">
                        <button
                          type="button"
                          class="rounded-lg border border-slate-600 px-3 py-2 text-xs font-semibold text-slate-100 hover:bg-slate-800 disabled:opacity-50"
                          [disabled]="actionCredentialId() === credential.id"
                          (click)="handleDownload(credential.id)"
                        >
                          Descargar JSON
                        </button>
                        <button
                          type="button"
                          class="rounded-lg border border-slate-600 px-3 py-2 text-xs font-semibold text-slate-100 hover:bg-slate-800"
                          (click)="handleShare(credential.id)"
                        >
                          Copiar ID
                        </button>
                        <button
                          type="button"
                          class="rounded-lg border border-blue-500/40 bg-blue-500/10 px-3 py-2 text-xs font-semibold text-blue-200 hover:bg-blue-500/20"
                          (click)="openAnchorsModal(credential.id)"
                        >
                          Ver anclas
                        </button>
                      </div>
                    </div>
                  }
                </div>
              </article>
            </div>
            </div>
          </div>

          <aside class="min-h-0 overflow-y-auto overscroll-y-contain rounded-lg border border-slate-700 bg-slate-800 p-5">
            <p class="text-xs font-semibold uppercase text-blue-300">Resumen</p>
            <h3 class="mt-2 text-xl font-semibold text-white">Estado del holder</h3>
            <div class="mt-5 grid grid-cols-2 gap-3">
              <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-sm text-slate-500">Instituciones</p>
                <p class="mt-2 text-2xl font-bold text-white">{{ institutions().length }}</p>
              </div>
              <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-sm text-slate-500">Credenciales</p>
                <p class="mt-2 text-2xl font-bold text-white">{{ credentials().length }}</p>
              </div>
              <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-sm text-slate-500">Activas</p>
                <p class="mt-2 text-2xl font-bold text-emerald-300">{{ activeCredentials() }}</p>
              </div>
              <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <p class="text-sm text-slate-500">Perfil</p>
                <p class="mt-2 text-lg font-bold" [ngClass]="isProfileComplete() ? 'text-emerald-300' : 'text-amber-300'">
                  {{ isProfileComplete() ? 'Completo' : 'Pendiente' }}
                </p>
              </div>
            </div>

            <div class="mt-5 rounded-lg border border-slate-700 bg-slate-900/60 p-4 text-sm">
              <h4 class="font-semibold text-white">Alcance</h4>
              <p class="mt-2 text-slate-400">
                Estos datos personales quedan off-chain. Las credenciales emitidas por una institucion se verifican por DID, hash, estado y anclas registradas por Issuer.
              </p>
            </div>
          </aside>
        </section>
        </div>
      }

      <app-modal
        [isOpen]="isProfileModalOpen()"
        title="Editar perfil holder"
        description="Actualiza datos personales off-chain visibles en tu portal."
        size="lg"
        (closed)="closeProfileModal()"
      >
        <form class="grid gap-4" [formGroup]="profileForm" (ngSubmit)="saveProfile()">
          <div class="grid gap-4 sm:grid-cols-2">
            <label class="block text-sm text-slate-200">
              Nombre visible
              <input class="mt-1 w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white outline-none focus:border-blue-500" formControlName="displayName" />
            </label>
            <label class="block text-sm text-slate-200">
              Nombre completo
              <input class="mt-1 w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white outline-none focus:border-blue-500" formControlName="fullName" />
            </label>
            <label class="block text-sm text-slate-200">
              Fecha nacimiento
              <input type="date" class="mt-1 w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white outline-none focus:border-blue-500" formControlName="birthDate" />
            </label>
            <label class="block text-sm text-slate-200">
              Pais
              <input maxlength="2" class="mt-1 w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 uppercase text-white outline-none focus:border-blue-500" formControlName="countryCode" />
            </label>
            <label class="block text-sm text-slate-200">
              Email contacto
              <input type="email" class="mt-1 w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white outline-none focus:border-blue-500" formControlName="contactEmail" />
            </label>
            <label class="block text-sm text-slate-200">
              Telefono
              <input class="mt-1 w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white outline-none focus:border-blue-500" formControlName="phoneNumber" />
            </label>
          </div>

          <div class="flex justify-end gap-3 pt-2">
            <button
              type="button"
              class="rounded-lg border border-slate-600 px-4 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-700"
              (click)="closeProfileModal()"
            >
              Cancelar
            </button>
            <button
              type="submit"
              class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-60"
              [disabled]="isSavingProfile()"
            >
              {{ isSavingProfile() ? 'Guardando...' : 'Guardar perfil' }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="anchorsModal().open"
        title="Anclas on-chain"
        description="Datos verificables registrados por el emisor."
        size="lg"
        (closed)="closeAnchorsModal()"
      >
        @if (anchorsModal().state === 'loading') {
          <p class="text-sm text-slate-300">Cargando anclas...</p>
        }

        @if (anchorsModal().state === 'error') {
          <p class="text-sm text-red-300" role="alert">{{ anchorsModal().error }}</p>
          <button
            type="button"
            class="mt-4 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500"
            (click)="retryAnchorsModal()"
          >
            Reintentar
          </button>
        }

        @if (anchorsModal().state === 'loaded' && anchorsModal().detail; as detail) {
          <app-credential-anchors-panel
            [anchors]="detail.anchors"
            [credentialId]="detail.id"
          />
        }
      </app-modal>
    </app-portal-shell>
  `,
})
export class HolderComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly holderService = inject(HolderService);
  private readonly router = inject(Router);

  readonly credentials = signal<ReadonlyArray<HolderCredentialSummary>>([]);
  readonly institutions = signal<ReadonlyArray<HolderInstitutionSummary>>([]);
  readonly profile = signal<HolderProfile | null>(null);
  readonly loadState = signal<HolderLoadState>('loading');
  readonly errorMessage = signal<string | null>(null);
  readonly isUnauthorized = signal(false);
  readonly feedback = signal<string | null>(null);
  readonly feedbackType = signal<'success' | 'error'>('success');
  readonly isProfileModalOpen = signal(false);
  readonly anchorsModal = signal<AnchorsModalModel>(CLOSED_ANCHORS_MODAL);
  readonly isSavingProfile = signal(false);
  readonly actionCredentialId = signal<string | null>(null);

  private readonly credentialDetailCache = new Map<string, HolderCredentialDetail>();

  readonly profileForm = this.formBuilder.nonNullable.group({
    displayName: [''],
    fullName: [''],
    birthDate: [''],
    contactEmail: [''],
    countryCode: ['CL'],
    phoneNumber: [''],
  });

  readonly activeCredentials = computed(
    () => this.credentials().filter((credential) => credential.status === 'active').length,
  );

  readonly isProfileComplete = computed(() => {
    const profile = this.profile();
    return Boolean(profile?.fullName && profile?.birthDate && profile?.contactEmail);
  });

  readonly displayName = computed(() => {
    const profile = this.profile();
    return profile?.displayName || profile?.fullName || null;
  });

  ngOnInit(): void {
    void this.loadHolder();
  }

  async loadHolder(): Promise<void> {
    this.loadState.set('loading');
    this.errorMessage.set(null);
    this.isUnauthorized.set(false);
    this.feedback.set(null);

    try {
      const [dashboard, credentials] = await Promise.all([
        this.holderService.getMyDashboard(),
        this.holderService.listMyCredentials(),
      ]);
      this.profile.set(dashboard.profile);
      this.institutions.set(dashboard.institutions);
      this.credentials.set(credentials);
      this.loadState.set('loaded');
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
      this.isUnauthorized.set(error instanceof HolderUnauthorizedError);
      this.loadState.set('error');
    }
  }

  openProfileModal(): void {
    const profile = this.profile();
    this.profileForm.reset({
      displayName: profile?.displayName ?? '',
      fullName: profile?.fullName ?? '',
      birthDate: profile?.birthDate ?? '',
      contactEmail: profile?.contactEmail ?? '',
      countryCode: profile?.countryCode ?? 'CL',
      phoneNumber: profile?.phoneNumber ?? '',
    });
    this.isProfileModalOpen.set(true);
  }

  closeProfileModal(): void {
    if (!this.isSavingProfile()) {
      this.isProfileModalOpen.set(false);
    }
  }

  async saveProfile(): Promise<void> {
    this.isSavingProfile.set(true);
    this.feedback.set(null);

    try {
      const value = this.profileForm.getRawValue();
      const updated = await this.holderService.updateMyProfile({
        displayName: this.blankToNull(value.displayName),
        fullName: this.blankToNull(value.fullName),
        birthDate: this.blankToNull(value.birthDate),
        contactEmail: this.blankToNull(value.contactEmail),
        countryCode: this.blankToNull(value.countryCode)?.toUpperCase() ?? null,
        phoneNumber: this.blankToNull(value.phoneNumber),
      });
      this.profile.set(updated);
      this.isProfileModalOpen.set(false);
      this.showFeedback('Perfil actualizado correctamente.', 'success');
    } catch (error: unknown) {
      this.showFeedback(toErrorMessage(error), 'error');
    } finally {
      this.isSavingProfile.set(false);
    }
  }

  async handleDownload(credentialId: string): Promise<void> {
    this.actionCredentialId.set(credentialId);
    this.feedback.set(null);

    try {
      const detail = await this.getCredentialDetail(credentialId);
      this.holderService.downloadCredentialJson(detail);
      this.showFeedback('Credencial descargada.', 'success');
    } catch (error: unknown) {
      this.showFeedback(toErrorMessage(error), 'error');
    } finally {
      this.actionCredentialId.set(null);
    }
  }

  async handleShare(credentialId: string): Promise<void> {
    try {
      await this.holderService.shareCredentialId(credentialId);
      this.showFeedback(`Credential ID copiado: ${credentialId}`, 'success');
    } catch (error: unknown) {
      this.showFeedback(toErrorMessage(error), 'error');
    }
  }

  async openAnchorsModal(credentialId: string): Promise<void> {
    this.anchorsModal.set({
      ...CLOSED_ANCHORS_MODAL,
      open: true,
      credentialId,
    });
    await this.loadAnchorsModalDetail();
  }

  closeAnchorsModal(): void {
    this.anchorsModal.set(CLOSED_ANCHORS_MODAL);
  }

  async retryAnchorsModal(): Promise<void> {
    await this.loadAnchorsModalDetail();
  }

  handleLogout(): void {
    this.credentialDetailCache.clear();
    this.closeAnchorsModal();
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  goToLogin(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  statusLabel(status: HolderCredentialSummary['status']): string {
    return STATUS_LABELS[status];
  }

  statusClass(status: HolderCredentialSummary['status']): string {
    switch (status) {
      case 'active':
        return 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300';
      case 'revoked':
        return 'border-red-500/30 bg-red-500/10 text-red-300';
      default:
        return 'border-amber-500/30 bg-amber-500/10 text-amber-300';
    }
  }

  formatDate(value?: string | null): string {
    if (!value) {
      return '-';
    }

    return value;
  }

  formatDateTime(value?: string | null): string {
    if (!value) {
      return '-';
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
  }

  private blankToNull(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length === 0 ? null : trimmed;
  }

  private showFeedback(message: string, type: 'success' | 'error'): void {
    this.feedback.set(message);
    this.feedbackType.set(type);
  }

  private async getCredentialDetail(credentialId: string): Promise<HolderCredentialDetail> {
    const cached = this.credentialDetailCache.get(credentialId);
    if (cached) {
      return cached;
    }

    const detail = await this.holderService.getMyCredential(credentialId);
    this.credentialDetailCache.set(credentialId, detail);
    return detail;
  }

  private async loadAnchorsModalDetail(): Promise<void> {
    const credentialId = this.anchorsModal().credentialId;
    if (!credentialId) {
      return;
    }

    this.anchorsModal.update((modal) => ({
      ...modal,
      state: 'loading',
      error: null,
      detail: null,
    }));

    try {
      const detail = await this.getCredentialDetail(credentialId);
      this.anchorsModal.update((modal) => ({
        ...modal,
        detail,
        state: 'loaded',
      }));
    } catch (error: unknown) {
      this.anchorsModal.update((modal) => ({
        ...modal,
        error: toErrorMessage(error),
        state: 'error',
      }));
    }
  }
}
