import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { PlatformInstitutionsTabComponent } from './platform-institutions-tab.component';
import { PlatformReportsTabComponent } from './platform-reports-tab.component';

type PlatformTab = 'institutions' | 'reports';

@Component({
  selector: 'app-platform',
  standalone: true,
  imports: [
    CommonModule,
    PlatformInstitutionsTabComponent,
    PlatformReportsTabComponent,
  ],
  template: `
    <div class="flex h-screen flex-col overflow-hidden bg-slate-900 text-slate-100">
      <nav class="shrink-0 border-b border-slate-700/60 bg-slate-800/80 backdrop-blur-sm">
        <div class="mx-auto flex w-full max-w-none items-center justify-between px-6 py-4 2xl:px-8">
          <div class="flex items-center gap-3">
            <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-violet-600">
              <svg class="h-5 w-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
            </div>
            <div>
              <h1 class="text-lg font-bold tracking-tight text-white">SovereignID</h1>
              <p class="text-xs text-slate-400">Platform Portal</p>
            </div>
          </div>

          <button
            type="button"
            class="inline-flex items-center rounded-lg border border-slate-600 bg-slate-700/50 px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-slate-700 hover:text-white"
            (click)="handleLogout()"
          >
            Cerrar sesion
          </button>
        </div>
      </nav>

      <main class="mx-auto flex min-h-0 w-full max-w-none flex-1 flex-col gap-6 p-6 2xl:px-8">
        <div class="flex shrink-0 gap-2 border-b border-slate-700">
          <button
            type="button"
            class="border-b-2 px-4 py-3 text-sm font-medium"
            [ngClass]="activeTab() === 'institutions'
              ? 'border-violet-500 text-violet-300'
              : 'border-transparent text-slate-400 hover:text-white'"
            (click)="setActiveTab('institutions')"
          >
            Instituciones
          </button>
          <button
            type="button"
            class="border-b-2 px-4 py-3 text-sm font-medium"
            [ngClass]="activeTab() === 'reports'
              ? 'border-violet-500 text-violet-300'
              : 'border-transparent text-slate-400 hover:text-white'"
            (click)="setActiveTab('reports')"
          >
            Reportes
          </button>
        </div>

        @if (activeTab() === 'institutions') {
          <app-platform-institutions-tab />
        }
        @if (activeTab() === 'reports') {
          <app-platform-reports-tab />
        }
      </main>

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
export class PlatformComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly activeTab = signal<PlatformTab>('institutions');

  setActiveTab(tab: PlatformTab): void {
    this.activeTab.set(tab);
  }

  async handleLogout(): Promise<void> {
    await this.authService.logout();
    await this.router.navigate(['/login']);
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
