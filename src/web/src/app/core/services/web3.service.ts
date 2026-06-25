import { Injectable, signal } from '@angular/core';
import { BrowserProvider, getAddress } from 'ethers';

import { AddEthereumChainParameter } from '../constants/auth.constants';

export interface EthereumProvider {
  request: (args: {
    method: string;
    params?: readonly unknown[];
  }) => Promise<unknown>;
  on: (eventName: string, callback: (args: unknown) => void) => void;
  removeListener: (
    eventName: string,
    callback: (args: unknown) => void,
  ) => void;
}

declare global {
  interface Window {
    ethereum?: EthereumProvider;
  }
}

/** MetaMask error code returned when the requested chain is not yet added */
const CHAIN_NOT_ADDED_ERROR_CODE = 4902;

function isStringArray(value: unknown): value is readonly string[] {
  return (
    Array.isArray(value) && value.every((item) => typeof item === 'string')
  );
}

function isChainNotAddedError(error: unknown): boolean {
  return (
    typeof error === 'object' &&
    error !== null &&
    'code' in error &&
    (error as { code: unknown }).code === CHAIN_NOT_ADDED_ERROR_CODE
  );
}

@Injectable({
  providedIn: 'root',
})
export class Web3Service {
  private readonly connectedAddress = signal<string | null>(null);

  readonly address = this.connectedAddress.asReadonly();

  private provider: BrowserProvider | null = null;

  constructor() {
    this.initializeProvider();
    this.setupAccountChangeListener();
    this.setupChainChangeListener();
  }

  isMetaMaskAvailable(): boolean {
    return typeof window !== 'undefined' && !!window.ethereum;
  }

  async connectWallet(): Promise<string | null> {
    if (!this.isMetaMaskAvailable()) {
      console.warn('MetaMask or Ethereum provider not detected');
      return null;
    }

    try {
      if (!this.provider) {
        this.provider = new BrowserProvider(window.ethereum!);
      }

      const accounts = await window.ethereum!.request({
        method: 'eth_requestAccounts',
      });

      if (!isStringArray(accounts) || accounts.length === 0) {
        return null;
      }

      const address = this.toChecksumAddress(accounts[0]);
      this.connectedAddress.set(address);
      return address;
    } catch (error: unknown) {
      console.error('Error connecting wallet:', error);
      return null;
    }
  }

  disconnectWallet(): void {
    this.connectedAddress.set(null);
  }

  getConnectedAddress(): string | null {
    return this.connectedAddress();
  }

  getProvider(): BrowserProvider | null {
    return this.provider;
  }

  /**
   * Reads the chain ID of the network the wallet is currently connected to.
   *
   * Queries `eth_chainId` directly instead of the ethers provider so the value
   * is never stale after a `wallet_switchEthereumChain` call.
   */
  async getChainId(): Promise<number> {
    if (!this.isMetaMaskAvailable()) {
      throw new Error('MetaMask or Ethereum provider not detected');
    }

    const chainIdHex = await window.ethereum!.request({
      method: 'eth_chainId',
    });

    if (typeof chainIdHex !== 'string') {
      throw new Error('Wallet returned an invalid chain ID.');
    }

    return Number.parseInt(chainIdHex, 16);
  }

  /**
   * Asks the wallet to switch to the target chain, adding it first when the
   * wallet does not know about it yet (EIP-3085 / error 4902).
   */
  async switchToChain(
    chainId: number,
    addParams?: AddEthereumChainParameter,
  ): Promise<void> {
    if (!this.isMetaMaskAvailable()) {
      throw new Error('MetaMask or Ethereum provider not detected');
    }

    const hexChainId = `0x${chainId.toString(16)}`;

    try {
      await window.ethereum!.request({
        method: 'wallet_switchEthereumChain',
        params: [{ chainId: hexChainId }],
      });
    } catch (error: unknown) {
      if (isChainNotAddedError(error) && addParams) {
        await window.ethereum!.request({
          method: 'wallet_addEthereumChain',
          params: [addParams],
        });
      } else {
        throw error;
      }
    } finally {
      this.resetProvider();
    }
  }

  private initializeProvider(): void {
    if (!this.isMetaMaskAvailable()) {
      return;
    }

    try {
      this.provider = new BrowserProvider(window.ethereum!);
    } catch (error: unknown) {
      console.error('Error initializing provider:', error);
    }
  }

  /** Rebuilds the provider so it re-detects the network after a chain change */
  private resetProvider(): void {
    this.provider = null;
    this.initializeProvider();
  }

  private setupAccountChangeListener(): void {
    if (!this.isMetaMaskAvailable()) {
      return;
    }

    window.ethereum!.on('accountsChanged', (accounts: unknown) => {
      if (!isStringArray(accounts) || accounts.length === 0) {
        this.connectedAddress.set(null);
        return;
      }

      this.connectedAddress.set(this.toChecksumAddress(accounts[0]));
    });
  }

  private setupChainChangeListener(): void {
    if (!this.isMetaMaskAvailable()) {
      return;
    }

    window.ethereum!.on('chainChanged', () => {
      this.resetProvider();
    });
  }

  /** Normalizes a raw wallet address to EIP-55 checksum format required by SIWE */
  private toChecksumAddress(rawAddress: string): string {
    return getAddress(rawAddress);
  }
}
