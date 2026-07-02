import { Injectable } from '@angular/core';

import {
  IssueCredentialModel,
  VerifiableCredentialDocument,
} from '../models/credential.models';

@Injectable({ providedIn: 'root' })
export class VcDocumentService {
  buildTitleCredential(input: IssueCredentialModel): VerifiableCredentialDocument {
    return {
      '@context': [
        'https://www.w3.org/2018/credentials/v1',
        'https://www.w3.org/2018/credentials/examples/v1',
      ],
      type: ['VerifiableCredential', 'UniversityDegreeCredential'],
      issuer: input.issuerDid,
      issuanceDate: new Date(input.issuedDate).toISOString(),
      credentialSubject: {
        id: input.subjectDid,
        degree: input.documentType,
        studentLabel: input.studentLabel,
        careerId: input.careerId || undefined,
      },
      credentialSchema: {
        id: 'https://sovereignid.local/schemas/titulo-v1.json',
        type: 'JsonSchemaValidator2018',
      },
    };
  }
}
