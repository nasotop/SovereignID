import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { AuthService } from '../../../core/services/auth.service';
import { BlockchainBackgroundComponent } from '../../../shared/ui/blockchain-background/blockchain-background.component';
import { UserMenuComponent } from '../../../shared/ui/user-menu/user-menu.component';
import { PlatformInstitutionsTabComponent } from './platform-institutions-tab.component';
import { PlatformReportsTabComponent } from './platform-reports-tab.component';

type PlatformTab = 'institutions' | 'reports';

@Component({
  selector: 'app-platform',
  standalone: true,
  imports: [
    CommonModule,
    TranslatePipe,
    BlockchainBackgroundComponent,
    UserMenuComponent,
    PlatformInstitutionsTabComponent,
    PlatformReportsTabComponent,
  ],
  template: `
    <div class="app-glass-shell relative flex h-screen flex-col overflow-hidden bg-slate-950 text-slate-100">
      <app-blockchain-background />
      <div class="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(14,165,233,0.13),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(124,58,237,0.12),transparent_38%),linear-gradient(180deg,rgba(2,6,23,0.2),rgba(2,6,23,0.82))]"></div>

      <nav class="relative z-40 shrink-0 border-b border-slate-700/60 bg-slate-800/80 backdrop-blur-sm">
        <div class="mx-auto flex w-full max-w-none items-center justify-between px-6 py-4 2xl:px-8">
          <div class="flex items-center gap-3">
            <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-violet-600">
              <svg class="h-5 w-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
            </div>
            <div>
              <h1 class="text-lg font-bold tracking-tight text-white">SovereignID</h1>
              <p class="text-xs text-slate-400">{{ 'platform.portal' | translate }}</p>
            </div>
          </div>

          <app-user-menu role="platform_admin" (logout)="handleLogout()" />
        </div>
      </nav>

      <main class="relative z-10 mx-auto flex min-h-0 w-full max-w-none flex-1 flex-col gap-6 p-6 2xl:px-8">
        <div class="flex shrink-0 gap-2 border-b border-slate-700">
          <button
            type="button"
            class="border-b-2 px-4 py-3 text-sm font-medium"
            [ngClass]="activeTab() === 'institutions'
              ? 'border-violet-500 text-violet-300'
              : 'border-transparent text-slate-400 hover:text-white'"
            (click)="setActiveTab('institutions')"
          >
            {{ 'platform.tabs.institutions' | translate }}
          </button>
          <button
            type="button"
            class="border-b-2 px-4 py-3 text-sm font-medium"
            [ngClass]="activeTab() === 'reports'
              ? 'border-violet-500 text-violet-300'
              : 'border-transparent text-slate-400 hover:text-white'"
            (click)="setActiveTab('reports')"
          >
            {{ 'platform.tabs.reports' | translate }}
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
