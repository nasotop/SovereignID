import { Component } from '@angular/core';

@Component({
  selector: 'app-verifier',
  standalone: true,
  template: `
    <div class="min-h-screen bg-slate-900 flex flex-col">
      <!-- Centered Header -->
      <header class="pt-12 pb-8 px-6 text-center">
        <div
          class="w-16 h-16 rounded-2xl bg-blue-600 flex items-center justify-center mx-auto mb-5"
        >
          <svg
            class="w-9 h-9 text-white"
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
        <h1 class="text-3xl font-bold text-white mb-2">SovereignID</h1>
        <p class="text-slate-400 text-lg">Credential Verifier</p>
        <p class="text-slate-500 text-sm mt-2 max-w-md mx-auto">
          Public verification service — no account required
        </p>
      </header>

      <!-- Main Content -->
      <main class="flex-1 flex items-start justify-center px-6 pb-16">
        <div class="w-full max-w-2xl">
          <!-- Drag & Drop Zone -->
          <div
            class="border-2 border-dashed border-slate-600 hover:border-blue-500/60 rounded-2xl bg-slate-800/50 p-12 flex flex-col items-center justify-center text-center transition-colors cursor-pointer"
            role="button"
            tabindex="0"
            aria-label="Drag and drop a Verifiable Credential JSON file here"
          >
            <div
              class="w-20 h-20 rounded-full bg-slate-700/50 flex items-center justify-center mb-6"
            >
              <svg
                class="w-10 h-10 text-slate-400"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                aria-hidden="true"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="1.5"
                  d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
                />
              </svg>
            </div>
            <p class="text-white font-medium text-lg mb-2">
              Drag and drop a Verifiable Credential (.json) here
            </p>
            <p class="text-slate-400 text-sm max-w-sm">
              to verify its authenticity
            </p>
            <p class="text-slate-500 text-xs mt-4">
              or click to browse files
            </p>
          </div>

          <!-- Verify Button -->
          <button
            type="button"
            class="w-full mt-6 py-4 px-6 text-base font-semibold text-white bg-blue-600 hover:bg-blue-500 rounded-xl transition-colors flex items-center justify-center gap-3"
          >
            <svg
              class="w-5 h-5"
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
            Verify Credential
          </button>

          <p class="text-center text-slate-500 text-xs mt-6">
            Verification is performed against the blockchain-anchored issuer
            registry
          </p>
        </div>
      </main>
    </div>
  `,
})
export class VerifierComponent {}
