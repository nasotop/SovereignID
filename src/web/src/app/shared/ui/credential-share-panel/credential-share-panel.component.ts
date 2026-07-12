import { CommonModule } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { HolderService } from '../../../core/services/holder.service';
import { buildVerifierShareUrl } from '../../../core/utils/verifier-share-url.util';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { CopyValueComponent } from '../copy-value/copy-value.component';
import { QrCodeComponent } from '../qr-code';

@Component({
  selector: 'app-credential-share-panel',
  standalone: true,
  imports: [CommonModule, CopyValueComponent, QrCodeComponent, TranslatePipe],
  template: `
    <div class="space-y-5">
      @if (credentialTitle()) {
        <p class="text-sm text-slate-300">
          {{ 'holder.shareIntroPrefix' | translate }}
          <span class="font-semibold text-white">{{ credentialTitle() }}</span>
          {{ 'holder.shareIntroSuffix' | translate }}
        </p>
      }

      <div class="flex flex-col items-center gap-4 rounded-lg border border-slate-700 bg-slate-900/50 p-5">
        <app-qr-code [value]="shareUrl()" [alt]="qrAlt()" />
        <p class="text-center text-sm text-slate-400">
          {{ 'holder.shareQrHint' | translate }}
        </p>
      </div>

      <div class="space-y-2">
        <p class="text-xs font-semibold uppercase text-slate-500">
          {{ 'holder.verificationLink' | translate }}
        </p>
        <app-copy-value [value]="shareUrl()" [head]="24" [tail]="12" />
      </div>

      <div class="flex flex-wrap gap-3 border-t border-slate-700 pt-4">
        <button
          type="button"
          class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500"
          (click)="copyShareLink()"
        >
          {{ 'holder.copyLink' | translate }}
        </button>
        <button
          type="button"
          class="rounded-lg border border-slate-600 px-4 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-700"
          (click)="copyCredentialId()"
        >
          {{ 'holder.copyUuidOnly' | translate }}
        </button>
      </div>
    </div>
  `,
})
export class CredentialSharePanelComponent {
  private readonly holderService = inject(HolderService);

  readonly credentialId = input.required<string>();
  readonly credentialTitle = input<string | null>(null);

  readonly linkCopied = output<void>();
  readonly idCopied = output<void>();
  readonly copyError = output<string>();

  readonly shareUrl = computed(() =>
    buildVerifierShareUrl(this.credentialId()),
  );

  readonly qrAlt = computed(
    () => `QR code for credential ${this.credentialId()}`,
  );

  async copyShareLink(): Promise<void> {
    try {
      await this.holderService.shareVerifierLink(this.credentialId());
      this.linkCopied.emit();
    } catch (error: unknown) {
      this.copyError.emit(toErrorMessage(error));
    }
  }

  async copyCredentialId(): Promise<void> {
    try {
      await this.holderService.shareCredentialId(this.credentialId());
      this.idCopied.emit();
    } catch (error: unknown) {
      this.copyError.emit(toErrorMessage(error));
    }
  }
}
