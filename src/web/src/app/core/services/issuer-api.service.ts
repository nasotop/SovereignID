import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CredentialRevokedResponse,
  CredentialSummaryResponse,
  LinkStudentTitleRequest,
} from '../models/credential.models';

@Injectable({ providedIn: 'root' })
export class IssuerApiService {
  private readonly http = inject(HttpClient);

  listInstitutionCredentials(
    institutionId: string,
  ): Observable<ReadonlyArray<CredentialSummaryResponse>> {
    return this.http.get<ReadonlyArray<CredentialSummaryResponse>>(
      `/issuer/institutions/${institutionId}/credentials`,
    );
  }

  linkStudentTitle(
    studentId: string,
    request: LinkStudentTitleRequest,
  ): Observable<CredentialSummaryResponse> {
    return this.http.post<CredentialSummaryResponse>(
      `/issuer/students/${studentId}/title`,
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
      `/issuer/credentials/${credentialId}/revoke`,
      request,
    );
  }
}
