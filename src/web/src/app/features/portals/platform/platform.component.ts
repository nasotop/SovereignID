import { CommonModule } from '@angular/common';
import { Component, inject, signal, WritableSignal } from '@angular/core';
import { Router } from '@angular/router';

import { InstitutionCreated } from '../../../api/bff/models/institution-created';
import { InstitutionInvitationCreated } from '../../../api/bff/models/institution-invitation-created';
import { InstitutionSummary } from '../../../api/bff/models/institution-summary';
import {
  PLATFORM_DEFAULT_COUNTRY_CODE,
  PLATFORM_INVITATION_ROLES,
  PlatformInvitationRole,
} from '../../../core/constants/platform.constants';
import {
  AcademyService,
  PlatformUnauthorizedError,
} from '../../../core/services/academy.service';
import { AuthService } from '../../../core/services/auth.service';
import { toErrorMessage } from '../../../core/utils/error.utils';

@Component({
  selector: 'app-platform',
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
              class="w-9 h-9 rounded-lg bg-violet-600 flex items-center justify-center"
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
                  d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
                />
              </svg>
            </div>
            <div>
              <h1 class="text-lg font-bold text-white tracking-tight">
                SovereignID
              </h1>
              <p class="text-xs text-slate-400">Platform Portal</p>
            </div>
          </div>
          <button
            type="button"
            class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-slate-300 hover:text-white bg-slate-700/50 hover:bg-slate-700 border border-slate-600 rounded-lg transition-colors"
            (click)="handleLogout()"
          >
            Cerrar sesión
          </button>
        </div>
      </nav>

      <main class="max-w-7xl mx-auto px-6 py-8 space-y-8">
        @if (errorMessage()) {
          <div
            class="p-4 bg-red-900/40 border border-red-700 rounded-lg text-red-100 text-sm"
          >
            {{ errorMessage() }}
          </div>
        }

        <section
          class="bg-slate-800/60 border border-slate-700 rounded-xl p-6"
        >
          <h2 class="text-xl font-semibold text-white mb-4">
            Crear institución
          </h2>
          <form class="grid gap-4 md:grid-cols-2" (submit)="handleCreate($event)">
            <div>
              <label class="block text-sm text-slate-300 mb-1" for="code"
                >Código</label
              >
              <input
                id="code"
                class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
                [value]="createCode()"
                (input)="updateSignal(createCode, $event)"
                required
              />
            </div>
            <div>
              <label class="block text-sm text-slate-300 mb-1" for="countryCode"
                >País</label
              >
              <input
                id="countryCode"
                class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
                [value]="createCountryCode()"
                (input)="updateSignal(createCountryCode, $event)"
                required
              />
            </div>
            <div class="md:col-span-2">
              <label class="block text-sm text-slate-300 mb-1" for="legalName"
                >Razón social</label
              >
              <input
                id="legalName"
                class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
                [value]="createLegalName()"
                (input)="updateSignal(createLegalName, $event)"
                required
              />
            </div>
            <div class="md:col-span-2">
              <label class="block text-sm text-slate-300 mb-1" for="displayName"
                >Nombre para mostrar</label
              >
              <input
                id="displayName"
                class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
                [value]="createDisplayName()"
                (input)="updateSignal(createDisplayName, $event)"
                required
              />
            </div>
            <div>
              <label class="block text-sm text-slate-300 mb-1" for="contactEmail"
                >Email de contacto</label
              >
              <input
                id="contactEmail"
                type="email"
                class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
                [value]="createContactEmail()"
                (input)="updateSignal(createContactEmail, $event)"
                required
              />
            </div>
            <div>
              <label class="block text-sm text-slate-300 mb-1" for="websiteUrl"
                >Sitio web (opcional)</label
              >
              <input
                id="websiteUrl"
                type="url"
                class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
                [value]="createWebsiteUrl()"
                (input)="updateSignal(createWebsiteUrl, $event)"
              />
            </div>
            <div class="md:col-span-2">
              <button
                type="submit"
                class="bg-violet-600 hover:bg-violet-700 disabled:opacity-50 text-white font-semibold px-4 py-2 rounded-lg transition"
                [disabled]="creating()"
              >
                {{ creating() ? 'Creando...' : 'Crear institución' }}
              </button>
            </div>
          </form>

          @if (createdResult()) {
            <div
              class="mt-6 p-4 bg-emerald-900/20 border border-emerald-700/50 rounded-lg text-sm text-slate-200 space-y-2"
            >
              <p class="font-semibold text-emerald-300">
                Institución creada: {{ createdResult()!.institution?.displayName }}
              </p>
              <p>
                <span class="text-slate-400">ID:</span>
                <span class="font-mono ml-2">{{
                  createdResult()!.institution?.id
                }}</span>
              </p>
              @if (createdResult()!.invitation?.invitationUrl) {
                <div class="flex flex-wrap items-center gap-2">
                  <span class="text-slate-400">Invitación admin:</span>
                  <a
                    class="text-blue-400 hover:text-blue-300 break-all"
                    [href]="createdResult()!.invitation!.invitationUrl!"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {{ createdResult()!.invitation!.invitationUrl }}
                  </a>
                  <button
                    type="button"
                    class="text-xs px-2 py-1 rounded border border-slate-600 text-slate-300 hover:bg-slate-700"
                    (click)="copyInvitationUrl()"
                  >
                    Copiar
                  </button>
                </div>
              }
            </div>
          }
        </section>

        <section
          class="bg-slate-800/60 border border-slate-700 rounded-xl p-6"
        >
          <h2 class="text-xl font-semibold text-white mb-4">
            Consultar institución
          </h2>
          <div class="flex flex-wrap gap-3 mb-4">
            <input
              class="flex-1 min-w-[240px] rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white font-mono text-sm"
              placeholder="UUID de institución"
              [value]="lookupId()"
              (input)="updateSignal(lookupId, $event)"
            />
            <button
              type="button"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white font-semibold px-4 py-2 rounded-lg transition"
              [disabled]="loadingInstitution()"
              (click)="handleLookup()"
            >
              {{ loadingInstitution() ? 'Buscando...' : 'Buscar' }}
            </button>
          </div>

          @if (selectedInstitution()) {
            <div class="p-4 border border-slate-600 rounded-lg space-y-3 text-sm">
              <div class="flex flex-wrap items-center justify-between gap-2">
                <h3 class="text-lg font-semibold text-white">
                  {{ selectedInstitution()!.displayName }}
                </h3>
                <span
                  class="px-2 py-0.5 rounded text-xs border"
                  [class]="
                    selectedInstitution()!.isActive
                      ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
                      : 'bg-red-500/15 text-red-400 border-red-500/30'
                  "
                >
                  {{ selectedInstitution()!.isActive ? 'Activa' : 'Inactiva' }}
                </span>
              </div>
              <dl class="grid gap-2 md:grid-cols-2 text-slate-300">
                <div>
                  <dt class="text-slate-500">Código</dt>
                  <dd>{{ selectedInstitution()!.code }}</dd>
                </div>
                <div>
                  <dt class="text-slate-500">País</dt>
                  <dd>{{ selectedInstitution()!.countryCode }}</dd>
                </div>
                <div class="md:col-span-2">
                  <dt class="text-slate-500">ID</dt>
                  <dd class="font-mono break-all">{{ selectedInstitution()!.id }}</dd>
                </div>
                <div class="md:col-span-2">
                  <dt class="text-slate-500">DID emisor</dt>
                  <dd class="font-mono break-all text-xs">
                    {{ selectedInstitution()!.did || '—' }}
                  </dd>
                </div>
                <div class="md:col-span-2">
                  <dt class="text-slate-500">Wallet emisor</dt>
                  <dd class="font-mono break-all text-xs">
                    {{ selectedInstitution()!.issuerWalletAddress || '—' }}
                  </dd>
                </div>
                <div>
                  <dt class="text-slate-500">Registrada</dt>
                  <dd>{{ selectedInstitution()!.registeredAt || '—' }}</dd>
                </div>
              </dl>

              <details class="pt-2 border-t border-slate-700">
                <summary class="cursor-pointer text-violet-300 font-medium">
                  Invitar usuario
                </summary>
                <form
                  class="mt-4 grid gap-3 md:grid-cols-2"
                  (submit)="handleInvite($event)"
                >
                  <div class="md:col-span-2">
                    <label class="block text-sm text-slate-300 mb-1" for="inviteEmail"
                      >Email</label
                    >
                    <input
                      id="inviteEmail"
                      type="email"
                      class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
                      [value]="inviteEmail()"
                      (input)="updateSignal(inviteEmail, $event)"
                      required
                    />
                  </div>
                  <div>
                    <label class="block text-sm text-slate-300 mb-1" for="inviteRole"
                      >Rol</label
                    >
                    <select
                      id="inviteRole"
                      class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white"
                      [value]="inviteRole()"
                      (change)="onInviteRoleChange($event)"
                    >
                      @for (role of invitationRoles; track role) {
                        <option [value]="role">{{ role }}</option>
                      }
                    </select>
                  </div>
                  <div class="flex items-end">
                    <button
                      type="submit"
                      class="bg-violet-600 hover:bg-violet-700 disabled:opacity-50 text-white font-semibold px-4 py-2 rounded-lg transition"
                      [disabled]="inviting()"
                    >
                      {{ inviting() ? 'Enviando...' : 'Enviar invitación' }}
                    </button>
                  </div>
                </form>

                @if (lastInvitation()) {
                  <div class="mt-4 p-3 bg-slate-900/60 rounded-lg text-xs text-slate-300">
                    <p>Invitación creada para {{ lastInvitation()!.email }}</p>
                    <a
                      class="text-blue-400 hover:text-blue-300 break-all"
                      [href]="lastInvitation()!.invitationUrl!"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {{ lastInvitation()!.invitationUrl }}
                    </a>
                  </div>
                }
              </details>
            </div>
          }
        </section>
      </main>
    </div>
  `,
})
export class PlatformComponent {
  readonly invitationRoles = PLATFORM_INVITATION_ROLES;

  private readonly academyService = inject(AcademyService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly createCode = signal('');
  readonly createLegalName = signal('');
  readonly createDisplayName = signal('');
  readonly createContactEmail = signal('');
  readonly createCountryCode = signal(PLATFORM_DEFAULT_COUNTRY_CODE);
  readonly createWebsiteUrl = signal('');

  readonly lookupId = signal('');
  readonly inviteEmail = signal('');
  readonly inviteRole = signal<PlatformInvitationRole>('issuer');

  readonly creating = signal(false);
  readonly loadingInstitution = signal(false);
  readonly inviting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly createdResult = signal<InstitutionCreated | null>(null);
  readonly selectedInstitution = signal<InstitutionSummary | null>(null);
  readonly lastInvitation = signal<InstitutionInvitationCreated | null>(null);

  updateSignal(target: WritableSignal<string>, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    target.set(value);
  }

  onInviteRoleChange(event: Event): void {
    const value = (event.target as HTMLSelectElement)
      .value as PlatformInvitationRole;
    this.inviteRole.set(value);
  }

  async handleCreate(event: Event): Promise<void> {
    event.preventDefault();
    this.creating.set(true);
    this.errorMessage.set(null);

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

  async handleLookup(): Promise<void> {
    const institutionId = this.lookupId().trim();
    if (!institutionId) {
      this.errorMessage.set('Ingresa un UUID de institución.');
      return;
    }

    this.loadingInstitution.set(true);
    this.errorMessage.set(null);
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

    try {
      const invitation = await this.academyService.createInvitation(
        institution.id,
        {
          email: this.inviteEmail().trim(),
          role: this.inviteRole(),
        },
      );
      this.lastInvitation.set(invitation);
      this.inviteEmail.set('');
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.inviting.set(false);
    }
  }

  async copyInvitationUrl(): Promise<void> {
    const url = this.createdResult()?.invitation?.invitationUrl;
    if (!url || !navigator.clipboard?.writeText) {
      return;
    }

    await navigator.clipboard.writeText(url);
  }

  async handleLogout(): Promise<void> {
    await this.authService.logout();
    await this.router.navigate(['/login']);
  }
}
