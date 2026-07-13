import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import {
  CareerSummary,
  InstitutionRole,
  InstitutionSummary,
  InstitutionUserSummary,
  StudentSummary,
} from '../../../core/models/academy.models';
import { PLATFORM_DEFAULT_COUNTRY_CODE } from '../../../core/constants/platform.constants';
import { AcademyService } from '../../../core/services/academy.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { toErrorMessage } from '../../../core/utils/error.utils';
import { withMinimumVisualDelay } from '../../../core/utils/visual-delay.util';
import { CopyValueComponent } from '../../../shared/ui/copy-value/copy-value.component';
import { HexLoaderComponent } from '../../../shared/ui/hex-loader/hex-loader.component';
import { ModalComponent } from '../../../shared/ui/modal/modal.component';
import { PortalShellComponent } from '../../../shared/ui/portal-shell/portal-shell.component';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge/status-badge.component';
import { IssuerTabComponent } from '../issuer/issuer-tab.component';
import { AcademyReportsTabComponent } from './academy-reports-tab.component';

type AcademyTab = 'students' | 'careers' | 'users' | 'reports' | 'issuer';
type AcademyInfoPanelTab = 'summary' | 'institution';

const INSTITUTION_ROLES: readonly InstitutionRole[] = ['admin', 'issuer', 'viewer'];

@Component({
  selector: 'app-academy',
  standalone: true,
  host: {
    class: 'block h-full',
  },
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    RouterLink,
    PortalShellComponent,
    ModalComponent,
    HexLoaderComponent,
    StatusBadgeComponent,
    CopyValueComponent,
    AcademyReportsTabComponent,
    IssuerTabComponent,
  ],
  template: `
    <app-portal-shell
      [portalLabel]="'shell.academyPortal' | translate"
      [title]="'academy.title' | translate"
      [subtitle]="'academy.subtitle' | translate"
      layoutWidth="full"
      [hideHeader]="true"
      [userName]="activeUserName()"
      [userRole]="activeRoleLabel()"
      (logout)="handleLogout()"
    >
      <div class="flex min-h-0 flex-1 flex-col overflow-hidden">
      @if (errorMessage()) {
        <div class="mb-6 shrink-0 rounded-lg border border-red-700 bg-red-900/40 p-4 text-sm text-red-100">
          {{ errorMessage() }}
        </div>
      }
      @if (successMessage()) {
        <div class="mb-6 shrink-0 rounded-lg border border-emerald-500/40 bg-emerald-500/10 p-4 text-sm text-emerald-100">
          {{ successMessage() }}
        </div>
      }

      @if (selectedInstitution()) {
        <div class="grid min-h-0 flex-1 grid-rows-[auto_auto_minmax(0,1fr)] gap-4 overflow-hidden">
          <section class="grid shrink-0 gap-4 rounded-lg border border-slate-700 bg-slate-800/40 p-4 lg:grid-cols-[minmax(260px,0.9fr)_minmax(320px,1.1fr)_auto] lg:items-center">
          <div class="min-w-0">
            <h2 class="text-2xl font-bold text-white">{{ 'academy.title' | translate }}</h2>
            <p class="mt-1 text-sm text-slate-400">
              {{ 'academy.subtitle' | translate }}
            </p>
          </div>

          <div class="min-w-0 border-slate-700 lg:border-l lg:pl-5">
            <p class="text-xs font-medium uppercase text-blue-300">{{ 'academy.activeInstitution' | translate }}</p>
            <h3 class="mt-1 truncate text-xl font-bold text-white">
              {{ selectedInstitution()!.displayName }}
            </h3>
            <div class="mt-1 flex min-w-0 flex-wrap items-center gap-2">
              <p class="truncate text-sm text-slate-400">{{ selectedInstitution()!.legalName }}</p>
              <span
                class="inline-flex rounded-full border px-2 py-0.5 text-xs font-semibold"
                [ngClass]="activeRoleBadgeClass()"
              >
                {{ activeRoleLabel() }}
              </span>
            </div>
          </div>

          <div class="flex flex-wrap justify-start gap-3 lg:justify-end">
            @if (authService.hasPlatformAdmin()) {
              <a
                class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
                [routerLink]="['/platform']"
              >
                Volver a Platform
              </a>
            }
            <button
              type="button"
              class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
              (click)="loadSelectedInstitution()"
              [disabled]="loading()"
            >
              {{ loading() ? ('common.loading' | translate) : ('common.refresh' | translate) }}
            </button>
            @if (activeTab() === 'students' && canManageInstitution()) {
              <button
                type="button"
                class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500"
                (click)="openCreateStudentModal()"
              >
                {{ 'academy.students.create' | translate }}
              </button>
            }
            @if (activeTab() === 'careers' && canManageInstitution()) {
              <button
                type="button"
                class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500"
                (click)="openCareerModal()"
              >
                {{ 'academy.careers.create' | translate }}
              </button>
            }
            @if (activeTab() === 'careers' && canManageInstitution()) {
              <button
                type="button"
                class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500"
                (click)="openCareerModal()"
              >
                Crear carrera
              </button>
            }
            @if (activeTab() === 'users' && canManageInstitution()) {
              <button
                type="button"
                class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500"
                (click)="openInviteUserModal()"
              >
                {{ 'academy.users.invite' | translate }}
              </button>
            }
          </div>
        </section>

        <div class="flex shrink-0 gap-2 border-b border-slate-700">
          @if (canViewAcademyManagement()) {
            <button
              type="button"
              class="border-b-2 px-4 py-3 text-sm font-medium"
              [ngClass]="activeTab() === 'students'
                ? 'border-blue-500 text-blue-300'
                : 'border-transparent text-slate-400 hover:text-white'"
              (click)="setActiveTab('students')"
            >
              {{ 'academy.tabs.students' | translate }}
            </button>
          }
          @if (canViewAcademyManagement()) {
            <button
              type="button"
              class="border-b-2 px-4 py-3 text-sm font-medium"
              [ngClass]="activeTab() === 'careers'
                ? 'border-blue-500 text-blue-300'
                : 'border-transparent text-slate-400 hover:text-white'"
              (click)="setActiveTab('careers')"
            >
              {{ 'academy.tabs.careers' | translate }}
            </button>
          }
          @if (canManageInstitution()) {
            <button
              type="button"
              class="border-b-2 px-4 py-3 text-sm font-medium"
              [ngClass]="activeTab() === 'users'
                ? 'border-blue-500 text-blue-300'
                : 'border-transparent text-slate-400 hover:text-white'"
              (click)="setActiveTab('users')"
            >
              {{ 'academy.tabs.users' | translate }}
            </button>
          }
          @if (canViewAcademyManagement()) {
            <button
              type="button"
              class="border-b-2 px-4 py-3 text-sm font-medium"
              [ngClass]="activeTab() === 'reports'
                ? 'border-blue-500 text-blue-300'
                : 'border-transparent text-slate-400 hover:text-white'"
              (click)="setActiveTab('reports')"
            >
              {{ 'academy.tabs.reports' | translate }}
            </button>
          }
          @if (canUseIssuerTab()) {
            <button
              type="button"
              class="border-b-2 px-4 py-3 text-sm font-medium"
              [ngClass]="activeTab() === 'issuer'
                ? 'border-blue-500 text-blue-300'
                : 'border-transparent text-slate-400 hover:text-white'"
              (click)="setActiveTab('issuer')"
            >
              {{ 'academy.tabs.issuance' | translate }}
            </button>
          }
        </div>

        @if (activeTab() === 'reports' && canViewAcademyManagement()) {
          <section class="min-h-0 overflow-y-auto overscroll-y-contain">
            <app-academy-reports-tab [institutionId]="selectedInstitutionId()" />
          </section>
        } @else {
          <section class="grid min-h-0 overflow-hidden gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(360px,440px)]">
          @if (activeTab() === 'students') {
            <section class="flex min-h-0 flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-800">
              <div class="border-b border-slate-700 p-4">
                <h3 class="text-base font-semibold text-white">{{ 'academy.students.title' | translate }}</h3>
                <p class="text-xs text-slate-400">{{ 'academy.records' | translate:{ count: students().length } }}</p>
              </div>
              <div class="min-h-0 flex-1 overflow-auto">
                @for (student of students(); track student.id) {
                  <button
                    type="button"
                    class="w-full border-b border-slate-700/70 p-4 text-left transition hover:bg-slate-700/40"
                    [class.bg-slate-700]="selectedStudent()?.id === student.id"
                    (click)="selectStudent(student)"
                  >
                    <div class="flex items-start justify-between gap-3">
                      <div class="min-w-0">
                        <p class="truncate text-sm font-semibold text-white">
                          {{ student.externalReference || ('academy.noReference' | translate) }}
                        </p>
                        <p class="mt-1 truncate font-mono text-xs text-slate-500">{{ student.id }}</p>
                      </div>
                      <app-status-badge
                        [label]="student.isActive ? ('common.active' | translate) : ('common.inactive' | translate)"
                        [tone]="student.isActive ? 'success' : 'danger'"
                      />
                    </div>
                    <div class="mt-3 grid gap-2 text-xs text-slate-400 md:grid-cols-2">
                      <span>{{ 'academy.students.year' | translate }}: {{ student.enrollmentYear || '-' }}</span>
                      <span class="truncate font-mono">{{ student.primaryWalletAddress || ('common.withoutWallet' | translate) }}</span>
                    </div>
                  </button>
                } @empty {
                  @if (loading()) {
                    <div class="flex min-h-48 flex-col items-center justify-center gap-4 p-10 text-center text-sm text-slate-400">
                      <app-hex-loader [label]="'academy.students.loading' | translate" />
                      <p>{{ 'academy.students.loading' | translate }}</p>
                    </div>
                  } @else {
                    <p class="p-10 text-center text-sm text-slate-400">
                      {{ 'academy.students.empty' | translate }}
                    </p>
                  }
                }
              </div>
            </section>

            <aside class="min-h-0 overflow-auto rounded-lg border border-slate-700 bg-slate-800 p-6">
              @if (selectedStudent()) {
                <div class="space-y-6">
                  <div class="flex items-start justify-between gap-3">
                    <div class="min-w-0">
                      <p class="text-xs font-medium uppercase text-blue-300">{{ 'academy.students.selected' | translate }}</p>
                      <h3 class="mt-1 truncate text-xl font-semibold text-white">
                        {{ selectedStudent()!.externalReference || ('academy.noReference' | translate) }}
                      </h3>
                    </div>
                    <button
                      type="button"
                      class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                      [attr.aria-label]="'common.close' | translate"
                      (click)="selectedStudent.set(null)"
                    >
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                  <dl class="space-y-4 text-sm">
                    <div>
                      <dt class="text-slate-500">ID</dt>
                      <dd><app-copy-value [value]="selectedStudent()!.id" /></dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">{{ 'academy.students.entryYear' | translate }}</dt>
                      <dd class="text-slate-200">{{ selectedStudent()!.enrollmentYear || '-' }}</dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">{{ 'academy.students.primaryWallet' | translate }}</dt>
                      <dd><app-copy-value [value]="selectedStudent()!.primaryWalletAddress" /></dd>
                    </div>
                  </dl>
                  @if (canManageInstitution()) {
                    <button
                      type="button"
                      class="w-full rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
                      (click)="openWalletModal(selectedStudent()!.id)"
                    >
                      {{ 'academy.students.addWallet' | translate }}
                    </button>
                  }
                </div>
              } @else {
                <div class="space-y-6">
                  <div class="flex gap-2 border-b border-slate-700">
                    <button
                      type="button"
                      class="border-b-2 px-3 py-2 text-sm font-medium"
                      [ngClass]="infoPanelTab() === 'summary'
                        ? 'border-blue-500 text-blue-300'
                        : 'border-transparent text-slate-400 hover:text-white'"
                      (click)="infoPanelTab.set('summary')"
                    >
                      {{ 'holder.summary' | translate }}
                    </button>
                    <button
                      type="button"
                      class="border-b-2 px-3 py-2 text-sm font-medium"
                      [ngClass]="infoPanelTab() === 'institution'
                        ? 'border-blue-500 text-blue-300'
                        : 'border-transparent text-slate-400 hover:text-white'"
                      (click)="infoPanelTab.set('institution')"
                    >
                      {{ 'invitation.institution' | translate }}
                    </button>
                  </div>

                  @if (infoPanelTab() === 'summary') {
                  <div>
                    <p class="text-xs font-medium uppercase text-blue-300">{{ 'academy.students.summary' | translate }}</p>
                    <h3 class="mt-1 text-xl font-semibold text-white">{{ 'academy.students.overview' | translate }}</h3>
                    <p class="mt-1 text-sm text-slate-400">{{ 'academy.students.selectHint' | translate }}</p>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">{{ 'common.total' | translate }}</p>
                      <p class="mt-2 text-2xl font-bold text-white">{{ students().length }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">{{ 'academy.students.withWallet' | translate }}</p>
                      <p class="mt-2 text-2xl font-bold text-emerald-300">{{ studentsWithWalletCount() }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">{{ 'academy.students.withoutWallet' | translate }}</p>
                      <p class="mt-2 text-2xl font-bold text-amber-300">{{ studentsWithoutWalletCount() }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">{{ 'academy.students.active' | translate }}</p>
                      <p class="mt-2 text-2xl font-bold text-blue-300">{{ activeStudentsCount() }}</p>
                    </article>
                  </div>
                  } @else {
                    <ng-container [ngTemplateOutlet]="institutionContextPanel" />
                  }
                </div>
              }
            </aside>
          }

          @if (activeTab() === 'careers') {
            <section class="flex min-h-0 flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-800">
              <div class="border-b border-slate-700 p-4">
                <h3 class="text-base font-semibold text-white">{{ 'academy.careers.pool' | translate }}</h3>
                <p class="text-xs text-slate-400">{{ 'academy.careers.count' | translate:{ count: careers().length } }}</p>
              </div>
              <div class="min-h-0 flex-1 overflow-auto">
                @for (career of careers(); track career.id) {
                  <button
                    type="button"
                    class="w-full border-b border-slate-700/70 p-4 text-left transition hover:bg-slate-700/40"
                    [class.bg-slate-700]="selectedCareer()?.id === career.id"
                    (click)="selectCareer(career)"
                  >
                    <div class="flex items-start justify-between gap-3">
                      <div class="min-w-0">
                        <p class="truncate text-sm font-semibold text-white">{{ career.name }}</p>
                        <p class="mt-1 font-mono text-xs text-slate-500">{{ career.code }}</p>
                      </div>
                      <app-status-badge
                        [label]="career.isActive ? ('common.active' | translate) : ('common.inactive' | translate)"
                        [tone]="career.isActive ? 'success' : 'danger'"
                      />
                    </div>
                    <div class="mt-3 flex items-center gap-2 text-xs text-slate-400">
                      <span class="h-1.5 w-1.5 rounded-full" [ngClass]="career.isActive ? 'bg-emerald-400' : 'bg-red-400'"></span>
                      <span>{{ career.isActive ? ('academy.careers.availableForIssuance' | translate) : ('academy.careers.outOfIssuance' | translate) }}</span>
                    </div>
                  </button>
                } @empty {
                  <div class="p-10 text-center">
                    @if (loading()) {
                      <div class="flex min-h-40 flex-col items-center justify-center gap-4 text-sm text-slate-400">
                        <app-hex-loader [label]="'academy.careers.loading' | translate" />
                        <p>{{ 'academy.careers.loading' | translate }}</p>
                      </div>
                    } @else {
                      <p class="text-sm font-medium text-white">
                        {{ 'academy.careers.empty' | translate }}
                      </p>
                    }
                    @if (!loading() && canManageInstitution()) {
                      <button
                        type="button"
                        class="mt-4 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500"
                        (click)="openCareerModal()"
                      >
                        {{ 'academy.careers.createFirst' | translate }}
                      </button>
                    }
                  </div>
                }
              </div>
            </section>

            <aside class="detail-panel min-h-0 overflow-auto rounded-lg border border-slate-700 bg-slate-800 p-6">
              @if (selectedCareer()) {
                <div class="space-y-6">
                  <div class="flex items-start justify-between gap-3">
                    <div class="min-w-0">
                      <p class="text-xs font-medium uppercase text-blue-300">{{ 'academy.careers.selected' | translate }}</p>
                      <h3 class="mt-1 truncate text-xl font-semibold text-white">
                        {{ selectedCareer()!.name }}
                      </h3>
                    </div>
                    <button
                      type="button"
                      class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                      [attr.aria-label]="'common.close' | translate"
                      (click)="selectedCareer.set(null)"
                    >
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                  <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                    <p class="text-xs text-slate-500">{{ 'academy.careers.operationalStatus' | translate }}</p>
                    <p class="mt-2 text-sm font-semibold" [ngClass]="selectedCareer()!.isActive ? 'text-emerald-300' : 'text-red-300'">
                      {{ selectedCareer()!.isActive ? ('academy.careers.activeForIssuance' | translate) : ('common.inactive' | translate) }}
                    </p>
                  </div>
                  <dl class="space-y-4 text-sm">
                    <div>
                      <dt class="text-slate-500">{{ 'platform.institutions.code' | translate }}</dt>
                      <dd class="font-mono text-slate-200">{{ selectedCareer()!.code }}</dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">ID</dt>
                      <dd><app-copy-value [value]="selectedCareer()!.id" /></dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">{{ 'academy.careers.createdAt' | translate }}</dt>
                      <dd class="text-slate-200">{{ formatDate(selectedCareer()!.createdAt) }}</dd>
                    </div>
                  </dl>

                  @if (canManageInstitution()) {
                    <div class="space-y-3 border-t border-slate-700 pt-5">
                      <button
                        type="button"
                        class="w-full rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
                        (click)="openCareerModal(selectedCareer()!)"
                      >
                        {{ 'academy.careers.edit' | translate }}
                      </button>
                      @if (selectedCareer()!.isActive) {
                        <button
                          type="button"
                          class="w-full rounded-lg border border-red-500/40 px-4 py-2.5 text-sm font-semibold text-red-300 hover:bg-red-500/10"
                          (click)="handleDeactivateCareer(selectedCareer()!)"
                          [disabled]="submitting()"
                        >
                          {{ 'academy.careers.deactivate' | translate }}
                        </button>
                      }
                    </div>
                  }
                </div>
              } @else {
                <div class="space-y-6">
                  <div>
                    <p class="text-xs font-medium uppercase text-blue-300">{{ 'academy.careers.catalog' | translate }}</p>
                    <h3 class="mt-1 text-xl font-semibold text-white">{{ 'academy.careers.institutional' | translate }}</h3>
                    <p class="mt-1 text-sm text-slate-400">
                      {{ 'academy.careers.poolHint' | translate }}
                    </p>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">{{ 'common.active' | translate }}</p>
                      <p class="mt-2 text-2xl font-bold text-emerald-300">{{ activeCareersCount() }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">{{ 'common.inactive' | translate }}</p>
                      <p class="mt-2 text-2xl font-bold text-red-300">{{ inactiveCareersCount() }}</p>
                    </article>
                  </div>
                  <ng-container [ngTemplateOutlet]="institutionContextPanel" />
                </div>
              }
            </aside>
          }

          @if (activeTab() === 'users' && canManageInstitution()) {
            <section class="flex min-h-0 flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-800">
              <div class="border-b border-slate-700 p-4">
                <h3 class="text-base font-semibold text-white">{{ 'academy.users.title' | translate }}</h3>
                <p class="text-xs text-slate-400">{{ 'academy.users.activeCount' | translate:{ count: institutionUsers().length } }}</p>
              </div>
              <div class="min-h-0 flex-1 overflow-auto">
                @for (user of institutionUsers(); track user.id) {
                  <button
                    type="button"
                    class="w-full border-b border-slate-700/70 p-4 text-left transition hover:bg-slate-700/40"
                    [class.bg-slate-700]="selectedUser()?.id === user.id"
                    (click)="selectUser(user)"
                  >
                    <div class="flex items-start justify-between gap-3">
                      <div class="min-w-0">
                        <p class="truncate text-sm font-semibold text-white">{{ user.displayName || user.email || user.walletAddress }}</p>
                        <p class="mt-1 truncate font-mono text-xs text-slate-500">{{ user.walletAddress || user.id }}</p>
                      </div>
                      <app-status-badge [label]="user.role" tone="info" />
                    </div>
                  </button>
                } @empty {
                  @if (loading()) {
                    <div class="flex min-h-48 flex-col items-center justify-center gap-4 p-10 text-center text-sm text-slate-400">
                      <app-hex-loader [label]="'academy.users.loading' | translate" />
                      <p>{{ 'academy.users.loading' | translate }}</p>
                    </div>
                  } @else {
                    <p class="p-10 text-center text-sm text-slate-400">
                      {{ 'academy.users.empty' | translate }}
                    </p>
                  }
                }
              </div>
            </section>

            <aside class="min-h-0 overflow-auto rounded-lg border border-slate-700 bg-slate-800 p-6">
              @if (selectedUser()) {
                <div class="space-y-6">
                  <div class="flex items-start justify-between gap-3">
                    <div class="min-w-0">
                      <p class="text-xs font-medium uppercase text-blue-300">{{ 'academy.users.selected' | translate }}</p>
                      <h3 class="mt-1 truncate text-xl font-semibold text-white">
                        {{ selectedUser()!.displayName || selectedUser()!.email || ('academy.users.institutional' | translate) }}
                      </h3>
                    </div>
                    <button
                      type="button"
                      class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                      [attr.aria-label]="'common.close' | translate"
                      (click)="selectedUser.set(null)"
                    >
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                  <dl class="space-y-4 text-sm">
                    <div>
                      <dt class="text-slate-500">Email</dt>
                      <dd class="text-slate-200">{{ selectedUser()!.email || '-' }}</dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">{{ 'common.wallet' | translate }}</dt>
                      <dd><app-copy-value [value]="selectedUser()!.walletAddress" /></dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">{{ 'platform.institutions.role' | translate }}</dt>
                      <dd class="text-slate-200">{{ selectedUser()!.role }}</dd>
                    </div>
                  </dl>

                  <div class="space-y-3 border-t border-slate-700 pt-5">
                    <label class="block text-sm font-medium text-slate-300" for="detailUserRole">{{ 'academy.users.changeRole' | translate }}</label>
                    <select
                      id="detailUserRole"
                      class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white"
                      [ngModel]="selectedUser()!.role"
                      (ngModelChange)="handleRoleChange(selectedUser()!, $event)"
                    >
                      @for (role of roles; track role) {
                        <option [value]="role">{{ role }}</option>
                      }
                    </select>
                    <button
                      type="button"
                      class="w-full rounded-lg border border-red-500/40 px-4 py-2.5 text-sm font-semibold text-red-300 hover:bg-red-500/10"
                      (click)="handleRevokeUser(selectedUser()!)"
                    >
                      {{ 'academy.users.revokeAccess' | translate }}
                    </button>
                  </div>
                </div>
              } @else {
                <div class="space-y-6">
                  <div class="flex gap-2 border-b border-slate-700">
                    <button
                      type="button"
                      class="border-b-2 px-3 py-2 text-sm font-medium"
                      [ngClass]="infoPanelTab() === 'summary'
                        ? 'border-blue-500 text-blue-300'
                        : 'border-transparent text-slate-400 hover:text-white'"
                      (click)="infoPanelTab.set('summary')"
                    >
                      {{ 'holder.summary' | translate }}
                    </button>
                    <button
                      type="button"
                      class="border-b-2 px-3 py-2 text-sm font-medium"
                      [ngClass]="infoPanelTab() === 'institution'
                        ? 'border-blue-500 text-blue-300'
                        : 'border-transparent text-slate-400 hover:text-white'"
                      (click)="infoPanelTab.set('institution')"
                    >
                      {{ 'invitation.institution' | translate }}
                    </button>
                  </div>

                  @if (infoPanelTab() === 'summary') {
                  <div>
                    <p class="text-xs font-medium uppercase text-blue-300">{{ 'academy.users.summary' | translate }}</p>
                    <h3 class="mt-1 text-xl font-semibold text-white">{{ 'academy.students.overview' | translate }}</h3>
                    <p class="mt-1 text-sm text-slate-400">{{ 'academy.users.selectHint' | translate }}</p>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">{{ 'common.total' | translate }}</p>
                      <p class="mt-2 text-2xl font-bold text-white">{{ institutionUsers().length }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">Admins</p>
                      <p class="mt-2 text-2xl font-bold text-blue-300">{{ institutionAdminCount() }}</p>
                    </article>
                  </div>
                  } @else {
                    <ng-container [ngTemplateOutlet]="institutionContextPanel" />
                  }
                </div>
              }
            </aside>
          }

          @if (activeTab() === 'issuer' && canUseIssuerTab()) {
            <div class="flex min-h-0 flex-col xl:col-span-2">
              <app-issuer-tab
                [institutionId]="selectedInstitutionId()"
                [institution]="selectedInstitution()"
                [students]="students()"
                [careers]="careers()"
                [canIssue]="canIssueCredentials()"
                [canRevoke]="canIssueCredentials()"
                [showHeader]="false"
              />
            </div>
          }
        </section>
        }
        </div>
      } @else {
        <section class="shrink-0 rounded-lg border border-slate-700 bg-slate-800/50 p-10 text-center">
          <p class="font-medium text-white">{{ 'academy.selectInstitution' | translate }}</p>
          <p class="mt-2 text-sm text-slate-400">
            {{ 'academy.roleHint' | translate }}
          </p>
        </section>
      }
      </div>

      <ng-template #institutionContextPanel>
        <div class="space-y-5">
          <div>
            <p class="text-xs font-medium uppercase text-blue-300">{{ 'academy.institutionContext' | translate }}</p>
            <h3 class="mt-1 text-xl font-semibold text-white">
              {{ selectedInstitution()?.displayName }}
            </h3>
            <p class="mt-1 text-sm text-slate-400">
              {{ 'academy.institutionContextHint' | translate }}
            </p>
          </div>

          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="institutionSelector">
              {{ 'invitation.institution' | translate }}
            </label>

            @if (authService.hasPlatformAdmin()) {
              <div class="flex gap-3">
                <input
                  id="institutionSelector"
                  class="min-w-0 flex-1 rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30"
                  [placeholder]="'issuer.institutionUuid' | translate"
                  [ngModel]="selectedInstitutionId()"
                  (ngModelChange)="selectedInstitutionId.set($event)"
                />
                <button
                  type="button"
                  class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50"
                  [disabled]="loading() || !selectedInstitutionId().trim()"
                  (click)="loadSelectedInstitution()"
                >
                  {{ 'issuer.load' | translate }}
                </button>
              </div>
            } @else {
              <select
                id="institutionSelector"
                class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30"
                [ngModel]="selectedInstitutionId()"
                (ngModelChange)="onInstitutionChange($event)"
              >
                @for (membership of authService.getMemberships(); track membership.institutionId) {
                  <option [value]="membership.institutionId">
                    {{ membership.institutionId }} - {{ membership.role }}
                  </option>
                }
              </select>
            }
          </div>

          <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
            <p class="text-xs text-slate-500">{{ 'academy.activeRole' | translate }}</p>
            <p class="mt-1 text-sm font-semibold text-white">{{ activeRoleLabel() }}</p>
          </div>

          @if (canManageInstitution()) {
            <button
              type="button"
              class="inline-flex w-full items-center justify-center rounded-lg border border-blue-500/50 bg-blue-500/10 px-4 py-2.5 text-sm font-semibold text-blue-100 hover:bg-blue-500/20 disabled:cursor-not-allowed disabled:opacity-50"
              [disabled]="loading()"
              (click)="openEditInstitutionModal()"
            >
              {{ 'platform.institutions.edit' | translate }}
            </button>
          }

          <dl class="space-y-3 text-sm">
            <div>
              <dt class="text-slate-500">{{ 'platform.institutions.code' | translate }}</dt>
              <dd class="text-slate-200">{{ selectedInstitution()?.code || '-' }}</dd>
            </div>
            <div>
              <dt class="text-slate-500">{{ 'platform.institutions.country' | translate }}</dt>
              <dd class="text-slate-200">{{ selectedInstitution()?.countryCode || '-' }}</dd>
            </div>
            <div>
              <dt class="text-slate-500">{{ 'issuer.issuerWallet' | translate }}</dt>
              <dd class="break-all font-mono text-xs text-slate-200">
                <app-copy-value [value]="selectedInstitution()?.issuerWalletAddress" [emptyLabel]="'issuer.noIssuerWallet' | translate" />
              </dd>
            </div>
          </dl>

          @if (authService.hasPlatformAdmin() && institutions().length) {
            <div class="border-t border-slate-700 pt-4">
              <p class="mb-3 text-sm font-semibold text-white">{{ 'academy.recentInstitutions' | translate }}</p>
              <div class="space-y-2">
                @for (institution of institutions(); track institution.id) {
                  <button
                    type="button"
                    class="w-full rounded-lg border border-slate-700 bg-slate-900/60 p-3 text-left hover:border-violet-500/70"
                    (click)="selectInstitution(institution.id)"
                  >
                    <p class="truncate text-sm font-semibold text-white">{{ institution.displayName }}</p>
                    <p class="text-xs text-slate-500">{{ institution.code }}</p>
                  </button>
                }
              </div>
            </div>
          }
        </div>
      </ng-template>

      <app-modal
        [isOpen]="editInstitutionModalOpen()"
        [title]="'platform.institutions.edit' | translate"
        [description]="'platform.institutions.editDescription' | translate"
        size="lg"
        (closed)="closeEditInstitutionModal()"
      >
        <form class="grid gap-4 md:grid-cols-2" (submit)="handleUpdateInstitution($event)">
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="academyEditCode">{{ 'platform.institutions.code' | translate }}</label>
            <input id="academyEditCode" class="w-full cursor-not-allowed rounded-lg border border-slate-700 bg-slate-950/70 px-3 py-2 text-sm text-slate-500" [value]="selectedInstitution()?.code || ''" disabled />
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="academyEditCountryCode">{{ 'platform.institutions.country' | translate }}</label>
            <input id="academyEditCountryCode" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30" [ngModel]="editInstitutionCountryCode()" name="academyEditCountryCode" (ngModelChange)="editInstitutionCountryCode.set($event)" required />
          </div>
          <div class="md:col-span-2">
            <label class="mb-1 block text-sm font-medium text-slate-300" for="academyEditLegalName">{{ 'platform.institutions.legalName' | translate }}</label>
            <input id="academyEditLegalName" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30" [ngModel]="editInstitutionLegalName()" name="academyEditLegalName" (ngModelChange)="editInstitutionLegalName.set($event)" required />
          </div>
          <div class="md:col-span-2">
            <label class="mb-1 block text-sm font-medium text-slate-300" for="academyEditDisplayName">{{ 'platform.institutions.displayName' | translate }}</label>
            <input id="academyEditDisplayName" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30" [ngModel]="editInstitutionDisplayName()" name="academyEditDisplayName" (ngModelChange)="editInstitutionDisplayName.set($event)" required />
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="academyEditWebsiteUrl">{{ 'platform.institutions.websiteUrl' | translate }}</label>
            <input id="academyEditWebsiteUrl" type="url" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30" [ngModel]="editInstitutionWebsiteUrl()" name="academyEditWebsiteUrl" (ngModelChange)="editInstitutionWebsiteUrl.set($event)" />
          </div>
          <label class="flex items-center gap-3 self-end rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm text-slate-200">
            <input type="checkbox" class="h-4 w-4 rounded border-slate-600 bg-slate-900 text-blue-600 focus:ring-blue-500" [ngModel]="editInstitutionIsActive()" name="academyEditIsActive" (ngModelChange)="editInstitutionIsActive.set($event)" />
            {{ 'platform.institutions.activeInstitution' | translate }}
          </label>
          <div class="md:col-span-2">
            <label class="mb-1 block text-sm font-medium text-slate-300" for="academyEditIssuerWallet">{{ 'platform.institutions.issuerWallet' | translate }}</label>
            <input id="academyEditIssuerWallet" class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30" [placeholder]="'platform.institutions.issuerWalletPlaceholder' | translate" [ngModel]="editInstitutionIssuerWallet()" name="academyEditIssuerWallet" (ngModelChange)="editInstitutionIssuerWallet.set($event)" />
            <p class="mt-1 text-xs text-slate-500">{{ 'platform.institutions.issuerWalletHint' | translate }}</p>
          </div>
          <div class="flex justify-end gap-3 md:col-span-2">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeEditInstitutionModal()">{{ 'common.cancel' | translate }}</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting()">
              {{ submitting() ? ('common.saving' | translate) : ('platform.institutions.saveChanges' | translate) }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="careerModalOpen()"
        [title]="editingCareer() ? ('academy.careers.edit' | translate) : ('academy.careers.create' | translate)"
        [description]="'academy.careers.modalDescription' | translate"
        (closed)="closeCareerModal()"
      >
        <form class="grid gap-4" (submit)="handleSaveCareer($event)">
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white" [placeholder]="'academy.careers.codePlaceholder' | translate" [ngModel]="careerCode()" name="careerCode" (ngModelChange)="careerCode.set($event)" />
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" [placeholder]="'academy.careers.namePlaceholder' | translate" [ngModel]="careerName()" name="careerName" (ngModelChange)="careerName.set($event)" />
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeCareerModal()">{{ 'common.cancel' | translate }}</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting() || !careerCode().trim() || !careerName().trim()">
              {{ submitting() ? ('common.saving' | translate) : (editingCareer() ? ('academy.careers.save' | translate) : ('academy.careers.create' | translate)) }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="createStudentModalOpen()"
        [title]="'academy.students.create' | translate"
        [description]="'academy.students.createDescription' | translate"
        (closed)="closeCreateStudentModal()"
      >
        <form class="grid gap-4" (submit)="handleCreateStudent($event)">
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" [placeholder]="'academy.students.referencePlaceholder' | translate" [ngModel]="studentReference()" name="studentReference" (ngModelChange)="studentReference.set($event)" />
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" [placeholder]="'academy.students.yearPlaceholder' | translate" type="number" [ngModel]="studentYear()" name="studentYear" (ngModelChange)="studentYear.set($event)" />
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" [placeholder]="'academy.students.walletPlaceholder' | translate" [ngModel]="studentWallet()" name="studentWallet" (ngModelChange)="studentWallet.set($event)" />
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeCreateStudentModal()">{{ 'common.cancel' | translate }}</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting()">
              {{ submitting() ? ('academy.creating' | translate) : ('academy.students.create' | translate) }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="walletModalOpen()"
        [title]="'academy.students.addWallet' | translate"
        [description]="'academy.students.walletDescription' | translate"
        (closed)="closeWalletModal()"
      >
        <form class="grid gap-4" (submit)="handleAddWallet($event)">
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white" placeholder="Student ID" [ngModel]="walletStudentId()" name="walletStudentId" (ngModelChange)="walletStudentId.set($event)" />
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" [placeholder]="'academy.students.walletAddressPlaceholder' | translate" [ngModel]="walletAddress()" name="walletAddress" (ngModelChange)="walletAddress.set($event)" />
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeWalletModal()">{{ 'common.cancel' | translate }}</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting() || !walletStudentId().trim() || !walletAddress().trim()">
              {{ submitting() ? ('academy.students.linkingWallet' | translate) : ('academy.students.addWallet' | translate) }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="inviteUserModalOpen()"
        [title]="'academy.users.invite' | translate"
        [description]="'academy.users.inviteDescription' | translate"
        (closed)="closeInviteUserModal()"
      >
        <form class="grid gap-4" (submit)="handleInviteUser($event)">
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" [placeholder]="'academy.users.emailPlaceholder' | translate" type="email" [ngModel]="inviteEmail()" name="inviteEmail" (ngModelChange)="inviteEmail.set($event)" required />
          <select class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" [ngModel]="inviteRole()" name="inviteRole" (ngModelChange)="inviteRole.set($event)">
            @for (role of roles; track role) {
              <option [value]="role">{{ role }}</option>
            }
          </select>
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeInviteUserModal()">{{ 'common.cancel' | translate }}</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting() || !inviteEmail().trim()">
              {{ submitting() ? ('platform.institutions.sending' | translate) : ('platform.institutions.sendInvitation' | translate) }}
            </button>
          </div>
        </form>
      </app-modal>
    </app-portal-shell>
  `,
  styles: `
    .detail-panel {
      animation: panel-in 180ms ease-out;
    }

    @keyframes panel-in {
      from {
        opacity: 0;
        transform: translateX(12px);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }
  `,
})
export class AcademyComponent implements OnInit {
  readonly authService = inject(AuthService);
  private readonly academyService = inject(AcademyService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly languageService = inject(LanguageService);

  readonly roles = INSTITUTION_ROLES;
  readonly activeTab = signal<AcademyTab>('students');
  readonly infoPanelTab = signal<AcademyInfoPanelTab>('summary');
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly institutions = signal<readonly InstitutionSummary[]>([]);
  readonly selectedInstitutionId = signal('');
  readonly selectedInstitution = signal<InstitutionSummary | null>(null);
  readonly careers = signal<readonly CareerSummary[]>([]);
  readonly students = signal<readonly StudentSummary[]>([]);
  readonly institutionUsers = signal<readonly InstitutionUserSummary[]>([]);
  readonly selectedCareer = signal<CareerSummary | null>(null);
  readonly selectedStudent = signal<StudentSummary | null>(null);
  readonly selectedUser = signal<InstitutionUserSummary | null>(null);

  readonly careerModalOpen = signal(false);
  readonly editingCareer = signal<CareerSummary | null>(null);
  readonly editInstitutionModalOpen = signal(false);
  readonly createStudentModalOpen = signal(false);
  readonly walletModalOpen = signal(false);
  readonly inviteUserModalOpen = signal(false);

  readonly careerCode = signal('');
  readonly careerName = signal('');
  readonly studentReference = signal('');
  readonly studentYear = signal<number | null>(null);
  readonly studentWallet = signal('');
  readonly walletStudentId = signal('');
  readonly walletAddress = signal('');
  readonly inviteEmail = signal('');
  readonly inviteRole = signal<InstitutionRole>('viewer');
  readonly editInstitutionLegalName = signal('');
  readonly editInstitutionDisplayName = signal('');
  readonly editInstitutionCountryCode = signal(PLATFORM_DEFAULT_COUNTRY_CODE);
  readonly editInstitutionWebsiteUrl = signal('');
  readonly editInstitutionIssuerWallet = signal('');
  readonly editInstitutionIsActive = signal(true);

  readonly studentsWithWalletCount = computed(
    () => this.students().filter((student) => Boolean(student.primaryWalletAddress)).length,
  );

  readonly studentsWithoutWalletCount = computed(
    () => this.students().length - this.studentsWithWalletCount(),
  );

  readonly activeStudentsCount = computed(
    () => this.students().filter((student) => student.isActive).length,
  );

  readonly activeCareersCount = computed(
    () => this.careers().filter((career) => career.isActive).length,
  );

  readonly inactiveCareersCount = computed(
    () => this.careers().length - this.activeCareersCount(),
  );

  readonly institutionAdminCount = computed(
    () => this.institutionUsers().filter((user) => user.role === 'admin').length,
  );

  ngOnInit(): void {
    const queryInstitutionId = this.route.snapshot.queryParamMap.get('institutionId');
    const defaultInstitutionId =
      queryInstitutionId ?? this.authService.getMemberships()[0]?.institutionId ?? '';

    this.selectedInstitutionId.set(defaultInstitutionId);
    this.activeTab.set(this.defaultTab());
    void this.bootstrap();
  }

  async bootstrap(): Promise<void> {
    if (this.authService.hasPlatformAdmin()) {
      await this.loadInstitutions();
    }

    if (this.selectedInstitutionId()) {
      await this.loadSelectedInstitution();
    }
  }

  async loadInstitutions(): Promise<void> {
    try {
      this.institutions.set(
        await withMinimumVisualDelay(this.academyService.listInstitutions()),
      );
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    }
  }

  async onInstitutionChange(institutionId: string): Promise<void> {
    this.selectedInstitutionId.set(institutionId);
    await this.loadSelectedInstitution();
  }

  async selectInstitution(institutionId: string): Promise<void> {
    this.selectedInstitutionId.set(institutionId);
    await this.loadSelectedInstitution();
  }

  setActiveTab(tab: AcademyTab): void {
    if (tab === 'students' && !this.canViewAcademyManagement()) {
      return;
    }

    if (tab === 'careers' && !this.canViewAcademyManagement()) {
      return;
    }

    if (tab === 'users' && !this.canManageInstitution()) {
      return;
    }

    if (tab === 'reports' && !this.canViewAcademyManagement()) {
      return;
    }

    if (tab === 'issuer' && !this.canUseIssuerTab()) {
      return;
    }

    this.activeTab.set(tab);
    this.selectedCareer.set(null);
    this.selectedStudent.set(null);
    this.selectedUser.set(null);
  }

  selectCareer(career: CareerSummary): void {
    if (this.selectedCareer()?.id === career.id) {
      this.selectedCareer.set(null);
      return;
    }

    this.selectedCareer.set(career);
  }

  selectStudent(student: StudentSummary): void {
    if (this.selectedStudent()?.id === student.id) {
      this.selectedStudent.set(null);
      return;
    }

    this.selectedStudent.set(student);
  }

  selectUser(user: InstitutionUserSummary): void {
    if (this.selectedUser()?.id === user.id) {
      this.selectedUser.set(null);
      return;
    }

    this.selectedUser.set(user);
  }

  openCreateStudentModal(): void {
    this.createStudentModalOpen.set(true);
  }

  openCareerModal(career?: CareerSummary): void {
    this.editingCareer.set(career ?? null);
    this.careerCode.set(career?.code ?? '');
    this.careerName.set(career?.name ?? '');
    this.careerModalOpen.set(true);
  }

  openEditInstitutionModal(): void {
    const institution = this.selectedInstitution();
    if (!institution) {
      return;
    }

    this.editInstitutionLegalName.set(institution.legalName);
    this.editInstitutionDisplayName.set(institution.displayName);
    this.editInstitutionCountryCode.set(institution.countryCode || PLATFORM_DEFAULT_COUNTRY_CODE);
    this.editInstitutionWebsiteUrl.set(institution.websiteUrl ?? '');
    this.editInstitutionIssuerWallet.set(institution.issuerWalletAddress ?? '');
    this.editInstitutionIsActive.set(institution.isActive);
    this.editInstitutionModalOpen.set(true);
  }

  closeEditInstitutionModal(): void {
    if (!this.submitting()) {
      this.editInstitutionModalOpen.set(false);
    }
  }

  closeCareerModal(force = false): void {
    if (force || !this.submitting()) {
      this.careerModalOpen.set(false);
      this.editingCareer.set(null);
      this.careerCode.set('');
      this.careerName.set('');
    }
  }

  closeCreateStudentModal(): void {
    if (!this.submitting()) {
      this.createStudentModalOpen.set(false);
    }
  }

  openWalletModal(studentId?: string): void {
    if (studentId) {
      this.walletStudentId.set(studentId);
    }
    this.walletModalOpen.set(true);
  }

  closeWalletModal(): void {
    if (!this.submitting()) {
      this.walletModalOpen.set(false);
    }
  }

  openInviteUserModal(): void {
    this.inviteUserModalOpen.set(true);
  }

  closeInviteUserModal(): void {
    if (!this.submitting()) {
      this.inviteUserModalOpen.set(false);
    }
  }

  async loadSelectedInstitution(): Promise<void> {
    const institutionId = this.selectedInstitutionId().trim();
    if (!institutionId) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.selectedCareer.set(null);
    this.selectedStudent.set(null);
    this.selectedUser.set(null);

    try {
      const { institution, careers, students, users } = await withMinimumVisualDelay(
        (async () => {
          const institution = await this.academyService.getInstitution(institutionId);
          this.selectedInstitution.set(institution as InstitutionSummary);
          this.ensureActiveTabAllowed();
          const [careers, students] = await Promise.all([
            this.academyService.listCareers(institutionId),
            this.academyService.listStudents(institutionId),
          ]);
          const users = this.canManageInstitution()
            ? await this.academyService.listInstitutionUsers(institutionId)
            : [];

          return { institution, careers, students, users };
        })(),
      );

      this.selectedInstitution.set(institution as InstitutionSummary);
      this.careers.set(careers);
      this.students.set(students);
      this.institutionUsers.set(users);
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  activeRoleLabel(): string {
    if (this.authService.hasPlatformAdmin()) {
      return 'platform_admin';
    }

    return this.currentInstitutionRole() ?? this.translate.instant('academy.noRole');
  }

  formatDate(value?: string | null): string {
    if (!value) {
      return '-';
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? value
      : new Intl.DateTimeFormat(this.languageService.locale(), {
        dateStyle: 'medium',
      }).format(date);
  }

  activeUserName(): string {
    const currentAddress = this.authService.getAddress()?.toLowerCase();
    if (!currentAddress) {
      return this.authService.getUserDisplayName();
    }

    const user = this.institutionUsers().find(
      (item) => item.walletAddress?.toLowerCase() === currentAddress,
    );

    return user?.displayName || user?.email || this.authService.getUserDisplayName();
  }

  activeRoleBadgeClass(): string {
    const role = this.activeRoleLabel();
    if (role === 'platform_admin') {
      return 'border-violet-500/40 bg-violet-500/10 text-violet-200';
    }

    if (role === 'admin') {
      return 'border-blue-500/40 bg-blue-500/10 text-blue-200';
    }

    if (role === 'issuer') {
      return 'border-emerald-500/40 bg-emerald-500/10 text-emerald-200';
    }

    if (role === 'viewer') {
      return 'border-slate-500/50 bg-slate-700/40 text-slate-200';
    }

    return 'border-amber-500/40 bg-amber-500/10 text-amber-200';
  }

  activeUserName(): string {
    const currentAddress = this.authService.getAddress()?.toLowerCase();
    if (!currentAddress) {
      return this.authService.getUserDisplayName();
    }

    const user = this.institutionUsers().find(
      (item) => item.walletAddress?.toLowerCase() === currentAddress,
    );

    return user?.displayName || user?.email || this.authService.getUserDisplayName();
  }

  activeRoleBadgeClass(): string {
    const role = this.activeRoleLabel();
    if (role === 'platform_admin') {
      return 'border-violet-500/40 bg-violet-500/10 text-violet-200';
    }

    if (role === 'admin') {
      return 'border-blue-500/40 bg-blue-500/10 text-blue-200';
    }

    if (role === 'issuer') {
      return 'border-emerald-500/40 bg-emerald-500/10 text-emerald-200';
    }

    if (role === 'viewer') {
      return 'border-slate-500/50 bg-slate-700/40 text-slate-200';
    }

    return 'border-amber-500/40 bg-amber-500/10 text-amber-200';
  }

  canManageInstitution(): boolean {
    return this.authService.hasPlatformAdmin()
      || this.currentInstitutionRole() === 'admin';
  }

  canViewAcademyManagement(): boolean {
    return this.authService.hasPlatformAdmin()
      || ['admin', 'issuer', 'viewer'].includes(this.currentInstitutionRole() ?? '');
  }

  canUseIssuerTab(): boolean {
    return this.authService.hasPlatformAdmin()
      || ['admin', 'issuer'].includes(this.currentInstitutionRole() ?? '');
  }

  canIssueCredentials(): boolean {
    return this.canUseIssuerTab();
  }

  async handleUpdateInstitution(event: Event): Promise<void> {
    event.preventDefault();
    const institution = this.selectedInstitution();
    const institutionId = this.selectedInstitutionId().trim();
    if (!institution?.id || !institutionId || !this.canManageInstitution()) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const issuerWallet = this.normalizeWallet(this.editInstitutionIssuerWallet().trim());
      if (this.editInstitutionIssuerWallet().trim() && !issuerWallet) {
        this.errorMessage.set(this.translate.instant('platform.institutions.invalidIssuerWallet'));
        return;
      }

      await this.academyService.updateInstitution(institutionId, {
        legalName: this.editInstitutionLegalName().trim(),
        displayName: this.editInstitutionDisplayName().trim(),
        countryCode: this.editInstitutionCountryCode().trim() || PLATFORM_DEFAULT_COUNTRY_CODE,
        websiteUrl: this.editInstitutionWebsiteUrl().trim() || null,
        isActive: this.editInstitutionIsActive(),
      });

      if (issuerWallet && issuerWallet !== this.normalizeWallet(institution.issuerWalletAddress ?? '')) {
        await this.linkIssuerWallet(institutionId, issuerWallet);
      }

      const refreshed = await this.academyService.getInstitution(institutionId);
      this.selectedInstitution.set(refreshed as InstitutionSummary);
      if (this.authService.hasPlatformAdmin()) {
        await this.loadInstitutions();
      }

      this.editInstitutionModalOpen.set(false);
      this.successMessage.set(this.translate.instant('platform.institutions.updatedSuccess', { institution: refreshed.displayName }));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  async handleSaveCareer(event: Event): Promise<void> {
    event.preventDefault();
    const institutionId = this.selectedInstitutionId().trim();
    const code = this.careerCode().trim();
    const name = this.careerName().trim();
    if (!institutionId || !code || !name) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const editing = this.editingCareer();
      const career = editing
        ? await this.academyService.updateCareer(institutionId, editing.id, { code, name })
        : await this.academyService.createCareer(institutionId, { code, name });

      this.careers.set(await this.academyService.listCareers(institutionId));
      this.selectedCareer.set(career);
      this.closeCareerModal(true);
      this.successMessage.set(this.translate.instant('academy.messages.careerSaved', { code: career.code }));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  async handleDeactivateCareer(career: CareerSummary): Promise<void> {
    const institutionId = this.selectedInstitutionId().trim();
    if (!institutionId || !career.isActive) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const updatedCareer = await this.academyService.deactivateCareer(institutionId, career.id);
      this.careers.set(await this.academyService.listCareers(institutionId));
      this.selectedCareer.set(updatedCareer);
      this.successMessage.set(this.translate.instant('academy.messages.careerDisabled', { code: updatedCareer.code }));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  async handleCreateStudent(event: Event): Promise<void> {
    event.preventDefault();
    const institutionId = this.selectedInstitutionId().trim();
    if (!institutionId) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const student = await this.academyService.createStudentDirect(institutionId, {
        externalReference: this.studentReference().trim() || null,
        enrollmentYear: this.studentYear(),
        walletAddress: this.studentWallet().trim() || null,
      });
      this.studentReference.set('');
      this.studentYear.set(null);
      this.studentWallet.set('');
      this.createStudentModalOpen.set(false);
      this.students.set(await this.academyService.listStudents(institutionId));
      this.selectedStudent.set(student);
      this.successMessage.set(this.translate.instant('academy.messages.studentCreated', { student: student.externalReference ?? student.id }));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  async handleAddWallet(event: Event): Promise<void> {
    event.preventDefault();
    const institutionId = this.selectedInstitutionId().trim();
    const studentId = this.walletStudentId().trim();
    const walletAddress = this.walletAddress().trim();
    if (!institutionId || !studentId || !walletAddress) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const wallet = await this.academyService.addStudentWallet(institutionId, studentId, {
        walletAddress,
        makePrimary: true,
      });
      this.walletStudentId.set('');
      this.walletAddress.set('');
      this.walletModalOpen.set(false);
      this.students.set(await this.academyService.listStudents(institutionId));
      const refreshedStudent = this.students().find((student) => student.id === studentId);
      this.selectedStudent.set(refreshedStudent ?? this.selectedStudent());
      this.successMessage.set(this.translate.instant('academy.messages.walletLinked', { wallet: wallet.walletAddress }));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  async handleInviteUser(event: Event): Promise<void> {
    event.preventDefault();
    const institutionId = this.selectedInstitutionId().trim();
    const email = this.inviteEmail().trim();
    if (!institutionId || !email) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const invitation = await this.academyService.inviteInstitutionUser(institutionId, {
        email,
        role: this.inviteRole(),
      });
      this.inviteEmail.set('');
      this.inviteUserModalOpen.set(false);
      this.successMessage.set(this.translate.instant('academy.messages.invitationSent', { role: invitation.role, email: invitation.email }));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  async handleRoleChange(
    user: InstitutionUserSummary,
    role: InstitutionRole,
  ): Promise<void> {
    const institutionId = this.selectedInstitutionId().trim();
    if (!institutionId || user.role === role) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const updatedUser = await this.academyService.updateInstitutionUserRole(
        institutionId,
        user.userId,
        { role },
      );
      this.institutionUsers.set(
        await this.academyService.listInstitutionUsers(institutionId),
      );
      const refreshedUser = this.institutionUsers().find((item) => item.userId === user.userId);
      this.selectedUser.set(refreshedUser ?? updatedUser);
      this.successMessage.set(this.translate.instant('academy.messages.roleUpdated', { role }));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  async handleRevokeUser(user: InstitutionUserSummary): Promise<void> {
    const institutionId = this.selectedInstitutionId().trim();
    if (!institutionId) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      await this.academyService.revokeInstitutionUser(institutionId, user.userId);
      this.institutionUsers.set(
        await this.academyService.listInstitutionUsers(institutionId),
      );
      this.selectedUser.set(null);
      this.successMessage.set(this.translate.instant('academy.messages.accessRevoked'));
    } catch (error: unknown) {
      this.errorMessage.set(toErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  handleLogout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  private currentInstitutionRole(): InstitutionRole | null {
    const institutionId = this.selectedInstitutionId();
    const membership = this.authService
      .getMemberships()
      .find((item) => item.institutionId === institutionId);

    if (!membership) {
      return null;
    }

    const role = membership.role.toLowerCase();
    return INSTITUTION_ROLES.includes(role as InstitutionRole)
      ? role as InstitutionRole
      : null;
  }

  private defaultTab(): AcademyTab {
    if (this.canViewAcademyManagement()) {
      return 'students';
    }

    if (this.canUseIssuerTab()) {
      return 'issuer';
    }

    return 'students';
  }

  private ensureActiveTabAllowed(): void {
    const tab = this.activeTab();
    const isAllowed = (tab === 'students' && this.canViewAcademyManagement())
      || (tab === 'careers' && this.canViewAcademyManagement())
      || (tab === 'users' && this.canManageInstitution())
      || (tab === 'reports' && this.canViewAcademyManagement())
      || (tab === 'issuer' && this.canUseIssuerTab());

    if (!isAllowed) {
      this.activeTab.set(this.defaultTab());
      this.selectedCareer.set(null);
      this.selectedStudent.set(null);
      this.selectedUser.set(null);
    }
  }

  private async linkIssuerWallet(institutionId: string, walletAddress: string): Promise<void> {
    await this.academyService.linkInstitutionIssuerWallet(institutionId, {
      walletAddress,
      did: `did:ethr:sepolia:${walletAddress}`,
      publicKey: null,
    });
  }

  private normalizeWallet(value: string): string | null {
    const trimmed = value.trim();
    return /^0x[a-fA-F0-9]{40}$/.test(trimmed)
      ? trimmed.toLowerCase()
      : null;
  }
}
