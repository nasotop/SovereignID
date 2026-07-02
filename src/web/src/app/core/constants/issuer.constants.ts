import { SEPOLIA_CHAIN_ID } from './auth.constants';

import sepoliaDeployment from '../contracts/credential-registry.sepolia.json';

export interface CredentialRegistryDeployment {
  readonly contractName: string;
  readonly address: string;
  readonly chainId: number;
  readonly network: string;
}

export const DEFAULT_REGISTRY_DEPLOYMENT: CredentialRegistryDeployment =
  sepoliaDeployment as CredentialRegistryDeployment;

/** Active chain for credential registry (Sepolia in production). */
export const CREDENTIAL_REGISTRY_CHAIN_ID = SEPOLIA_CHAIN_ID;

/** Override via localStorage key `sovereignid.registry.address` after Sepolia deploy. */
export function resolveRegistryAddress(
  deployment: CredentialRegistryDeployment = DEFAULT_REGISTRY_DEPLOYMENT,
): string {
  if (typeof localStorage === 'undefined') {
    return deployment.address;
  }

  return localStorage.getItem('sovereignid.registry.address') ?? deployment.address;
}

export const CREDENTIAL_TYPE_TITULO = 'TITULO';

export const IPFS_GATEWAY_BASE = 'https://ipfs.io/ipfs';

export const ISSUER_INSTITUTION_STORAGE_KEY = 'sovereignid.issuer.institutionId';

export const EIP712_DOMAIN_NAME = 'SovereignID';
export const EIP712_DOMAIN_VERSION = '1';
