import { Injectable, inject, signal } from '@angular/core';
import { getAddress } from 'ethers';
import { SiweMessage } from 'siwe';

import { SEPOLIA_CHAIN_ID } from '../constants/auth.constants';
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
    jwt: null,
    address: null,
    expiresAt: null,
  });

  readonly authState$ = this.authState.asReadonly();

  private readonly storageType: StorageType = StorageType.LOCAL;

  constructor() {
    this.restoreAuthState();
  }

  /** Initiates SIWE login flow */
  async login(): Promise<void> {
    try {
      const walletAddress = await this.web3Service.connectWallet();
      if (!walletAddress) {
        throw new Error('Failed to connect wallet');
      }

      const { nonce } = await this.authApi.fetchNonce();
      const message = this.createSiweMessage(walletAddress, nonce);
      const signature = await this.signMessage(message);
      const verifyResponse = await this.authApi.verifySignature({
        message,
        signature,
      });

      this.setAuthState({
        isAuthenticated: true,
        jwt: verifyResponse.jwt,
        address: getAddress(verifyResponse.address),
        expiresAt: verifyResponse.expiresAt,
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
      jwt: null,
      address: null,
      expiresAt: null,
    });
    this.clearAuthState();
  }

  getAuthState(): AuthState {
    return this.authState();
  }

  isAuthenticated(): boolean {
    return this.authState().isAuthenticated;
  }

  getJwt(): string | null {
    return this.authState().jwt;
  }

  getAddress(): string | null {
    return this.authState().address;
  }

  private createSiweMessage(address: string, nonce: string): string {
    const message = new SiweMessage({
      domain: window.location.host,
      address: getAddress(address),
      statement: 'Sign in with Ethereum to the app',
      uri: window.location.origin,
      version: '1',
      chainId: SEPOLIA_CHAIN_ID,
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

    if (state.jwt && state.address && state.expiresAt) {
      storage.setItem('auth_jwt', state.jwt);
      storage.setItem('auth_address', state.address);
      storage.setItem('auth_expires_at', state.expiresAt);
      storage.setItem('auth_authenticated', 'true');
    }
  }

  private restoreAuthState(): void {
    const storage = this.getStorage();
    const jwt = storage.getItem('auth_jwt');
    const address = storage.getItem('auth_address');
    const expiresAt = storage.getItem('auth_expires_at');
    const isAuthenticated = storage.getItem('auth_authenticated') === 'true';

    if (!jwt || !address || !expiresAt || !isAuthenticated) {
      return;
    }

    if (this.isSessionExpired(expiresAt)) {
      this.clearAuthState();
      return;
    }

    this.setAuthState({
      isAuthenticated: true,
      jwt,
      address: getAddress(address),
      expiresAt,
    });
  }

  private isSessionExpired(expiresAt: string): boolean {
    const expiry = Date.parse(expiresAt);
    return Number.isNaN(expiry) || Date.now() >= expiry;
  }

  private clearAuthState(): void {
    const storage = this.getStorage();
    storage.removeItem('auth_jwt');
    storage.removeItem('auth_address');
    storage.removeItem('auth_expires_at');
    storage.removeItem('auth_authenticated');
    storage.removeItem('auth_token');
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
