import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { MOCK_HOLDER_CREDENTIALS } from '../../../core/models/credential.models';
import type { HolderCredential } from '../../../core/models/credential.models';

@Component({
  selector: 'app-holder',
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
              <p class="text-xs text-slate-400">Holder Portal</p>
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
          <h2 class="text-2xl font-bold text-white">My Credentials</h2>
          <p class="text-slate-400 mt-1">
            Your verifiable digital credentials issued by trusted institutions
          </p>
        </div>

        <!-- Credential Cards Grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          @for (credential of credentials(); track credential.id) {
            <div
              class="bg-slate-800 border border-slate-700 rounded-xl p-6 flex flex-col hover:border-slate-600 transition-colors"
            >
              <div class="flex items-start justify-between mb-5">
                <div
                  class="w-14 h-14 rounded-xl bg-blue-600/20 flex items-center justify-center"
                >
                  @if (credential.icon === 'degree') {
                    <svg
                      class="w-7 h-7 text-blue-400"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      aria-hidden="true"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="1.5"
                        d="M12 14l9-5-9-5-9 5 9 5zm0 0l6.16-3.422a12.083 12.083 0 01.665 6.479A11.952 11.952 0 0012 20.055a11.952 11.952 0 00-6.824-2.998 12.078 12.078 0 01.665-6.479L12 14zm-4 6v-7.5l4-2.222"
                      />
                    </svg>
                  } @else {
                    <svg
                      class="w-7 h-7 text-blue-400"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      aria-hidden="true"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="1.5"
                        d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                      />
                    </svg>
                  }
                </div>
                <span
                  class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-500/15 text-emerald-400 border border-emerald-500/30"
                >
                  <span class="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
                  Active
                </span>
              </div>

              <h3 class="text-lg font-semibold text-white mb-1">
                {{ credential.title }}
              </h3>
              <p class="text-sm text-slate-400 mb-1">
                Issued by {{ credential.issuer }}
              </p>
              <p class="text-xs text-slate-500 mb-6">
                {{ credential.issuedDate }}
              </p>

              <div class="flex gap-3 mt-auto">
                <button
                  type="button"
                  class="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-medium text-white bg-slate-700 hover:bg-slate-600 border border-slate-600 rounded-lg transition-colors"
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
                      d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
                    />
                  </svg>
                  Download JSON
                </button>
                <button
                  type="button"
                  class="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-500 rounded-lg transition-colors"
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
                      d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z"
                    />
                  </svg>
                  Share QR
                </button>
              </div>
            </div>
          }
        </div>
      </main>
    </div>
  `,
})
export class HolderComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly credentials = signal<ReadonlyArray<HolderCredential>>([
    ...MOCK_HOLDER_CREDENTIALS,
  ]);

  handleLogout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
