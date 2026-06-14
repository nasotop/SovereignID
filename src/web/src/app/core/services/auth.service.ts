import { Injectable, inject, signal } from '@angular/core';
import { SiweMessage } from 'siwe';

import { AuthState, StorageType } from '../models/auth.models';
import { toThrownError } from '../utils/error.utils';
import { AuthApiService } from './auth-api.service';
import { Web3Service } from './web3.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly authApi = inject(AuthApiService);
  private readonly web3Service = inject(Web3Service);

  private readonly authState = signal<AuthState>({
    isAuthenticated: false,
    token: null,
    address: null,
  });

  readonly authState$ = this.authState.asReadonly();

  private readonly storageType: StorageType = StorageType.LOCAL;

  constructor() {
    this.restoreAuthState();
  }

  /** Initiates SIWE login flow */
  async login(): Promise<void> {
    try {
      const address = await this.web3Service.connectWallet();
      if (!address) {
        throw new Error('Failed to connect wallet');
      }

      const { nonce } = await this.authApi.fetchNonce();
      const message = this.createSiweMessage(address, nonce);
      const signature = await this.signMessage(message);
      const { token } = await this.authApi.verifySignature({ message, signature });

      this.setAuthState({
        isAuthenticated: true,
        token,
        address,
      });

      this.saveAuthState();
    } catch (error: unknown) {
      console.error('Login failed:', error);
      this.logout();
      throw toThrownError(error, 'Login failed');
    }
  }

  /** Logs out the user and clears auth state */
  logout(): void {
    this.web3Service.disconnectWallet();
    this.setAuthState({
      isAuthenticated: false,
      token: null,
      address: null,
    });
    this.clearAuthState();
  }

  getAuthState(): AuthState {
    return this.authState();
  }

  isAuthenticated(): boolean {
    return this.authState().isAuthenticated;
  }

  getToken(): string | null {
    return this.authState().token;
  }

  getAddress(): string | null {
    return this.authState().address;
  }

  private createSiweMessage(address: string, nonce: string): string {
    const message = new SiweMessage({
      domain: window.location.host,
      address,
      statement: 'Sign in with Ethereum to the app',
      uri: window.location.origin,
      version: '1',
      chainId: 1,
      nonce,
      issuedAt: new Date().toISOString(),
    });

    return message.prepareMessage();
  }

  private async signMessage(message: string): Promise<string> {
    try {
      const provider = this.web3Service.getProvider();
      if (!provider) {
        throw new Error('Provider not initialized');
      }

      const signer = await provider.getSigner();
      return signer.signMessage(message);
    } catch (error: unknown) {
      console.error('Message signing failed:', error);
      throw toThrownError(error, 'Failed to sign message');
    }
  }

  private setAuthState(state: AuthState): void {
    this.authState.set(state);
  }

  private saveAuthState(): void {
    const state = this.authState();
    const storage = this.getStorage();

    if (state.token) {
      storage.setItem('auth_token', state.token);
      storage.setItem('auth_address', state.address ?? '');
      storage.setItem('auth_authenticated', 'true');
    }
  }

  private restoreAuthState(): void {
    const storage = this.getStorage();
    const token = storage.getItem('auth_token');
    const address = storage.getItem('auth_address');
    const isAuthenticated = storage.getItem('auth_authenticated') === 'true';

    if (token && address && isAuthenticated) {
      this.setAuthState({
        isAuthenticated: true,
        token,
        address,
      });
    }
  }

  private clearAuthState(): void {
    const storage = this.getStorage();
    storage.removeItem('auth_token');
    storage.removeItem('auth_address');
    storage.removeItem('auth_authenticated');
  }

  private getStorage(): Storage {
    if (typeof window === 'undefined') {
      throw new Error('Storage is not available');
    }

    return this.storageType === StorageType.SESSION
      ? window.sessionStorage
      : window.localStorage;
  }
}
