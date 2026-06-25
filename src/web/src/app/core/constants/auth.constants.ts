/** Ethereum Sepolia testnet — sole chain accepted by auth v1 */
export const SEPOLIA_CHAIN_ID = 11155111;

/** EIP-3085 chain descriptor used by `wallet_addEthereumChain` */
export interface AddEthereumChainParameter {
  chainId: string;
  chainName: string;
  nativeCurrency: { name: string; symbol: string; decimals: number };
  rpcUrls: string[];
  blockExplorerUrls?: string[];
}

/** Sepolia network metadata so the wallet can add the chain if it is missing */
export const SEPOLIA_NETWORK_PARAMS: AddEthereumChainParameter = {
  chainId: `0x${SEPOLIA_CHAIN_ID.toString(16)}`,
  chainName: 'Sepolia',
  nativeCurrency: { name: 'Sepolia Ether', symbol: 'ETH', decimals: 18 },
  rpcUrls: ['https://rpc.sepolia.org'],
  blockExplorerUrls: ['https://sepolia.etherscan.io'],
};
