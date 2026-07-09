import { Component, input } from '@angular/core';

type HexLoaderSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-hex-loader',
  standalone: true,
  template: `
    <div class="hex-loader" [class]="sizeClass()" role="status" [attr.aria-label]="label()">
      <span class="hex-loader__ring"></span>
      <span class="hex-loader__core"></span>
    </div>
  `,
  styles: `
    .hex-loader {
      position: relative;
      display: inline-grid;
      place-items: center;
      color: rgb(103 232 249);
      filter: drop-shadow(0 0 16px rgb(34 211 238 / 0.18));
    }

    .hex-loader-sm {
      width: 2rem;
      height: 2rem;
    }

    .hex-loader-md {
      width: 3rem;
      height: 3rem;
    }

    .hex-loader-lg {
      width: 4rem;
      height: 4rem;
    }

    .hex-loader__ring,
    .hex-loader__core {
      position: absolute;
      clip-path: polygon(25% 4%, 75% 4%, 100% 50%, 75% 96%, 25% 96%, 0 50%);
    }

    .hex-loader__ring {
      inset: 0;
      background:
        linear-gradient(120deg, rgb(148 163 184 / 0.35), rgb(34 211 238), rgb(148 163 184 / 0.18));
      animation: hex-spin 1.2s linear infinite;
    }

    .hex-loader__ring::after {
      content: '';
      position: absolute;
      inset: 2px;
      clip-path: inherit;
      background: rgb(15 23 42);
    }

    .hex-loader__core {
      width: 42%;
      height: 42%;
      background: radial-gradient(circle, rgb(103 232 249 / 0.9), rgb(14 165 233 / 0.12) 68%);
      animation: hex-pulse 1.2s ease-in-out infinite;
    }

    @keyframes hex-spin {
      to {
        transform: rotate(360deg);
      }
    }

    @keyframes hex-pulse {
      0%,
      100% {
        opacity: 0.55;
        transform: scale(0.82);
      }
      50% {
        opacity: 1;
        transform: scale(1);
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .hex-loader__ring,
      .hex-loader__core {
        animation: none;
      }
    }
  `,
})
export class HexLoaderComponent {
  readonly size = input<HexLoaderSize>('md');
  readonly label = input('Cargando');

  sizeClass(): string {
    return `hex-loader-${this.size()}`;
  }
}
