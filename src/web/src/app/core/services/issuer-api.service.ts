import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { BFF_API_BASE } from '../constants/api.constants';
import {
  CredentialRevokedResponse,
  CredentialSummaryResponse,
  LinkStudentTitleRequest,
} from '../models/credential.models';

@Injectable({ providedIn: 'root' })
export class IssuerApiService {
  private readonly http = inject(HttpClient);

  linkInstitutionIssuerWallet(
    institutionId: string,
    request: {
      walletAddress: string;
      did: string;
      publicKey?: string | null;
    },
  ): Observable<{ institutionId: string; walletAddress: string; did: string }> {
    return this.http.post<{ institutionId: string; walletAddress: string; did: string }>(
      `${BFF_API_BASE}/issuer/institutions/${institutionId}/wallet`,
      request,
    );
  }

  listInstitutionCredentials(
    institutionId: string,
  ): Observable<ReadonlyArray<CredentialSummaryResponse>> {
    return this.http.get<ReadonlyArray<CredentialSummaryResponse>>(
      `${BFF_API_BASE}/issuer/institutions/${institutionId}/credentials`,
    );
  }

  linkStudentTitle(
    studentId: string,
    request: LinkStudentTitleRequest,
  ): Observable<CredentialSummaryResponse> {
    return this.http.post<CredentialSummaryResponse>(
      `${BFF_API_BASE}/issuer/students/${studentId}/title`,
      request,
    );
  }

  revokeCredential(
    credentialId: string,
    request: {
      reason: string;
      revocationTxHash: string;
      blockNumber: number;
      chainId?: number;
      eip712Signature: string;
    },
  ): Observable<CredentialRevokedResponse> {
    return this.http.post<CredentialRevokedResponse>(
      `${BFF_API_BASE}/issuer/credentials/${credentialId}/revoke`,
      request,
    );
  }
}
