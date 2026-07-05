import { Component, computed, input, signal } from '@angular/core';

@Component({
  selector: 'app-copy-value',
  standalone: true,
  template: `
    <button
      type="button"
      class="inline-flex max-w-full items-center gap-2 rounded-md border border-slate-700 bg-slate-900/60 px-2 py-1 text-left font-mono text-xs text-slate-300 hover:border-slate-500 hover:text-white"
      [title]="value() || emptyLabel()"
      [disabled]="!value()"
      (click)="copy()"
    >
      <span class="truncate">{{ displayValue() }}</span>
      @if (value()) {
        <span class="shrink-0 text-[10px] uppercase text-slate-500">
          {{ copied() ? 'Copiado' : 'Copiar' }}
        </span>
      }
    </button>
  `,
})
export class CopyValueComponent {
  readonly value = input<string | null | undefined>('');
  readonly emptyLabel = input('-');
  readonly head = input(10);
  readonly tail = input(6);

  readonly copied = signal(false);

  readonly displayValue = computed(() => {
    const value = this.value();
    if (!value) {
      return this.emptyLabel();
    }

    const head = this.head();
    const tail = this.tail();
    if (value.length <= head + tail + 3) {
      return value;
    }

    return `${value.slice(0, head)}...${value.slice(-tail)}`;
  });

  async copy(): Promise<void> {
    const value = this.value();
    if (!value || !navigator.clipboard?.writeText) {
      return;
    }

    await navigator.clipboard.writeText(value);
    this.copied.set(true);
    window.setTimeout(() => this.copied.set(false), 1200);
  }
}
