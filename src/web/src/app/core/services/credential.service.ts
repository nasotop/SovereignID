import { Injectable, computed, inject, signal } from '@angular/core';

import { rxResource } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';

import { ISSUER_INSTITUTION_STORAGE_KEY } from '../constants/issuer.constants';
import {
  CredentialSummaryResponse,
  IssueCredentialModel,
  IssuedCredential,
} from '../models/credential.models';
import { IssuerApiService } from './issuer-api.service';
import { TitleIssuanceService } from './title-issuance.service';

@Injectable({ providedIn: 'root' })
export class CredentialService {
  private readonly issuerApiService = inject(IssuerApiService);
  private readonly titleIssuanceService = inject(TitleIssuanceService);

  readonly institutionId = signal(this.readInstitutionId());

  readonly credentialsResource = rxResource({
    params: () => ({ institutionId: this.institutionId() }),
    stream: ({ params }) => {
      if (!params.institutionId) {
        return of([] as CredentialSummaryResponse[]);
      }

      return this.issuerApiService.listInstitutionCredentials(params.institutionId);
    },
  });

  readonly credentials = computed<ReadonlyArray<IssuedCredential>>(() => {
    const remote = this.credentialsResource.value();
    if (!remote) {
      return [];
    }

    return remote.map(mapSummaryToIssuedCredential);
  });

  readonly totalIssued = computed(() => this.credentials().length);

  readonly activeCount = computed(
    () => this.credentials().filter((c) => c.status === 'active').length,
  );

  readonly revokedCount = computed(
    () => this.credentials().filter((c) => c.status === 'revoked').length,
  );

  setInstitutionId(institutionId: string): void {
    localStorage.setItem(ISSUER_INSTITUTION_STORAGE_KEY, institutionId);
    this.institutionId.set(institutionId);
    this.credentialsResource.reload();
  }

  async issueCredential(model: IssueCredentialModel): Promise<void> {
    await this.titleIssuanceService.issueTitle(model);
    this.credentialsResource.reload();
  }

  async revokeCredential(credentialId: string, reason: string): Promise<void> {
    await this.titleIssuanceService.revokeTitle(credentialId, reason);
    this.credentialsResource.reload();
  }

  private readInstitutionId(): string {
    return localStorage.getItem(ISSUER_INSTITUTION_STORAGE_KEY) ?? '';
  }
}

function mapSummaryToIssuedCredential(
  summary: CredentialSummaryResponse,
): IssuedCredential {
  return {
    credentialId: summary.credentialId,
    institutionId: summary.institutionId,
    studentId: summary.studentId,
    studentLabel: summary.studentLabel ?? summary.studentId,
    documentType: summary.credentialTypeCode,
    issuedDate: summary.issuedAt.slice(0, 10),
    status: summary.status === 'revoked' ? 'revoked' : 'active',
    ipfsGatewayUrl: summary.ipfsGatewayUrl,
  };
}
