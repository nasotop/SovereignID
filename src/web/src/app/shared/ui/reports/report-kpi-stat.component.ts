import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';

@Component({
  selector: 'app-report-kpi-stat',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  template: `
    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
      <p class="text-xs text-slate-500">{{ label() }}</p>
      <p class="mt-2 text-2xl font-bold text-white">
        @if (decimals() !== null) {
          {{ value() | number: '1.0-' + decimals() }}
        } @else {
          {{ value() | number: '1.0-0' }}
        }
      </p>
    </article>
  `,
})
export class ReportKpiStatComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number>();
  readonly decimals = input<number | null>(null);
}
