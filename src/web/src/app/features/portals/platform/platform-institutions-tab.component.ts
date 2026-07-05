import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  Component,
  OnInit,
  WritableSignal,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { InstitutionCreated } from '../../../api/bff/models/institution-created';
import { InstitutionInvitationCreated } from '../../../api/bff/models/institution-invitation-created';
import { InstitutionSummary } from '../../../api/bff/models/institution-summary';
import { InstitutionSummary as AcademyInstitutionSummary } from '../../../core/models/academy.models';
import {
  PLATFORM_DEFAULT_COUNTRY_CODE,
  PLATFORM_INVITATION_ROLES,
  PlatformInvitationRole,
} from '../../../core/constants/platform.constants';
import {
  AcademyService,
  PlatformUnauthorizedError,
} from '../../../core/services/academy.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { CopyValueComponent } from '../../../shared/ui/copy-value/copy-value.component';
import { ModalComponent } from '../../../shared/ui/modal/modal.component';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge/status-badge.component';

@Component({
  selector: 'app-platform-institutions-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    ModalComponent,
    StatusBadgeComponent,
    CopyValueComponent,
  ],
  template: `
    <div class="flex min-h-0 flex-1 flex-col gap-6">
      <header class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <h2 class="text-2xl font-bold text-white">Instituciones</h2>
          <p class="mt-1 text-sm text-slate-400">
            Administra tenants academicos, usuarios institucionales y acceso a Academy.
          </p>
        </div>

        <div class="flex flex-wrap gap-3">
          <button
            type="button"
            class="inline-flex items-center justify-center rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-600 disabled:cursor-not-allowed disabled:opacity-50"
            [disabled]="loadingInstitutions()"
            (click)="loadInstitutions()"
          >
            {{ loadingInstitutions() ? 'Cargando...' : 'Refrescar' }}
          </button>
          <button
            type="button"
            class="inline-flex items-center justify-center rounded-lg bg-violet-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-violet-700"
            (click)="openCreateModal()"
          >
            Crear institucion
          </button>
        </div>
      </header>

      @if (errorMessage()) {
        <div class="rounded-lg border border-red-700 bg-red-900/40 p-4 text-sm text-red-100">
          {{ errorMessage() }}
        </div>
      }
      @if (successMessage()) {
        <div class="rounded-lg border border-emerald-500/40 bg-emerald-500/10 p-4 text-sm text-emerald-100">
          {{ successMessage() }}
        </div>
      }

      <section class="grid min-h-0 flex-1 gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(360px,440px)]">
        <section class="flex min-h-0 flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-800">
          <div class="border-b border-slate-700 p-4">
            <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
              <div>
                <h3 class="text-base font-semibold text-white">Listado</h3>
                <p class="text-xs text-slate-400">{{ institutions().length }} instituciones registradas</p>
              </div>
              <input
                class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white placeholder:text-slate-500 focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30 md:w-72"
                placeholder="Buscar por nombre, codigo o ID"
                [value]="searchTerm()"
                (input)="updateSignal(searchTerm, $event)"
              />
            </div>
          </div>

          <div class="min-h-0 flex-1 overflow-auto">
            <table class="w-full min-w-[860px] text-left">
              <thead class="sticky top-0 z-10 bg-slate-800">
                <tr class="border-b border-slate-700 text-xs uppercase text-slate-400">
                  <th class="px-4 py-3 font-semibold">Institucion</th>
                  <th class="px-4 py-3 font-semibold">Codigo</th>
                  <th class="px-4 py-3 font-semibold">Pais</th>
                  <th class="px-4 py-3 font-semibold">Estado</th>
                  <th class="px-4 py-3 font-semibold">Wallet emisor</th>
                  <th class="px-4 py-3 font-semibold">Registrada</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-700/70">
                @for (institution of filteredInstitutions(); track institution.id) {
                  <tr
                    class="cursor-pointer transition hover:bg-slate-700/40"
                    [class.bg-slate-700]="selectedInstitution()?.id === institution.id"
                    (click)="selectForLookup(institution.id)"
                  >
                    <td class="px-4 py-4">
                      <p class="max-w-[18rem] truncate text-sm font-semibold text-white">
                        {{ institution.displayName }}
                      </p>
                      <p class="mt-1 max-w-[18rem] truncate text-xs text-slate-500">
                        {{ institution.legalName }}
                      </p>
                      <p class="mt-2 max-w-[18rem] truncate font-mono text-xs text-slate-500">
                        {{ institution.id }}
                      </p>
                    </td>
                    <td class="px-4 py-4 text-sm text-slate-300">
                      {{ institution.code }}
                    </td>
                    <td class="px-4 py-4 text-sm text-slate-300">
                      {{ institution.countryCode || '-' }}
                    </td>
                    <td class="px-4 py-4">
                      <app-status-badge
                        [label]="institution.isActive ? 'Activa' : 'Inactiva'"
                        [tone]="institution.isActive ? 'success' : 'danger'"
                      />
                    </td>
                    <td class="px-4 py-4">
                      @if (institution.issuerWalletAddress) {
                        <app-copy-value [value]="institution.issuerWalletAddress" [head]="8" [tail]="6" />
                      } @else {
                        <span class="text-xs text-amber-300">Sin wallet</span>
                      }
                    </td>
                    <td class="px-4 py-4 text-xs text-slate-400">
                      {{ formatDate(institution.registeredAt) }}
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="6" class="px-4 py-10 text-center text-sm text-slate-400">
                      @if (loadingInstitutions()) {
                        Cargando instituciones...
                      } @else if (institutions().length) {
                        No hay resultados para la busqueda actual.
                      } @else {
                        No hay instituciones para mostrar.
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>

        <aside class="min-h-0 overflow-auto rounded-lg border border-slate-700 bg-slate-800 p-6">
          @if (selectedInstitution()) {
            <div class="space-y-6">
              @if (loadingInstitution()) {
                <div class="rounded-lg border border-blue-500/30 bg-blue-500/10 p-3 text-sm text-blue-100">
                  Cargando detalle de institucion...
                </div>
              }
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <p class="text-xs font-medium uppercase text-violet-300">Institucion seleccionada</p>
                  <h3 class="mt-1 truncate text-xl font-semibold text-white">{{ selectedInstitution()!.displayName }}</h3>
                  <p class="text-sm text-slate-400">{{ selectedInstitution()!.legalName }}</p>
                </div>
                <div class="flex items-center gap-2">
                  <app-status-badge
                    [label]="selectedInstitution()!.isActive ? 'Activa' : 'Inactiva'"
                    [tone]="selectedInstitution()!.isActive ? 'success' : 'danger'"
                  />
                  <button
                    type="button"
                    class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                    aria-label="Cerrar detalle"
                    (click)="clearSelection()"
                  >
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>

              <dl class="space-y-4 text-sm">
                <div>
                  <dt class="text-slate-500">Codigo</dt>
                  <dd class="text-slate-200">{{ selectedInstitution()!.code }}</dd>
                </div>
                <div>
                  <dt class="text-slate-500">Pais</dt>
                  <dd class="text-slate-200">{{ selectedInstitution()!.countryCode }}</dd>
                </div>
                <div>
                  <dt class="text-slate-500">ID</dt>
                  <dd><app-copy-value [value]="selectedInstitution()!.id" /></dd>
                </div>
                <div>
                  <dt class="text-slate-500">DID emisor</dt>
                  <dd><app-copy-value [value]="selectedInstitution()!.did" /></dd>
                </div>
                <div>
                  <dt class="text-slate-500">Wallet emisor</dt>
                  <dd><app-copy-value [value]="selectedInstitution()!.issuerWalletAddress" /></dd>
                </div>
                <div>
                  <dt class="text-slate-500">Registrada</dt>
                  <dd class="text-slate-200">{{ selectedInstitution()!.registeredAt || '-' }}</dd>
                </div>
              </dl>

              <div class="space-y-3 border-t border-slate-700 pt-5">
                <a
                  class="inline-flex w-full items-center justify-center rounded-lg bg-violet-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-violet-700"
                  [routerLink]="['/academy']"
                  [queryParams]="{ institutionId: selectedInstitution()!.id }"
                >
                  Abrir Academy
                </a>
                <button
                  type="button"
                  class="inline-flex w-full items-center justify-center rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
                  (click)="openInviteModal()"
                >
                  Invitar usuario
                </button>
                <button
                  type="button"
                  class="inline-flex w-full items-center justify-center rounded-lg border border-slate-600 px-4 py-2.5 text-sm font-semibold text-slate-300 hover:bg-slate-700 hover:text-white"
                  (click)="clearSelection()"
                >
                  Limpiar seleccion
                </button>
              </div>

              @if (lastInvitation()) {
                <div class="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-3 text-xs text-slate-200">
                  <p class="font-semibold text-emerald-300">Invitacion creada para {{ lastInvitation()!.email }}</p>
                  <a class="mt-2 block break-all text-blue-300 hover:text-blue-200" [href]="lastInvitation()!.invitationUrl!" target="_blank" rel="noopener noreferrer">
                    {{ lastInvitation()!.invitationUrl }}
                  </a>
                </div>
              }
            </div>
          } @else {
            <div class="space-y-6">
              <div>
                <p class="text-xs font-medium uppercase text-violet-300">Resumen Platform</p>
                <h3 class="mt-1 text-xl font-semibold text-white">Vista general</h3>
                <p class="mt-1 text-sm text-slate-400">
                  Selecciona una institucion del listado para ver su detalle operativo.
                </p>
              </div>

              <div class="grid grid-cols-2 gap-3">
                <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                  <p class="text-xs text-slate-500">Total</p>
                  <p class="mt-2 text-2xl font-bold text-white">{{ institutions().length }}</p>
                </article>
                <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                  <p class="text-xs text-slate-500">Activas</p>
                  <p class="mt-2 text-2xl font-bold text-emerald-300">{{ activeInstitutionCount() }}</p>
                </article>
                <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                  <p class="text-xs text-slate-500">Inactivas</p>
                  <p class="mt-2 text-2xl font-bold text-red-300">{{ inactiveInstitutionCount() }}</p>
                </article>
                <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                  <p class="text-xs text-slate-500">Sin wallet</p>
                  <p class="mt-2 text-2xl font-bold text-amber-300">{{ institutionsMissingIssuerWalletCount() }}</p>
                </article>
              </div>

              <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                <h4 class="text-sm font-semibold text-white">Datos rapidos</h4>
                <dl class="mt-4 space-y-3 text-sm">
                  <div class="flex items-center justify-between gap-3">
                    <dt class="text-slate-400">Resultados filtrados</dt>
                    <dd class="font-semibold text-white">{{ filteredInstitutions().length }}</dd>
                  </div>
                  <div class="flex items-center justify-between gap-3">
                    <dt class="text-slate-400">Ultima accion</dt>
                    <dd class="text-right text-slate-200">{{ latestActionLabel() }}</dd>
                  </div>
                </dl>
              </div>
            </div>
          }
        </aside>
      </section>

      <app-modal
        [isOpen]="createModalOpen()"
        title="Crear institucion"
        description="Registra una nueva institucion tenant y deja sus datos base disponibles para Academy."
        size="lg"
        (closed)="closeCreateModal()"
      >
        <form class="grid gap-4 md:grid-cols-2" (submit)="handleCreate($event)">
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="code">Codigo</label>
            <input id="code" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30" [value]="createCode()" (input)="updateSignal(createCode, $event)" required />
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="countryCode">Pais</label>
            <input id="countryCode" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30" [value]="createCountryCode()" (input)="updateSignal(createCountryCode, $event)" required />
          </div>
          <div class="md:col-span-2">
            <label class="mb-1 block text-sm font-medium text-slate-300" for="legalName">Razon social</label>
            <input id="legalName" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30" [value]="createLegalName()" (input)="updateSignal(createLegalName, $event)" required />
          </div>
          <div class="md:col-span-2">
            <label class="mb-1 block text-sm font-medium text-slate-300" for="displayName">Nombre para mostrar</label>
            <input id="displayName" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30" [value]="createDisplayName()" (input)="updateSignal(createDisplayName, $event)" required />
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="contactEmail">Email de contacto</label>
            <input id="contactEmail" type="email" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30" [value]="createContactEmail()" (input)="updateSignal(createContactEmail, $event)" required />
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="websiteUrl">Sitio web opcional</label>
            <input id="websiteUrl" type="url" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30" [value]="createWebsiteUrl()" (input)="updateSignal(createWebsiteUrl, $event)" />
          </div>
          <div class="flex justify-end gap-3 md:col-span-2">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeCreateModal()">Cancelar</button>
            <button type="submit" class="rounded-lg bg-violet-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-violet-700 disabled:cursor-not-allowed disabled:opacity-50" [disabled]="creating()">
              {{ creating() ? 'Creando...' : 'Crear institucion' }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="inviteModalOpen()"
        title="Invitar usuario"
        description="Envia un link temporal para que el usuario institucional conecte su wallet."
        (closed)="closeInviteModal()"
      >
        <form class="grid gap-4" (submit)="handleInvite($event)">
          <p class="text-sm text-slate-400">
            La invitacion quedara asociada a
            <span class="font-semibold text-white">{{ selectedInstitution()?.displayName }}</span>.
          </p>
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="inviteEmail">Email</label>
            <input id="inviteEmail" type="email" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30" [value]="inviteEmail()" (input)="updateSignal(inviteEmail, $event)" required />
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="inviteRole">Rol</label>
            <select id="inviteRole" name="platformInviteRole" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/30" [ngModel]="inviteRole()" (ngModelChange)="inviteRole.set($event)">
              @for (role of invitationRoles; track role) {
                <option [value]="role">{{ role }}</option>
              }
            </select>
          </div>
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeInviteModal()">Cancelar</button>
            <button type="submit" class="rounded-lg bg-violet-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-violet-700 disabled:cursor-not-allowed disabled:opacity-50" [disabled]="inviting()">
              {{ inviting() ? 'Enviando...' : 'Enviar invitacion' }}
            </button>
          </div>
        </form>
      </app-modal>
    </div>
  `,
})
export class PlatformInstitutionsTabComponent implements OnInit {
  readonly invitationRoles = PLATFORM_INVITATION_ROLES;

  private readonly academyService = inject(AcademyService);
  private readonly router = inject(Router);

  readonly createCode = signal('');
  readonly createLegalName = signal('');
  readonly createDisplayName = signal('');
  readonly createContactEmail = signal('');
  readonly createCountryCode = signal(PLATFORM_DEFAULT_COUNTRY_CODE);
  readonly createWebsiteUrl = signal('');

  readonly searchTerm = signal('');
  readonly lookupId = signal('');
  readonly inviteEmail = signal('');
  readonly inviteRole = signal<PlatformInvitationRole>('issuer');

  readonly createModalOpen = signal(false);
  readonly inviteModalOpen = signal(false);
  readonly creating = signal(false);
  readonly loadingInstitutions = signal(false);
  readonly loadingInstitution = signal(false);
  readonly inviting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly createdResult = signal<InstitutionCreated | null>(null);
  readonly institutions = signal<readonly AcademyInstitutionSummary[]>([]);
  readonly selectedInstitution = signal<InstitutionSummary | null>(null);
  readonly lastInvitation = signal<InstitutionInvitationCreated | null>(null);

  readonly filteredInstitutions = computed(() => {
    const term = this.searchTerm().trim().toLocaleLowerCase();
    const institutions = this.institutions();

    if (!term) {
      return institutions;
    }

    return institutions.filter((institution) =>
      [
        institution.id,
        institution.code,
        institution.displayName,
        institution.legalName,
        institution.countryCode,
      ]
        .filter(Boolean)
        .some((value) => value.toLocaleLowerCase().includes(term)),
    );
  });

  readonly activeInstitutionCount = computed(
    () => this.institutions().filter((institution) => institution.isActive).length,
  );

  readonly inactiveInstitutionCount = computed(
    () => this.institutions().length - this.activeInstitutionCount(),
  );

  readonly institutionsMissingIssuerWalletCount = computed(
    () => this.institutions().filter((institution) => !institution.issuerWalletAddress).length,
  );

  readonly latestActionLabel = computed(() => {
    const invited = this.lastInvitation()?.email;
    const created = this.createdResult()?.institution?.displayName;

    if (invited) {
      return `Invitacion enviada a ${invited}`;
    }

    if (created) {
      return `Institucion creada: ${created}`;
    }

    return 'Sin acciones recientes';
  });

  ngOnInit(): void {
    void this.loadInstitutions();
  }

  updateSignal(target: WritableSignal<string>, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    target.set(value);
  }

  formatDate(value?: string | null): string {
    if (!value) {
      return '-';
    }

    return new Intl.DateTimeFormat('es-CL', {
      dateStyle: 'short',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  openCreateModal(): void {
    this.createModalOpen.set(true);
  }

  closeCreateModal(): void {
    if (!this.creating()) {
      this.createModalOpen.set(false);
    }
  }

  openInviteModal(): void {
    if (this.selectedInstitution()) {
      this.inviteModalOpen.set(true);
    }
  }

  closeInviteModal(): void {
    if (!this.inviting()) {
      this.inviteModalOpen.set(false);
    }
  }

  clearSelection(): void {
    this.lookupId.set('');
    this.selectedInstitution.set(null);
    this.lastInvitation.set(null);
  }

  async handleCreate(event: Event): Promise<void> {
    event.preventDefault();
    this.creating.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const result = await this.academyService.createInstitution({
        code: this.createCode().trim(),
        legalName: this.createLegalName().trim(),
        displayName: this.createDisplayName().trim(),
        contactEmail: this.createContactEmail().trim(),
        countryCode: this.createCountryCode().trim() || PLATFORM_DEFAULT_COUNTRY_CODE,
        websiteUrl: this.createWebsiteUrl().trim() || null,
      });
      this.createdResult.set(result);
      if (result.institution?.id) {
        this.lookupId.set(result.institution.id);
        this.selectedInstitution.set(result.institution);
      }
      this.resetCreateForm();
      this.createModalOpen.set(false);
      await this.loadInstitutions();
      this.successMessage.set(`Institucion ${result.institution?.displayName ?? 'creada'} creada correctamente.`);
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
      if (error instanceof PlatformUnauthorizedError) {
        await this.router.navigate(['/login'], {
          queryParams: { returnUrl: '/platform' },
        });
      }
    } finally {
      this.creating.set(false);
    }
  }

  async loadInstitutions(): Promise<void> {
    this.loadingInstitutions.set(true);
    this.errorMessage.set(null);

    try {
      this.institutions.set(await this.academyService.listInstitutions());
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.loadingInstitutions.set(false);
    }
  }

  async selectForLookup(institutionId: string): Promise<void> {
    if (this.selectedInstitution()?.id === institutionId) {
      this.clearSelection();
      return;
    }

    this.lookupId.set(institutionId);
    await this.handleLookup();
  }

  async handleLookup(): Promise<void> {
    const institutionId = this.lookupId().trim();
    if (!institutionId) {
      this.errorMessage.set('Ingresa un UUID de institucion.');
      return;
    }

    this.loadingInstitution.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.lastInvitation.set(null);

    try {
      const institution = await this.academyService.getInstitution(institutionId);
      this.selectedInstitution.set(institution);
    } catch (error: unknown) {
      this.selectedInstitution.set(null);
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.loadingInstitution.set(false);
    }
  }

  async handleInvite(event: Event): Promise<void> {
    event.preventDefault();
    const institution = this.selectedInstitution();
    if (!institution?.id) {
      return;
    }

    this.inviting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const invitation = await this.academyService.inviteInstitutionUser(
        institution.id,
        {
          email: this.inviteEmail().trim(),
          role: this.inviteRole(),
        },
      );
      this.lastInvitation.set(invitation);
      this.inviteEmail.set('');
      this.inviteModalOpen.set(false);
      this.successMessage.set(`Invitacion ${invitation.role} enviada a ${invitation.email}.`);
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.inviting.set(false);
    }
  }

  private resetCreateForm(): void {
    this.createCode.set('');
    this.createLegalName.set('');
    this.createDisplayName.set('');
    this.createContactEmail.set('');
    this.createCountryCode.set(PLATFORM_DEFAULT_COUNTRY_CODE);
    this.createWebsiteUrl.set('');
  }
}
