import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { CREDENTIAL_REGISTRY_CHAIN_ID } from '../constants/issuer.constants';
import { IssueCredentialModel } from '../models/credential.models';
import { CredentialContractService } from './credential-contract.service';
import { IpfsPinningService } from './ipfs-pinning.service';
import { IssuerApiService } from './issuer-api.service';
import { VcDocumentService } from './vc-document.service';

@Injectable({ providedIn: 'root' })
export class TitleIssuanceService {
  private readonly vcDocumentService = inject(VcDocumentService);
  private readonly ipfsPinningService = inject(IpfsPinningService);
  private readonly credentialContractService = inject(CredentialContractService);
  private readonly issuerApiService = inject(IssuerApiService);

  async issueTitle(model: IssueCredentialModel): Promise<void> {
    const credentialId = crypto.randomUUID();
    const document = this.vcDocumentService.buildTitleCredential(model);
    const pin = await this.ipfsPinningService.pinJson(document);

    await this.credentialContractService.ensureInstitutionIssuerRegistered(
      model.institutionId,
    );

    const chain = await this.credentialContractService.registerCredentialOnChain({
      credentialId,
      institutionId: model.institutionId,
      contentHash: pin.contentHash,
      ipfsCid: pin.cid,
      subjectWallet: model.subjectWallet,
    });

    await firstValueFrom(
      this.issuerApiService.linkStudentTitle(model.studentId, {
        credentialId,
        careerId: model.careerId || undefined,
        credentialTypeCode: model.documentType,
        ipfsCid: pin.cid,
        ipfsGatewayUrl: pin.gatewayUrl,
        contentHash: pin.contentHash,
        transactionHash: chain.transactionHash,
        blockNumber: chain.blockNumber,
        chainId: CREDENTIAL_REGISTRY_CHAIN_ID,
        eip712Signature: chain.eip712Signature,
        metadata: {
          studentLabel: model.studentLabel,
          issuedDate: model.issuedDate,
        },
      }),
    );
  }

  async revokeTitle(credentialId: string, reason: string): Promise<void> {
    const chain = await this.credentialContractService.revokeCredentialOnChain(
      credentialId,
    );

    await firstValueFrom(
      this.issuerApiService.revokeCredential(credentialId, {
        reason,
        revocationTxHash: chain.transactionHash,
        blockNumber: chain.blockNumber,
        chainId: CREDENTIAL_REGISTRY_CHAIN_ID,
        eip712Signature: chain.eip712Signature,
      }),
    );
  }
}
