import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import {
  CareerSummary,
  InstitutionSummary,
  StudentSummary,
} from '../../../core/models/academy.models';
import { AcademyService } from '../../../core/services/academy.service';
import { AuthService } from '../../../core/services/auth.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { withMinimumVisualDelay } from '../../../core/utils/visual-delay.util';
import { HexLoaderComponent } from '../../../shared/ui/hex-loader/hex-loader.component';
import { PortalShellComponent } from '../../../shared/ui/portal-shell/portal-shell.component';
import { IssuerTabComponent } from './issuer-tab.component';

@Component({
  selector: 'app-issuer',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    HexLoaderComponent,
    RouterLink,
    PortalShellComponent,
    IssuerTabComponent,
  ],
  template: `
    <app-portal-shell
      [portalLabel]="'shell.issuerPortal' | translate"
      [title]="'issuer.shellTitle' | translate"
      [subtitle]="'issuer.shellSubtitle' | translate"
      layoutWidth="full"
      [userRole]="activeRoleLabel()"
      (logout)="handleLogout()"
    >
      @if (errorMessage()) {
        <div class="mb-6 rounded-lg border border-red-700 bg-red-900/40 p-4 text-sm text-red-100">
          {{ errorMessage() }}
        </div>
      }

      <section class="mb-6 flex shrink-0 flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p class="text-xs font-medium uppercase text-blue-300">{{ 'issuer.activeInstitution' | translate }}</p>
          <h3 class="mt-1 truncate text-xl font-semibold text-white">
            {{ selectedInstitution()?.displayName || ('issuer.selectInstitution' | translate) }}
          </h3>
          <p class="text-sm text-slate-400">
            {{ 'issuer.sameModule' | translate }}
          </p>
          <span
            class="mt-2 inline-flex rounded-full border border-emerald-500/40 bg-emerald-500/10 px-2 py-0.5 text-xs font-semibold text-emerald-200"
          >
            {{ activeRoleLabel() }}
          </span>
        </div>

        <div class="flex flex-wrap gap-3">
          @if (authService.hasPlatformAdmin()) {
            <input
              class="min-w-80 rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white"
              [placeholder]="'issuer.institutionUuid' | translate"
              [ngModel]="selectedInstitutionId()"
              (ngModelChange)="selectedInstitutionId.set($event)"
            />
            <button
              type="button"
              class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50"
              [disabled]="loading() || !selectedInstitutionId().trim()"
              (click)="loadInstitution()"
            >
              {{ 'issuer.load' | translate }}
            </button>
          } @else {
            <select
              class="min-w-80 rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white"
              [ngModel]="selectedInstitutionId()"
              (ngModelChange)="onInstitutionChange($event)"
            >
              @for (membership of authService.getMemberships(); track membership.institutionId) {
                @if (['admin', 'issuer'].includes(membership.role.toLowerCase())) {
                  <option [value]="membership.institutionId">
                    {{ membership.institutionId }} - {{ membership.role }}
                  </option>
                }
              }
            </select>
          }

          <a
            class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-600"
            [routerLink]="['/academy']"
            [queryParams]="{ institutionId: selectedInstitutionId() }"
          >
            {{ 'issuer.openAcademy' | translate }}
          </a>
        </div>
      </section>

      @if (selectedInstitution()) {
        <app-issuer-tab
          [institutionId]="selectedInstitutionId()"
          [institution]="selectedInstitution()"
          [students]="students()"
          [careers]="careers()"
          [canIssue]="true"
          [canRevoke]="true"
        />
      } @else {
        <section class="rounded-lg border border-slate-700 bg-slate-800/50 p-10 text-center">
          @if (loading()) {
            <div class="flex min-h-48 flex-col items-center justify-center gap-4 text-sm text-slate-400">
              <app-hex-loader [label]="'issuer.loadingInstitution' | translate" />
              <p>{{ 'issuer.loadingInstitution' | translate }}...</p>
            </div>
          } @else {
            <p class="font-medium text-white">{{ 'issuer.noInstitution' | translate }}</p>
          }
        </section>
      }
    </app-portal-shell>
  `,
})
export class IssuerComponent implements OnInit {
  readonly authService = inject(AuthService);
  private readonly academyService = inject(AcademyService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedInstitutionId = signal('');
  readonly selectedInstitution = signal<InstitutionSummary | null>(null);
  readonly students = signal<readonly StudentSummary[]>([]);
  readonly careers = signal<readonly CareerSummary[]>([]);

  activeRoleLabel(): string {
    if (this.authService.hasPlatformAdmin()) {
      return 'platform_admin';
    }

    const institutionId = this.selectedInstitutionId();
    return this.authService
      .getMemberships()
      .find((membership) => membership.institutionId === institutionId)
      ?.role ?? 'issuer';
  }

  ngOnInit(): void {
    this.selectedInstitutionId.set(this.authService.getMemberships()[0]?.institutionId ?? '');
    if (this.selectedInstitutionId()) {
      void this.loadInstitution();
    }
  }

  async onInstitutionChange(institutionId: string): Promise<void> {
    this.selectedInstitutionId.set(institutionId);
    await this.loadInstitution();
  }

  async loadInstitution(): Promise<void> {
    const institutionId = this.selectedInstitutionId().trim();
    if (!institutionId) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      const [institution, students, careers] = await withMinimumVisualDelay(
        Promise.all([
          this.academyService.getInstitution(institutionId),
          this.academyService.listStudents(institutionId),
          this.academyService.listCareers(institutionId),
        ]),
      );
      this.selectedInstitution.set(institution as InstitutionSummary);
      this.students.set(students);
      this.careers.set(careers);
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  handleLogout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
