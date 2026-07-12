import { Injectable, inject, signal } from '@angular/core';
import { getAddress } from 'ethers';
import { SiweMessage } from 'siwe';

import { VerifyResponse } from '../../api/auth/models/verify-response';
import {
  SEPOLIA_CHAIN_ID,
  SEPOLIA_NETWORK_PARAMS,
} from '../constants/auth.constants';
import {
  AuthState,
  InstitutionMembership,
  StorageType,
} from '../models/auth.models';
import { toThrownError } from '../utils/error.utils';
import { extractAuthClaimsFromJwt } from '../utils/jwt-claims.util';
import { AuthApiService } from './auth-api.service';
import { Web3Service } from './web3.service';

const EMPTY_AUTH_STATE: AuthState = {
  isAuthenticated: false,
  jwt: null,
  address: null,
  expiresAt: null,
  userId: null,
  platformAdmin: false,
  holder: false,
  memberships: [],
};

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly authApi = inject(AuthApiService);
  private readonly web3Service = inject(Web3Service);

  private readonly authState = signal<AuthState>(EMPTY_AUTH_STATE);

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

      const chainId = await this.ensureSupportedChain();

      const { nonce } = await this.authApi.fetchNonce();
      const message = this.createSiweMessage(walletAddress, nonce, chainId);
      const signature = await this.signMessage(message);
      const verifyResponse = await this.authApi.verifySignature({
        message,
        signature,
      });

      this.applyVerifyResponse(verifyResponse);
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
    this.setAuthState(EMPTY_AUTH_STATE);
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

  getShortAddress(): string {
    const address = this.getAddress();
    if (!address) {
      try {
        return localStorage.getItem('sovereignid.language') === 'es' ? 'Sin wallet' : 'No wallet';
      } catch {
        return 'No wallet';
      }
    }

    return `${address.slice(0, 6)}...${address.slice(-4)}`;
  }

  getUserDisplayName(): string {
    return this.getShortAddress();
  }

  hasPlatformAdmin(): boolean {
    return this.authState().platformAdmin;
  }

  isHolder(): boolean {
    return this.authState().holder;
  }

  getMemberships(): readonly InstitutionMembership[] {
    return this.authState().memberships;
  }

  hasInstitutionRole(institutionId: string, roles: readonly string[]): boolean {
    const normalizedRoles = new Set(roles.map((role) => role.toLowerCase()));
    return this.getMemberships().some(
      (membership) =>
        membership.institutionId === institutionId &&
        normalizedRoles.has(membership.role.toLowerCase()),
    );
  }

  getDefaultPortalUrl(): string {
    if (this.hasPlatformAdmin()) {
      return '/platform';
    }

    if (
      this.getMemberships().some((membership) =>
        ['admin', 'issuer', 'viewer'].includes(membership.role.toLowerCase()),
      )
    ) {
      return '/academy';
    }

    if (this.isHolder()) {
      return '/holder';
    }

    return '/holder';
  }

  resolvePostLoginUrl(returnUrl: string | null): string {
    if (returnUrl && returnUrl.startsWith('/')) {
      return returnUrl;
    }

    return this.getDefaultPortalUrl();
  }

  private applyVerifyResponse(verifyResponse: VerifyResponse): void {
    const jwtClaims = extractAuthClaimsFromJwt(verifyResponse.jwt);
    const membershipsFromResponse = (verifyResponse.memberships ?? []).map(
      (membership) => ({
        institutionId: membership.institutionId,
        role: membership.role,
      }),
    );

    this.setAuthState({
      isAuthenticated: true,
      jwt: verifyResponse.jwt,
      address: getAddress(verifyResponse.address),
      expiresAt: verifyResponse.expiresAt,
      userId: verifyResponse.userId ?? jwtClaims?.userId ?? null,
      platformAdmin:
        Boolean(verifyResponse.platformAdmin) ||
        (jwtClaims?.platformAdmin ?? false),
      holder:
        Boolean(verifyResponse.holder) || (jwtClaims?.holder ?? false),
      memberships:
        membershipsFromResponse.length > 0
          ? membershipsFromResponse
          : [...(jwtClaims?.memberships ?? [])],
    });
  }

  /**
   * Ensures the wallet is on the supported chain, prompting an in-wallet
   * network switch when needed. Returns the active chain ID once confirmed.
   */
  private async ensureSupportedChain(): Promise<number> {
    let chainId = await this.web3Service.getChainId();
    if (chainId === SEPOLIA_CHAIN_ID) {
      return chainId;
    }

    await this.web3Service.switchToChain(
      SEPOLIA_CHAIN_ID,
      SEPOLIA_NETWORK_PARAMS,
    );

    chainId = await this.web3Service.getChainId();
    if (chainId !== SEPOLIA_CHAIN_ID) {
      throw new Error(
        `Unsupported network (chain ID ${chainId}). Please switch your wallet to Sepolia (chain ID ${SEPOLIA_CHAIN_ID}).`,
      );
    }

    return chainId;
  }

  private createSiweMessage(
    address: string,
    nonce: string,
    chainId: number,
  ): string {
    const message = new SiweMessage({
      domain: window.location.host,
      address: getAddress(address),
      statement: 'Sign in with Ethereum to the app',
      uri: window.location.origin,
      version: '1',
      chainId,
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
      storage.setItem('auth_user_id', state.userId ?? '');
      storage.setItem('auth_platform_admin', String(state.platformAdmin));
      storage.setItem('auth_holder', String(state.holder));
      storage.setItem(
        'auth_memberships',
        JSON.stringify(state.memberships),
      );
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

    const memberships = this.parseMemberships(storage.getItem('auth_memberships'));
    const jwtClaims = extractAuthClaimsFromJwt(jwt);
    const platformAdmin = this.resolvePlatformAdmin(
      storage.getItem('auth_platform_admin'),
      jwtClaims,
    );
    const holder = this.resolveHolder(storage.getItem('auth_holder'), jwtClaims);
    const userId = storage.getItem('auth_user_id') || jwtClaims?.userId || null;
    const resolvedMemberships =
      memberships.length > 0 ? memberships : [...(jwtClaims?.memberships ?? [])];

    this.setAuthState({
      isAuthenticated: true,
      jwt,
      address: getAddress(address),
      expiresAt,
      userId,
      platformAdmin,
      holder,
      memberships: resolvedMemberships,
    });

    if (jwtClaims) {
      this.saveAuthState();
    }
  }

  private resolvePlatformAdmin(
    stored: string | null,
    jwtClaims: ReturnType<typeof extractAuthClaimsFromJwt>,
  ): boolean {
    if (stored === 'true') {
      return true;
    }

    return jwtClaims?.platformAdmin ?? false;
  }

  private resolveHolder(
    stored: string | null,
    jwtClaims: ReturnType<typeof extractAuthClaimsFromJwt>,
  ): boolean {
    if (stored === 'true') {
      return true;
    }

    return jwtClaims?.holder ?? false;
  }

  private parseMemberships(raw: string | null): InstitutionMembership[] {
    if (!raw) {
      return [];
    }

    try {
      const parsed = JSON.parse(raw) as InstitutionMembership[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
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
    storage.removeItem('auth_user_id');
    storage.removeItem('auth_platform_admin');
    storage.removeItem('auth_holder');
    storage.removeItem('auth_memberships');
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
