export const SEPOLIA_CHAIN_ID = 11155111;

export interface CredentialAnchorsData {
  ipfsCid: string;
  contentHash: string;
  transactionHash: string;
  chainId: number | string;
  blockNumber?: number | string | null;
  eip712Signature?: string | null;
  ipfsGatewayUrl?: string | null;
}

export function isSepoliaChain(chainId: number | string): boolean {
  return String(chainId) === String(SEPOLIA_CHAIN_ID);
}

export function buildSepoliaExplorerTxUrl(transactionHash: string): string {
  return `https://sepolia.etherscan.io/tx/${transactionHash}`;
}
