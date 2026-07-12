import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';
import { Web3Service } from '../../../core/services/web3.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { BlockchainBackgroundComponent } from '../../../shared/ui/blockchain-background/blockchain-background.component';
import { HexLoaderComponent } from '../../../shared/ui/hex-loader/hex-loader.component';
import { LanguageSwitcherComponent } from '../../../shared/ui/language-switcher/language-switcher.component';

type LoginState = 'idle' | 'loading' | 'success' | 'error';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe, BlockchainBackgroundComponent, HexLoaderComponent, LanguageSwitcherComponent],
  template: `
    <div class="relative min-h-screen overflow-hidden bg-slate-950 px-4">
      <app-blockchain-background />
      <div class="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(14,165,233,0.14),transparent_42%),linear-gradient(180deg,rgba(2,6,23,0.2),rgba(2,6,23,0.88))]"></div>
      <div class="absolute right-4 top-4 z-20">
        <app-language-switcher variant="floating" />
      </div>

      <div class="relative z-10 flex min-h-screen items-center justify-center">
      <div class="w-full max-w-md rounded-lg border border-slate-700/80 bg-slate-900/88 p-8 shadow-2xl shadow-cyan-950/30 backdrop-blur-md">
        <!-- Header -->
        <div class="text-center mb-8">
          <h1 class="text-3xl font-bold text-white mb-2">SovereignID</h1>
          <p class="text-slate-400">{{ 'login.subtitle' | translate }}</p>
        </div>

        <!-- MetaMask Check -->
        @if (!web3Service.isMetaMaskAvailable()) {
          <div
            class="mb-6 p-4 bg-red-900 border border-red-700 rounded-lg text-red-100 text-sm"
          >
            <p class="font-semibold">{{ 'login.metamaskMissingTitle' | translate }}</p>
            <p class="mt-1">
              {{ 'login.metamaskMissingBody' | translate }}
            </p>
          </div>
        }

        <!-- Loading State -->
        @if (state() === 'loading') {
          <div class="text-center">
            <div class="mb-4 flex justify-center">
              <app-hex-loader size="lg" [label]="'login.waitingSignature' | translate" />
            </div>
            <p class="text-gray-300 font-medium">
              {{ 'login.signMessage' | translate }}
            </p>
          </div>
        }

        <!-- Success State -->
        @if (state() === 'success') {
          <div class="text-center">
            <div class="mb-4">
              <svg
                class="w-16 h-16 text-green-500 mx-auto"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M5 13l4 4L19 7"
                ></path>
              </svg>
            </div>
            <p class="text-green-400 font-semibold text-lg mb-2">
              {{ 'login.welcome' | translate }}
            </p>
            <p class="text-gray-400 text-sm break-all">
              {{ authService.getAddress() }}
            </p>
          </div>
        }

        <!-- Error State -->
        @if (state() === 'error') {
          <div class="text-center">
            <div class="mb-4">
              <svg
                class="w-16 h-16 text-red-500 mx-auto"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M6 18L18 6M6 6l12 12"
                ></path>
              </svg>
            </div>
            <p class="text-red-400 font-semibold mb-2">{{ 'login.failed' | translate }}</p>
            <p class="text-gray-400 text-sm mb-4">
              {{ errorMessage() || ('login.tryAgainMessage' | translate) }}
            </p>
            <button
              (click)="resetState()"
              class="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 px-4 rounded-lg transition duration-200"
            >
              {{ 'login.tryAgain' | translate }}
            </button>
          </div>
        }

        <!-- Idle / Login Button State -->
        @if (state() === 'idle') {
          <button
            (click)="handleLogin()"
            [disabled]="!web3Service.isMetaMaskAvailable()"
            [class.opacity-50]="!web3Service.isMetaMaskAvailable()"
            [class.cursor-not-allowed]="!web3Service.isMetaMaskAvailable()"
            class="w-full bg-orange-500 hover:bg-orange-600 disabled:hover:bg-orange-500 text-white font-bold py-3 px-4 rounded-lg transition duration-200 flex items-center justify-center gap-2"
          >
            <svg
              class="w-5 h-5"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 40 40"
              fill="currentColor"
            >
              <path
                d="M36.3 2H3.7A1.7 1.7 0 002 3.7v32.6A1.7 1.7 0 003.7 38h32.6a1.7 1.7 0 001.7-1.7V3.7A1.7 1.7 0 0036.3 2z"
              />
              <path
                d="M20 4l12 7v13l-12 7-12-7V11l12-7z"
                fill="white"
                opacity="0.6"
              />
            </svg>
            <span>{{ 'login.connect' | translate }}</span>
          </button>
        }

        <!-- Footer -->
        <div class="text-center mt-6 space-y-3">
          <p class="text-gray-500 text-xs">
            {{ 'login.disclaimer' | translate }}
          </p>
          @if (authService.hasPlatformAdmin()) {
            <a
              routerLink="/platform"
              class="inline-block text-sm text-blue-400 hover:text-blue-300 transition"
            >
              {{ 'login.platformPortal' | translate }}
            </a>
          }
        </div>
      </div>
      </div>
    </div>
  `,
})
export class LoginComponent {
  readonly authService = inject(AuthService);
  readonly web3Service = inject(Web3Service);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly state = signal<LoginState>('idle');
  readonly errorMessage = signal<string | null>(null);
  /**
   * Handles login button click
   */
  async handleLogin(): Promise<void> {
    this.state.set('loading');
    this.errorMessage.set(null);

    try {
      await this.authService.login();
      this.state.set('success');
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
      const destination = this.authService.resolvePostLoginUrl(returnUrl);
      await this.router.navigateByUrl(destination);
    } catch (error: unknown) {
      this.state.set('error');
      this.errorMessage.set(toErrorMessage(error));
      console.error(this.translate.instant('login.errorLog'), error);
    }
  }

  /**
   * Resets component state to idle
   */
  resetState(): void {
    this.state.set('idle');
    this.errorMessage.set(null);
  }
}
