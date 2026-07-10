import { Component, effect, ElementRef, input, output, signal, viewChild } from '@angular/core';

type ModalSize = 'sm' | 'md' | 'lg';

/**
 * Accessible modal built on the native HTML <dialog> element.
 * Reusable for maintainer forms and other overlay interactions.
 */
@Component({
  selector: 'app-modal',
  template: `
    <dialog
      #dialogElement
      class="modal-dialog"
      [class.modal-dialog-closing]="isClosing()"
      [attr.aria-labelledby]="titleId"
      (cancel)="onCancel($event)"
      (click)="onBackdropClick($event)"
    >
      <div class="modal-panel {{ panelSizeClass() }}">
        <header class="flex items-center justify-between mb-6">
          <div>
            <h2 [id]="titleId" class="text-xl font-semibold text-white">
              {{ title() }}
            </h2>
            @if (description()) {
              <p class="mt-1 text-sm text-slate-400">{{ description() }}</p>
            }
          </div>
          <button
            type="button"
            class="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-700 transition-colors"
            aria-label="Close dialog"
            (click)="close()"
          >
            <svg
              class="w-5 h-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </header>

        <ng-content />
      </div>
    </dialog>
  `,
  styles: `
    .modal-dialog {
      background: transparent;
      border: none;
      padding: 0;
      margin: auto;
      max-width: calc(100vw - 2rem);
      max-height: calc(100vh - 2rem);
      overflow: visible;
    }

    .modal-dialog::backdrop {
      background-color: rgb(15 23 42 / 0.75);
      backdrop-filter: blur(4px);
      animation: modal-backdrop-in 180ms ease-out;
    }

    .modal-panel {
      width: 100%;
      background-color: rgb(30 41 59);
      border: 1px solid rgb(51 65 85);
      border-radius: 0.75rem;
      padding: 1.5rem;
      box-shadow: 0 25px 50px -12px rgb(0 0 0 / 0.5);
      animation: modal-panel-in 220ms cubic-bezier(0.16, 1, 0.3, 1);
      transform-origin: center top;
      will-change: opacity, transform;
    }

    .modal-panel-sm {
      max-width: 24rem;
    }

    .modal-panel-md {
      max-width: 32rem;
    }

    .modal-panel-lg {
      max-width: 42rem;
    }

    .modal-dialog-closing::backdrop {
      animation: modal-backdrop-out 140ms ease-in forwards;
    }

    .modal-dialog-closing .modal-panel {
      animation: modal-panel-out 140ms ease-in forwards;
    }

    @keyframes modal-backdrop-in {
      from {
        opacity: 0;
      }
      to {
        opacity: 1;
      }
    }

    @keyframes modal-backdrop-out {
      from {
        opacity: 1;
      }
      to {
        opacity: 0;
      }
    }

    @keyframes modal-panel-in {
      from {
        opacity: 0;
        transform: translateY(10px) scale(0.98);
      }
      to {
        opacity: 1;
        transform: translateY(0) scale(1);
      }
    }

    @keyframes modal-panel-out {
      from {
        opacity: 1;
        transform: translateY(0) scale(1);
      }
      to {
        opacity: 0;
        transform: translateY(8px) scale(0.985);
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .modal-dialog::backdrop,
      .modal-dialog-closing::backdrop,
      .modal-panel,
      .modal-dialog-closing .modal-panel {
        animation: none;
      }
    }
  `,
})
export class ModalComponent {
  readonly isOpen = input.required<boolean>();
  readonly title = input.required<string>();
  readonly description = input('');
  readonly size = input<ModalSize>('md');
  readonly closed = output<void>();

  readonly titleId = `modal-title-${crypto.randomUUID()}`;
  readonly isClosing = signal(false);

  private readonly dialogRef =
    viewChild.required<ElementRef<HTMLDialogElement>>('dialogElement');
  private closeTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const dialog = this.dialogRef().nativeElement;

      if (this.isOpen()) {
        this.cancelPendingClose();
        this.isClosing.set(false);
        if (!dialog.open) {
          dialog.showModal();
        }
        return;
      }

      if (dialog.open) {
        this.closeWithAnimation(dialog);
      }
    });
  }

  close(): void {
    this.closed.emit();
  }

  onCancel(event: Event): void {
    event.preventDefault();
    this.close();
  }

  onBackdropClick(event: MouseEvent): void {
    const dialog = this.dialogRef().nativeElement;

    if (event.target === dialog) {
      this.close();
    }
  }

  panelSizeClass(): string {
    switch (this.size()) {
      case 'sm':
        return 'modal-panel-sm';
      case 'lg':
        return 'modal-panel-lg';
      default:
        return 'modal-panel-md';
    }
  }

  private closeWithAnimation(dialog: HTMLDialogElement): void {
    if (this.isClosing()) {
      return;
    }

    this.isClosing.set(true);
    this.closeTimer = setTimeout(() => {
      if (dialog.open) {
        dialog.close();
      }
      this.isClosing.set(false);
      this.closeTimer = null;
    }, 150);
  }

  private cancelPendingClose(): void {
    if (!this.closeTimer) {
      return;
    }

    clearTimeout(this.closeTimer);
    this.closeTimer = null;
  }
}
