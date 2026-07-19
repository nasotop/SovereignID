import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import {
  CredentialsPerStudentReport,
  ReportPeriodRange,
  TimeSeriesReport,
  VerificationOutcomesReport,
  computeReportPeriod,
} from '../../../core/models/reports.models';
import { ReportsService } from '../../../core/services/reports.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { withMinimumVisualDelay } from '../../../core/utils/visual-delay.util';
import { HexLoaderComponent } from '../../../shared/ui/hex-loader/hex-loader.component';
import {
  ReportBarPoint,
  ReportChartComponent,
  ReportGroupedLinePoint,
  ReportLinePoint,
} from '../../../shared/ui/reports/report-chart.component';
import { ReportKpiStatComponent } from '../../../shared/ui/reports/report-kpi-stat.component';
import { ReportPeriodSelectorComponent } from '../../../shared/ui/reports/report-period-selector.component';
import { ReportSourceBadgeComponent } from '../../../shared/ui/reports/report-source-badge.component';

type SectionStatus = 'idle' | 'loading' | 'loaded' | 'error';

interface SectionState<T> {
  status: SectionStatus;
  data: T | null;
  error: string | null;
}

function idleSection<T>(): SectionState<T> {
  return { status: 'idle', data: null, error: null };
}

@Component({
  selector: 'app-academy-reports-tab',
  standalone: true,
  imports: [
    CommonModule,
    HexLoaderComponent,
    ReportPeriodSelectorComponent,
    ReportKpiStatComponent,
    ReportSourceBadgeComponent,
    ReportChartComponent,
    TranslatePipe,
  ],
  template: `
    <div class="flex flex-col gap-6">
      <app-report-period-selector accent="blue" (periodChange)="onPeriodChange($event)" />

      @if (initialLoading()) {
        <div class="flex items-center gap-3 rounded-lg border border-cyan-500/30 bg-cyan-500/10 p-4 text-sm text-cyan-100">
          <app-hex-loader size="sm" [label]="'academy.reports.loading' | translate" />
          <span>{{ 'academy.reports.loading' | translate }}</span>
        </div>
      }

      <section class="grid gap-3 md:grid-cols-3">
        @if (perStudent().status === 'loaded' && perStudent().data) {
          <app-report-kpi-stat
            [label]="'academy.reports.avgCredentialsPerStudent' | translate"
            [value]="perStudent().data!.averageCredentialsPerStudent"
            [decimals]="2"
          />
          <app-report-kpi-stat
            [label]="'academy.reports.activeStudents' | translate"
            [value]="perStudent().data!.totalStudents"
          />
          <app-report-kpi-stat
            [label]="'reports.totalCredentials' | translate"
            [value]="perStudent().data!.totalCredentials"
          />
        } @else {
          <ng-container [ngTemplateOutlet]="sectionState" [ngTemplateOutletContext]="{ section: perStudent(), retry: retryPerStudent }" />
        }
      </section>

      <section class="grid gap-6 xl:grid-cols-2">
        <article class="rounded-lg border border-slate-700 bg-slate-800 p-4">
          <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 class="text-base font-semibold text-white">{{ 'academy.reports.issued' | translate }}</h3>
              @if (issued().status === 'loaded' && issued().data) {
                <p class="text-sm text-slate-400">{{ 'reports.total' | translate:{ total: issued().data!.total } }}</p>
              }
            </div>
            @if (issued().status === 'loaded' && issued().data) {
              <app-report-source-badge [source]="issued().data!.source" />
            }
          </div>
          @if (issued().status === 'loaded' && issued().data) {
            <app-report-chart mode="line" [lineData]="issuedLineData()" />
          } @else {
            <ng-container [ngTemplateOutlet]="sectionState" [ngTemplateOutletContext]="{ section: issued(), retry: retryIssued }" />
          }
        </article>

        <article class="rounded-lg border border-slate-700 bg-slate-800 p-4">
          <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 class="text-base font-semibold text-white">{{ 'academy.reports.verifications' | translate }}</h3>
              @if (reads().status === 'loaded' && reads().data) {
                <p class="text-sm text-slate-400">{{ 'reports.total' | translate:{ total: reads().data!.total } }}</p>
              }
            </div>
            @if (reads().status === 'loaded' && reads().data) {
              <app-report-source-badge [source]="reads().data!.source" />
            }
          </div>
          @if (reads().status === 'loaded' && reads().data) {
            <app-report-chart mode="line" [lineData]="readsLineData()" />
          } @else {
            <ng-container [ngTemplateOutlet]="sectionState" [ngTemplateOutletContext]="{ section: reads(), retry: retryReads }" />
          }
        </article>

        <article class="rounded-lg border border-slate-700 bg-slate-800 p-4">
          <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 class="text-base font-semibold text-white">{{ 'academy.reports.revocations' | translate }}</h3>
              @if (revoked().status === 'loaded' && revoked().data) {
                <p class="text-sm text-slate-400">{{ 'reports.total' | translate:{ total: revoked().data!.total } }}</p>
              }
            </div>
            @if (revoked().status === 'loaded' && revoked().data) {
              <app-report-source-badge [source]="revoked().data!.source" />
            }
          </div>
          @if (revoked().status === 'loaded' && revoked().data) {
            <app-report-chart mode="line" [lineData]="revokedLineData()" />
          } @else {
            <ng-container [ngTemplateOutlet]="sectionState" [ngTemplateOutletContext]="{ section: revoked(), retry: retryRevoked }" />
          }
        </article>

        <article class="rounded-lg border border-slate-700 bg-slate-800 p-4">
          <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 class="text-base font-semibold text-white">{{ 'academy.reports.verificationOutcomes' | translate }}</h3>
              @if (outcomes().status === 'loaded' && outcomes().data) {
                <p class="text-sm text-slate-400">
                  {{ 'reports.validInvalid' | translate:{ valid: outcomes().data!.validTotal, invalid: outcomes().data!.invalidTotal } }}
                </p>
              }
            </div>
            @if (outcomes().status === 'loaded' && outcomes().data) {
              <app-report-source-badge [source]="outcomes().data!.source" />
            }
          </div>
          @if (outcomes().status === 'loaded' && outcomes().data) {
            <app-report-chart mode="grouped-line" [groupedLineData]="outcomesLineData()" />
          } @else {
            <ng-container [ngTemplateOutlet]="sectionState" [ngTemplateOutletContext]="{ section: outcomes(), retry: retryOutcomes }" />
          }
        </article>
      </section>
    </div>

    <ng-template #sectionState let-section="section" let-retry="retry">
      @if (section.status === 'loading') {
        <div class="flex min-h-40 flex-col items-center justify-center gap-4 py-8 text-center text-sm text-slate-400">
          <app-hex-loader [label]="'academy.reports.loadingSection' | translate" />
          <p>{{ 'common.loading' | translate }}</p>
        </div>
      } @else if (section.status === 'error') {
        <div class="rounded-lg border border-red-700/50 bg-red-900/30 p-4 text-sm text-red-100">
          <p>{{ section.error }}</p>
          <button
            type="button"
            class="mt-3 rounded-lg border border-red-500/40 px-3 py-1.5 text-xs font-semibold text-red-100 hover:bg-red-900/50"
            (click)="retry()"
          >
            {{ 'login.tryAgain' | translate }}
          </button>
        </div>
      }
    </ng-template>
  `,
})
export class AcademyReportsTabComponent implements OnChanges {
  @Input({ required: true }) institutionId!: string;

  private readonly reportsService = inject(ReportsService);

  readonly period = signal<ReportPeriodRange>(computeReportPeriod(30));
  readonly initialLoading = signal(false);

  readonly issued = signal<SectionState<TimeSeriesReport>>(idleSection());
  readonly reads = signal<SectionState<TimeSeriesReport>>(idleSection());
  readonly perStudent = signal<SectionState<CredentialsPerStudentReport>>(idleSection());
  readonly revoked = signal<SectionState<TimeSeriesReport>>(idleSection());
  readonly outcomes = signal<SectionState<VerificationOutcomesReport>>(idleSection());

  readonly issuedLineData = signal<readonly ReportLinePoint[]>([]);
  readonly readsLineData = signal<readonly ReportLinePoint[]>([]);
  readonly revokedLineData = signal<readonly ReportLinePoint[]>([]);
  readonly outcomesLineData = signal<readonly ReportGroupedLinePoint[]>([]);

  readonly retryIssued = (): void => {
    void this.loadIssued();
  };
  readonly retryReads = (): void => {
    void this.loadReads();
  };
  readonly retryPerStudent = (): void => {
    void this.loadPerStudent();
  };
  readonly retryRevoked = (): void => {
    void this.loadRevoked();
  };
  readonly retryOutcomes = (): void => {
    void this.loadOutcomes();
  };

  ngOnChanges(): void {
    void this.loadAll();
  }

  onPeriodChange(period: ReportPeriodRange): void {
    this.period.set(period);
    void this.loadAll();
  }

  private async loadAll(): Promise<void> {
    if (!this.institutionId) {
      return;
    }

    this.initialLoading.set(true);
    this.setAllLoading();

    await withMinimumVisualDelay(
      Promise.allSettled([
        this.loadIssued(),
        this.loadReads(),
        this.loadPerStudent(),
        this.loadRevoked(),
        this.loadOutcomes(),
      ]),
    );

    this.initialLoading.set(false);
  }

  private setAllLoading(): void {
    this.issued.set({ status: 'loading', data: null, error: null });
    this.reads.set({ status: 'loading', data: null, error: null });
    this.perStudent.set({ status: 'loading', data: null, error: null });
    this.revoked.set({ status: 'loading', data: null, error: null });
    this.outcomes.set({ status: 'loading', data: null, error: null });
  }

  private async loadIssued(): Promise<void> {
    this.issued.set({ status: 'loading', data: null, error: null });
    try {
      const { from, to } = this.period();
      const data = await withMinimumVisualDelay(
        this.reportsService.getCredentialsIssued(this.institutionId, from, to),
      );
      this.issued.set({ status: 'loaded', data, error: null });
      this.issuedLineData.set(this.toLineData(data));
    } catch (error: unknown) {
      this.issued.set({ status: 'error', data: null, error: toErrorMessage(error) });
    }
  }

  private async loadReads(): Promise<void> {
    this.reads.set({ status: 'loading', data: null, error: null });
    try {
      const { from, to } = this.period();
      const data = await withMinimumVisualDelay(
        this.reportsService.getCredentialReads(this.institutionId, from, to),
      );
      this.reads.set({ status: 'loaded', data, error: null });
      this.readsLineData.set(this.toLineData(data));
    } catch (error: unknown) {
      this.reads.set({ status: 'error', data: null, error: toErrorMessage(error) });
    }
  }

  private async loadPerStudent(): Promise<void> {
    this.perStudent.set({ status: 'loading', data: null, error: null });
    try {
      const asOf = this.period().to;
      const data = await withMinimumVisualDelay(
        this.reportsService.getCredentialsPerStudent(this.institutionId, asOf),
      );
      this.perStudent.set({ status: 'loaded', data, error: null });
    } catch (error: unknown) {
      this.perStudent.set({ status: 'error', data: null, error: toErrorMessage(error) });
    }
  }

  private async loadRevoked(): Promise<void> {
    this.revoked.set({ status: 'loading', data: null, error: null });
    try {
      const { from, to } = this.period();
      const data = await withMinimumVisualDelay(
        this.reportsService.getCredentialsRevoked(this.institutionId, from, to),
      );
      this.revoked.set({ status: 'loaded', data, error: null });
      this.revokedLineData.set(this.toLineData(data));
    } catch (error: unknown) {
      this.revoked.set({ status: 'error', data: null, error: toErrorMessage(error) });
    }
  }

  private async loadOutcomes(): Promise<void> {
    this.outcomes.set({ status: 'loading', data: null, error: null });
    try {
      const { from, to } = this.period();
      const data = await withMinimumVisualDelay(
        this.reportsService.getVerificationOutcomes(this.institutionId, from, to),
      );
      this.outcomes.set({ status: 'loaded', data, error: null });
      this.outcomesLineData.set(this.toGroupedLineData(data));
    } catch (error: unknown) {
      this.outcomes.set({ status: 'error', data: null, error: toErrorMessage(error) });
    }
  }

  private toLineData(report: TimeSeriesReport): readonly ReportLinePoint[] {
    return report.series.map((point) => ({
      time: point.date,
      value: point.value,
    }));
  }

  private toGroupedLineData(report: VerificationOutcomesReport): readonly ReportGroupedLinePoint[] {
    return report.series.flatMap((point) => [
      { time: point.date, value: point.valid, group: 'Valid' },
      { time: point.date, value: point.invalid, group: 'Invalid' },
    ]);
  }
}
