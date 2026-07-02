import { Contract } from 'ethers';

import credentialRegistryAbi from '../contracts/credential-registry.abi.json';
import {
  CREDENTIAL_REGISTRY_CHAIN_ID,
  EIP712_DOMAIN_NAME,
  EIP712_DOMAIN_VERSION,
  resolveRegistryAddress,
} from '../constants/issuer.constants';
import { SEPOLIA_NETWORK_PARAMS } from '../constants/auth.constants';
import { guidToBytes32 } from '../utils/blockchain.utils';
import { Web3Service } from './web3.service';
import { Injectable, inject } from '@angular/core';

export interface RegisterCredentialParams {
  credentialId: string;
  institutionId: string;
  contentHash: string;
  ipfsCid: string;
  subjectWallet: string;
}

export interface ChainTransactionResult {
  transactionHash: string;
  blockNumber: number;
  eip712Signature: string;
}

@Injectable({ providedIn: 'root' })
export class CredentialContractService {
  private readonly web3Service = inject(Web3Service);

  async ensureInstitutionIssuerRegistered(institutionId: string): Promise<void> {
    const contract = await this.getWriteContract();
    const institutionBytes = guidToBytes32(institutionId);
    const onChainIssuer = await contract['institutionIssuers'](institutionBytes);

    if (onChainIssuer && onChainIssuer !== '0x0000000000000000000000000000000000000000') {
      return;
    }

    const tx = await contract['registerInstitutionIssuer'](institutionBytes);
    await tx.wait();
  }

  async registerCredentialOnChain(
    params: RegisterCredentialParams,
  ): Promise<ChainTransactionResult> {
    await this.web3Service.switchToChain(
      CREDENTIAL_REGISTRY_CHAIN_ID,
      SEPOLIA_NETWORK_PARAMS,
    );
    const contract = await this.getWriteContract();
    const signer = await this.getSigner();

    const credentialBytes = guidToBytes32(params.credentialId);
    const institutionBytes = guidToBytes32(params.institutionId);

    const signature = await signer.signTypedData(
      await this.buildDomain(contract),
      {
        CredentialIssuance: [
          { name: 'credentialId', type: 'bytes32' },
          { name: 'institutionId', type: 'bytes32' },
          { name: 'contentHash', type: 'bytes32' },
          { name: 'ipfsCid', type: 'string' },
          { name: 'subjectWallet', type: 'address' },
        ],
      },
      {
        credentialId: credentialBytes,
        institutionId: institutionBytes,
        contentHash: params.contentHash,
        ipfsCid: params.ipfsCid,
        subjectWallet: params.subjectWallet,
      },
    );

    const tx = await contract['registerCredential'](
      credentialBytes,
      institutionBytes,
      params.contentHash,
      params.ipfsCid,
      params.subjectWallet,
    );
    const receipt = await tx.wait();

    return {
      transactionHash: receipt?.hash ?? tx.hash,
      blockNumber: Number(receipt?.blockNumber ?? 0),
      eip712Signature: signature,
    };
  }

  async revokeCredentialOnChain(credentialId: string): Promise<ChainTransactionResult> {
    await this.web3Service.switchToChain(
      CREDENTIAL_REGISTRY_CHAIN_ID,
      SEPOLIA_NETWORK_PARAMS,
    );
    const contract = await this.getWriteContract();
    const signer = await this.getSigner();
    const credentialBytes = guidToBytes32(credentialId);

    const signature = await signer.signTypedData(
      await this.buildDomain(contract),
      {
        CredentialRevocation: [{ name: 'credentialId', type: 'bytes32' }],
      },
      { credentialId: credentialBytes },
    );

    const tx = await contract['revokeCredential'](credentialBytes);
    const receipt = await tx.wait();

    return {
      transactionHash: receipt?.hash ?? tx.hash,
      blockNumber: Number(receipt?.blockNumber ?? 0),
      eip712Signature: signature,
    };
  }

  private async getWriteContract(): Promise<Contract> {
    const signer = await this.getSigner();
    return new Contract(resolveRegistryAddress(), credentialRegistryAbi, signer);
  }

  private async getSigner() {
    const provider = this.web3Service.getProvider();
    if (!provider) {
      throw new Error('Wallet provider is not available.');
    }

    return provider.getSigner();
  }

  private async buildDomain(contract: Contract) {
    const network = await contract.runner?.provider?.getNetwork();
    return {
      name: EIP712_DOMAIN_NAME,
      version: EIP712_DOMAIN_VERSION,
      chainId: Number(network?.chainId ?? CREDENTIAL_REGISTRY_CHAIN_ID),
      verifyingContract: await contract.getAddress(),
    };
  }
}
