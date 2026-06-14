import { Component, effect, ElementRef, input, output, viewChild } from '@angular/core';

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
      [attr.aria-labelledby]="titleId"
      (cancel)="onCancel($event)"
      (click)="onBackdropClick($event)"
    >
      <div class="modal-panel">
        <header class="flex items-center justify-between mb-6">
          <h2 [id]="titleId" class="text-xl font-semibold text-white">
            {{ title() }}
          </h2>
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
    }

    .modal-panel {
      width: 100%;
      max-width: 32rem;
      background-color: rgb(30 41 59);
      border: 1px solid rgb(51 65 85);
      border-radius: 0.75rem;
      padding: 1.5rem;
      box-shadow: 0 25px 50px -12px rgb(0 0 0 / 0.5);
    }
  `,
})
export class ModalComponent {
  readonly isOpen = input.required<boolean>();
  readonly title = input.required<string>();
  readonly closed = output<void>();

  readonly titleId = `modal-title-${crypto.randomUUID()}`;

  private readonly dialogRef =
    viewChild.required<ElementRef<HTMLDialogElement>>('dialogElement');

  constructor() {
    effect(() => {
      const dialog = this.dialogRef().nativeElement;

      if (this.isOpen()) {
        if (!dialog.open) {
          dialog.showModal();
        }
        return;
      }

      if (dialog.open) {
        dialog.close();
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
}
