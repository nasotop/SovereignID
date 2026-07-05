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
}
