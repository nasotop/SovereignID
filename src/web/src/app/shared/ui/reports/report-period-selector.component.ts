import { CommonModule } from '@angular/common';
import { Component, input, output, signal } from '@angular/core';

import {
  ReportPeriodPreset,
  ReportPeriodRange,
  computeReportPeriod,
} from '../../../core/models/reports.models';

@Component({
  selector: 'app-report-period-selector',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-wrap items-center gap-2">
      <span class="text-xs font-medium uppercase text-slate-400">Periodo</span>
      @for (preset of presets; track preset) {
        <button
          type="button"
          class="rounded-lg border px-3 py-1.5 text-sm font-medium transition"
          [ngClass]="activePreset() === preset
            ? accentClass()
            : 'border-slate-600 bg-slate-800 text-slate-300 hover:border-slate-500 hover:text-white'"
          (click)="selectPreset(preset)"
        >
          {{ preset }}d
        </button>
      }
    </div>
  `,
})
export class ReportPeriodSelectorComponent {
  readonly accent = input<'blue' | 'violet'>('blue');
  readonly periodChange = output<ReportPeriodRange>();

  readonly presets: readonly ReportPeriodPreset[] = [7, 30, 90];
  readonly activePreset = signal<ReportPeriodPreset>(30);

  constructor() {
    this.emitCurrent();
  }

  selectPreset(preset: ReportPeriodPreset): void {
    this.activePreset.set(preset);
    this.emitCurrent();
  }

  accentClass(): string {
    return this.accent() === 'violet'
      ? 'border-violet-500 bg-violet-600/20 text-violet-200'
      : 'border-blue-500 bg-blue-600/20 text-blue-200';
  }

  private emitCurrent(): void {
    this.periodChange.emit(computeReportPeriod(this.activePreset()));
  }
}
