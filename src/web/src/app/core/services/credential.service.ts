import { HttpClient } from '@angular/common/http';
import { Service, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';

import {
  IssueCredentialModel,
  IssuedCredential,
  MOCK_ISSUED_CREDENTIALS,
} from '../models/credential.models';

/**
 * Credential data layer prepared for real HTTP integration.
 *
 * `credentialsResource` uses rxResource — the ideal pattern for the Issuer
 * portal table once GET /api/credentials is available. Until then, local
 * overlay signals keep mock data editable for UI demos.
 */
@Service()
export class CredentialService {
  private readonly http = inject(HttpClient);

  private readonly localCredentials = signal<ReadonlyArray<IssuedCredential>>([
    ...MOCK_ISSUED_CREDENTIALS,
  ]);

  /**
   * Reactive credential list resource.
   * TODO: Replace mock stream with:
   *   this.http.get<ReadonlyArray<IssuedCredential>>('/api/credentials')
   */
  readonly credentialsResource = rxResource({
    stream: () => {
      // Placeholder until backend endpoint exists
      void this.http;
      return of([...this.localCredentials()] as IssuedCredential[]);
    },
  });

  /** Merges remote resource value with local overlay for mock/demo flows */
  readonly credentials = computed<ReadonlyArray<IssuedCredential>>(() => {
    const remote = this.credentialsResource.value();
    return remote ?? this.localCredentials();
  });

  readonly totalIssued = computed(() => this.credentials().length);

  readonly activeCount = computed(
    () => this.credentials().filter((c) => c.status === 'active').length,
  );

  readonly revokedCount = computed(
    () => this.credentials().filter((c) => c.status === 'revoked').length,
  );

  /** Adds a credential locally and reloads the resource stream */
  addCredential(model: IssueCredentialModel): IssuedCredential {
    const credential: IssuedCredential = {
      id: crypto.randomUUID(),
      student: model.student,
      documentType: model.documentType,
      issuedDate: model.issuedDate,
      status: 'active',
    };

    this.localCredentials.update((current) => [...current, credential]);
    this.credentialsResource.reload();

    return credential;
  }
}
