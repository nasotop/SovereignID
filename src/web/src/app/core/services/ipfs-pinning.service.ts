import { Injectable } from '@angular/core';

import { IPFS_GATEWAY_BASE } from '../constants/issuer.constants';
import { sha256HexFromString } from '../utils/blockchain.utils';

export interface IpfsPinResult {
  readonly cid: string;
  readonly gatewayUrl: string;
  readonly contentHash: string;
}

@Injectable({ providedIn: 'root' })
export class IpfsPinningService {
  /**
   * Pins JSON content to IPFS.
   * Uses Pinata when `localStorage.sovereignid.pinata.jwt` is set; otherwise derives a dev CID.
   */
  async pinJson(document: unknown): Promise<IpfsPinResult> {
    const canonical = JSON.stringify(document);
    const contentHash = await sha256HexFromString(canonical);
    const pinataJwt =
      typeof localStorage !== 'undefined'
        ? localStorage.getItem('sovereignid.pinata.jwt')
        : null;

    if (pinataJwt) {
      return this.pinWithPinata(canonical, pinataJwt, contentHash);
    }

    const cid = `bafy${contentHash.slice(2, 14)}dev`;
    return {
      cid,
      gatewayUrl: `${IPFS_GATEWAY_BASE}/${cid}`,
      contentHash,
    };
  }

  private async pinWithPinata(
    canonicalJson: string,
    jwt: string,
    contentHash: string,
  ): Promise<IpfsPinResult> {
    const response = await fetch('https://api.pinata.cloud/pinning/pinJSONToIPFS', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${jwt}`,
      },
      body: JSON.stringify({
        pinataContent: JSON.parse(canonicalJson),
        pinataMetadata: { name: `sovereignid-${Date.now()}` },
      }),
    });

    if (!response.ok) {
      throw new Error('Pinata pinning failed.');
    }

    const body = (await response.json()) as { IpfsHash: string };
    return {
      cid: body.IpfsHash,
      gatewayUrl: `${IPFS_GATEWAY_BASE}/${body.IpfsHash}`,
      contentHash,
    };
  }
}
