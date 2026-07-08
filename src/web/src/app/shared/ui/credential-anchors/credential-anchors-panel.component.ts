import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CopyValueComponent } from '../copy-value/copy-value.component';
import {
  buildSepoliaExplorerTxUrl,
  CredentialAnchorsData,
  isSepoliaChain,
} from './credential-anchors.types';

@Component({
  selector: 'app-credential-anchors-panel',
  standalone: true,
  imports: [CommonModule, CopyValueComponent, RouterLink],
  template: `
    <dl class="grid gap-3 text-sm">
      <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
        <dt class="text-slate-500">IPFS CID</dt>
        <dd>
          <app-copy-value [value]="anchors().ipfsCid" />
        </dd>
      </div>

      @if (anchors().ipfsGatewayUrl) {
        <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
          <dt class="text-slate-500">Gateway IPFS</dt>
          <dd>
            <a
              class="text-blue-400 hover:text-blue-300 break-all"
              [href]="anchors().ipfsGatewayUrl!"
              target="_blank"
              rel="noopener noreferrer"
            >
              Abrir en gateway
            </a>
          </dd>
        </div>
      }

      <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
        <dt class="text-slate-500">Content hash</dt>
        <dd>
          <app-copy-value [value]="anchors().contentHash" />
        </dd>
      </div>

      <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
        <dt class="text-slate-500">Transaction</dt>
        <dd class="flex flex-wrap items-center gap-2">
          <app-copy-value [value]="anchors().transactionHash" />
          @if (showExplorerLink()) {
            <a
              class="text-xs font-semibold text-blue-400 hover:text-blue-300"
              [href]="explorerUrl()"
              target="_blank"
              rel="noopener noreferrer"
            >
              Ver en Sepolia
            </a>
          }
        </dd>
      </div>

      <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
        <dt class="text-slate-500">Chain ID</dt>
        <dd class="text-slate-200">{{ anchors().chainId }}</dd>
      </div>

      @if (anchors().blockNumber != null) {
        <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
          <dt class="text-slate-500">Block</dt>
          <dd class="text-slate-200">{{ anchors().blockNumber }}</dd>
        </div>
      }

      @if (anchors().eip712Signature) {
        <div class="grid grid-cols-[8rem_1fr] items-center gap-2">
          <dt class="text-slate-500">Firma EIP-712</dt>
          <dd>
            <app-copy-value [value]="anchors().eip712Signature" />
          </dd>
        </div>
      }
    </dl>

    @if (verifyLink(); as link) {
      <div class="mt-4 border-t border-slate-700 pt-4">
        <a
          class="inline-flex items-center rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500"
          [routerLink]="link.path"
          [queryParams]="link.queryParams"
        >
          Verificar en portal público
        </a>
      </div>
    }
  `,
})
export class CredentialAnchorsPanelComponent {
  readonly anchors = input.required<CredentialAnchorsData>();
  readonly credentialId = input<string | null>(null);

  readonly showExplorerLink = computed(() =>
    isSepoliaChain(this.anchors().chainId),
  );

  readonly explorerUrl = computed(() =>
    buildSepoliaExplorerTxUrl(this.anchors().transactionHash),
  );

  readonly verifyLink = computed(() => {
    const credentialId = this.credentialId()?.trim();
    if (!credentialId) {
      return null;
    }

    return {
      path: '/verifier',
      queryParams: { credentialId },
    };
  });
}
