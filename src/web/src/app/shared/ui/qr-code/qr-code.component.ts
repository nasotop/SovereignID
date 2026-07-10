import { Component, effect, input, signal } from '@angular/core';
import QRCode from 'qrcode';

@Component({
  selector: 'app-qr-code',
  standalone: true,
  template: `
  @if (error()) {
    <p class="text-sm text-red-300" role="alert">{{ error() }}</p>
  } @else if (dataUrl()) {
    <img
      [src]="dataUrl()"
      [width]="size()"
      [height]="size()"
      [alt]="alt()"
      class="rounded-lg bg-white p-2"
    />
  } @else {
    <div
      class="flex items-center justify-center rounded-lg bg-slate-900/60 text-sm text-slate-400"
      [style.width.px]="size()"
      [style.height.px]="size()"
    >
      Generando QR...
    </div>
  }
  `,
})
export class QrCodeComponent {
  readonly value = input.required<string>();
  readonly size = input(220);
  readonly alt = input('Codigo QR de verificacion');

  readonly dataUrl = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const value = this.value().trim();
      if (!value) {
        this.dataUrl.set(null);
        this.error.set('No hay contenido para generar el codigo QR.');
        return;
      }

      void this.renderQr(value);
    });
  }

  private async renderQr(value: string): Promise<void> {
    this.error.set(null);
    this.dataUrl.set(null);

    try {
      const dataUrl = await QRCode.toDataURL(value, {
        width: this.size(),
        margin: 2,
        errorCorrectionLevel: 'M',
      });
      this.dataUrl.set(dataUrl);
    } catch {
      this.error.set('No se pudo generar el codigo QR.');
    }
  }
}
