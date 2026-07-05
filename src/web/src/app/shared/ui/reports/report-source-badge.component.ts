import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';

import { ReportSource } from '../../../core/models/reports.models';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';

@Component({
  selector: 'app-report-source-badge',
  standalone: true,
  imports: [CommonModule, StatusBadgeComponent],
  template: `
    <app-status-badge [label]="label()" [tone]="tone()" />
  `,
})
export class ReportSourceBadgeComponent {
  readonly source = input.required<ReportSource>();

  readonly label = computed(() => {
    switch (this.source()) {
      case 'snapshot':
        return 'Snapshot';
      case 'live':
        return 'Live';
      case 'hybrid':
        return 'Hibrido';
      default:
        return this.source();
    }
  });

  readonly tone = computed(() => {
    switch (this.source()) {
      case 'snapshot':
        return 'neutral' as const;
      case 'live':
        return 'info' as const;
      case 'hybrid':
        return 'warning' as const;
      default:
        return 'neutral' as const;
    }
  });
}
