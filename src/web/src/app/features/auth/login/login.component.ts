import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { Web3Service } from '../../../core/services/web3.service';
import { toErrorMessage } from '../../../core/utils/error.utils';

type LoginState = 'idle' | 'loading' | 'success' | 'error';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-900 px-4">
      <div class="max-w-md w-full bg-gray-800 rounded-lg shadow-lg p-8">
        <!-- Header -->
        <div class="text-center mb-8">
          <h1 class="text-3xl font-bold text-white mb-2">SovereignID</h1>
          <p class="text-gray-400">Sign in with Ethereum</p>
        </div>

        <!-- MetaMask Check -->
        @if (!web3Service.isMetaMaskAvailable()) {
          <div
            class="mb-6 p-4 bg-red-900 border border-red-700 rounded-lg text-red-100 text-sm"
          >
            <p class="font-semibold">MetaMask not detected</p>
            <p class="mt-1">
              Please install MetaMask to sign in with your Ethereum wallet.
            </p>
          </div>
        }

        <!-- Loading State -->
        @if (state() === 'loading') {
          <div class="text-center">
            <div class="animate-spin mb-4">
              <svg
                class="w-12 h-12 text-blue-500 mx-auto"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  class="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  stroke-width="4"
                ></circle>
                <path
                  class="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
            </div>
            <p class="text-gray-300 font-medium">
              Please sign the message in your wallet...
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
              Welcome back!
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
            <p class="text-red-400 font-semibold mb-2">Sign in failed</p>
            <p class="text-gray-400 text-sm mb-4">
              {{ errorMessage() || 'Please try again' }}
            </p>
            <button
              (click)="resetState()"
              class="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 px-4 rounded-lg transition duration-200"
            >
              Try again
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
            <span>Connect with MetaMask</span>
          </button>
        }

        <!-- Footer -->
        <p class="text-center text-gray-500 text-xs mt-6">
          You will be asked to sign a message to verify your identity
        </p>
      </div>
    </div>
  `,
})
export class LoginComponent {
  readonly authService = inject(AuthService);
  readonly web3Service = inject(Web3Service);

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
    } catch (error: unknown) {
      this.state.set('error');
      this.errorMessage.set(toErrorMessage(error));
      console.error('Login error:', error);
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
