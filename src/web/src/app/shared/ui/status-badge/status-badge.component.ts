import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';

type BadgeTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span
      class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium"
      [ngClass]="toneClass()"
    >
      @if (dot()) {
        <span class="h-1.5 w-1.5 rounded-full" [ngClass]="dotClass()"></span>
      }
      {{ label() }}
    </span>
  `,
})
export class StatusBadgeComponent {
  readonly label = input.required<string>();
  readonly tone = input<BadgeTone>('neutral');
  readonly dot = input(false);

  toneClass(): string {
    switch (this.tone()) {
      case 'success':
        return 'border-emerald-500/30 bg-emerald-500/15 text-emerald-400';
      case 'warning':
        return 'border-amber-500/30 bg-amber-500/15 text-amber-300';
      case 'danger':
        return 'border-red-500/30 bg-red-500/15 text-red-400';
      case 'info':
        return 'border-blue-500/30 bg-blue-500/15 text-blue-300';
      default:
        return 'border-slate-500/40 bg-slate-500/20 text-slate-300';
    }
  }

  dotClass(): string {
    switch (this.tone()) {
      case 'success':
        return 'bg-emerald-400';
      case 'warning':
        return 'bg-amber-300';
      case 'danger':
        return 'bg-red-400';
      case 'info':
        return 'bg-blue-300';
      default:
        return 'bg-slate-300';
    }
  }
}
