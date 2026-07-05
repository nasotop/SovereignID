import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { PlatformUnauthorizedError } from '../../../core/services/academy.service';
import {
  ReportPeriodRange,
  ReportSource,
  computeReportPeriod,
} from '../../../core/models/reports.models';
import { ReportsService } from '../../../core/services/reports.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import {
  ReportBarPoint,
  ReportChartComponent,
} from '../../../shared/ui/reports/report-chart.component';
import { ReportPeriodSelectorComponent } from '../../../shared/ui/reports/report-period-selector.component';
import { ReportSourceBadgeComponent } from '../../../shared/ui/reports/report-source-badge.component';

type ReportSectionState = 'loading' | 'loaded' | 'error';

@Component({
  selector: 'app-platform-reports-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReportPeriodSelectorComponent,
    ReportChartComponent,
    ReportSourceBadgeComponent,
  ],
  template: `
    <div class="flex flex-col gap-6">
      <header class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <h2 class="text-2xl font-bold text-white">Reportes</h2>
          <p class="mt-1 text-sm text-slate-400">
            Rankings cross-tenant de credenciales emitidas y alumnos activos por institucion.
          </p>
        </div>
        <app-report-period-selector accent="violet" (periodChange)="onPeriodChange($event)" />
      </header>

      <section class="rounded-lg border border-slate-700 bg-slate-800 p-6">
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 class="text-base font-semibold text-white">Credenciales por institucion</h3>
            <p class="mt-1 text-xs text-slate-400">
              Ranking de emisiones en el periodo seleccionado (R-P1).
            </p>
          </div>
          @if (credentialsState() === 'loaded' && credentialsSource()) {
            <app-report-source-badge [source]="credentialsSource()!" />
          }
        </div>

        @if (credentialsState() === 'loading') {
          <div class="flex h-64 items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 text-sm text-slate-400">
            Cargando reporte de credenciales...
          </div>
        } @else if (credentialsState() === 'error') {
          <div class="rounded-lg border border-red-700 bg-red-900/40 p-4">
            <p class="text-sm text-red-100">{{ credentialsError() }}</p>
            <button
              type="button"
              class="mt-3 inline-flex items-center rounded-lg border border-red-500/50 bg-red-900/60 px-3 py-1.5 text-sm font-medium text-red-100 transition hover:bg-red-900"
              (click)="retryCredentials()"
            >
              Reintentar
            </button>
          </div>
        } @else {
          @if (credentialsBarData().length) {
            <app-report-chart mode="bar-horizontal" [barData]="credentialsBarData()" />
          } @else {
            <div class="flex h-64 items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 text-sm text-slate-400">
              No hay datos de credenciales para el periodo seleccionado.
            </div>
          }
        }
      </section>

      <section class="rounded-lg border border-slate-700 bg-slate-800 p-6">
        <div class="mb-4">
          <h3 class="text-base font-semibold text-white">Alumnos por institucion</h3>
          <p class="mt-1 text-xs text-slate-400">
            Stock de alumnos activos al cierre del periodo (R-P2, asOf = {{ currentPeriod()?.to ?? '-' }}).
          </p>
        </div>

        @if (studentsState() === 'loading') {
          <div class="flex h-64 items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 text-sm text-slate-400">
            Cargando reporte de alumnos...
          </div>
        } @else if (studentsState() === 'error') {
          <div class="rounded-lg border border-red-700 bg-red-900/40 p-4">
            <p class="text-sm text-red-100">{{ studentsError() }}</p>
            <button
              type="button"
              class="mt-3 inline-flex items-center rounded-lg border border-red-500/50 bg-red-900/60 px-3 py-1.5 text-sm font-medium text-red-100 transition hover:bg-red-900"
              (click)="retryStudents()"
            >
              Reintentar
            </button>
          </div>
        } @else {
          @if (studentsBarData().length) {
            <app-report-chart mode="bar-horizontal" [barData]="studentsBarData()" />
          } @else {
            <div class="flex h-64 items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 text-sm text-slate-400">
              No hay datos de alumnos para la fecha seleccionada.
            </div>
          }
        }
      </section>
    </div>
  `,
})
export class PlatformReportsTabComponent implements OnInit {
  private readonly reportsService = inject(ReportsService);
  private readonly router = inject(Router);

  readonly currentPeriod = signal<ReportPeriodRange | null>(null);
  readonly credentialsState = signal<ReportSectionState>('loading');
  readonly studentsState = signal<ReportSectionState>('loading');
  readonly credentialsError = signal<string | null>(null);
  readonly studentsError = signal<string | null>(null);
  readonly credentialsSource = signal<ReportSource | null>(null);
  readonly credentialsBarData = signal<readonly ReportBarPoint[]>([]);
  readonly studentsBarData = signal<readonly ReportBarPoint[]>([]);

  ngOnInit(): void {
    void this.loadReports(computeReportPeriod(30));
  }

  onPeriodChange(period: ReportPeriodRange): void {
    void this.loadReports(period);
  }

  retryCredentials(): void {
    const period = this.currentPeriod();
    if (!period) {
      return;
    }

    void this.loadCredentials(period);
  }

  retryStudents(): void {
    const period = this.currentPeriod();
    if (!period) {
      return;
    }

    void this.loadStudents(period);
  }

  private async loadReports(period: ReportPeriodRange): Promise<void> {
    this.currentPeriod.set(period);
    this.credentialsState.set('loading');
    this.studentsState.set('loading');
    this.credentialsError.set(null);
    this.studentsError.set(null);

    const [credentialsResult, studentsResult] = await Promise.allSettled([
      this.reportsService.getPlatformCredentialsByInstitution(period.from, period.to),
      this.reportsService.getPlatformStudentsByInstitution(period.to),
    ]);

    this.applyCredentialsResult(credentialsResult);
    this.applyStudentsResult(studentsResult);
  }

  private async loadCredentials(period: ReportPeriodRange): Promise<void> {
    this.credentialsState.set('loading');
    this.credentialsError.set(null);

    try {
      const report = await this.reportsService.getPlatformCredentialsByInstitution(
        period.from,
        period.to,
      );
      this.credentialsSource.set(report.source);
      this.credentialsBarData.set(
        report.items.map((item) => ({
          category: item.displayName,
          value: item.total,
        })),
      );
      this.credentialsState.set('loaded');
    } catch (error: unknown) {
      this.credentialsState.set('error');
      this.credentialsError.set(toErrorMessage(error));
      await this.handleUnauthorized(error);
    }
  }

  private async loadStudents(period: ReportPeriodRange): Promise<void> {
    this.studentsState.set('loading');
    this.studentsError.set(null);

    try {
      const report = await this.reportsService.getPlatformStudentsByInstitution(period.to);
      this.studentsBarData.set(
        report.items.map((item) => ({
          category: item.displayName,
          value: item.totalStudents,
        })),
      );
      this.studentsState.set('loaded');
    } catch (error: unknown) {
      this.studentsState.set('error');
      this.studentsError.set(toErrorMessage(error));
      await this.handleUnauthorized(error);
    }
  }

  private applyCredentialsResult(
    result: PromiseSettledResult<Awaited<ReturnType<ReportsService['getPlatformCredentialsByInstitution']>>>,
  ): void {
    if (result.status === 'fulfilled') {
      this.credentialsSource.set(result.value.source);
      this.credentialsBarData.set(
        result.value.items.map((item) => ({
          category: item.displayName,
          value: item.total,
        })),
      );
      this.credentialsState.set('loaded');
      return;
    }

    this.credentialsState.set('error');
    this.credentialsError.set(toErrorMessage(result.reason));
    void this.handleUnauthorized(result.reason);
  }

  private applyStudentsResult(
    result: PromiseSettledResult<Awaited<ReturnType<ReportsService['getPlatformStudentsByInstitution']>>>,
  ): void {
    if (result.status === 'fulfilled') {
      this.studentsBarData.set(
        result.value.items.map((item) => ({
          category: item.displayName,
          value: item.totalStudents,
        })),
      );
      this.studentsState.set('loaded');
      return;
    }

    this.studentsState.set('error');
    this.studentsError.set(toErrorMessage(result.reason));
    void this.handleUnauthorized(result.reason);
  }

  private async handleUnauthorized(error: unknown): Promise<void> {
    if (error instanceof PlatformUnauthorizedError) {
      await this.router.navigate(['/login'], {
        queryParams: { returnUrl: '/platform' },
      });
    }
  }
}
