import { Injectable, signal } from '@angular/core';
import { BrowserProvider, getAddress } from 'ethers';

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

function isStringArray(value: unknown): value is readonly string[] {
  return (
    Array.isArray(value) && value.every((item) => typeof item === 'string')
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

  /** Normalizes a raw wallet address to EIP-55 checksum format required by SIWE */
  private toChecksumAddress(rawAddress: string): string {
    return getAddress(rawAddress);
  }
}
