import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  CareerSummary,
  InstitutionRole,
  InstitutionSummary,
  InstitutionUserSummary,
  StudentSummary,
} from '../../../core/models/academy.models';
import { AcademyService } from '../../../core/services/academy.service';
import { AuthService } from '../../../core/services/auth.service';
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
      portalLabel="Academy Portal"
      title="Gestion academica"
      subtitle="Instituciones, estudiantes, usuarios y wallets segun tu rol"
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
            <h2 class="text-2xl font-bold text-white">Gestion academica</h2>
            <p class="mt-1 text-sm text-slate-400">
              Instituciones, estudiantes, usuarios y wallets segun tu rol
            </p>
          </div>

          <div class="min-w-0 border-slate-700 lg:border-l lg:pl-5">
            <p class="text-xs font-medium uppercase text-blue-300">Institucion activa</p>
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
              {{ loading() ? 'Cargando...' : 'Refrescar' }}
            </button>
            @if (activeTab() === 'students' && canManageInstitution()) {
              <button
                type="button"
                class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500"
                (click)="openCreateStudentModal()"
              >
                Crear estudiante
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
                Invitar usuario
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
              Estudiantes
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
              Carreras
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
              Usuarios
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
              Reportes
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
              Emision
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
                <h3 class="text-base font-semibold text-white">Estudiantes</h3>
                <p class="text-xs text-slate-400">{{ students().length }} registros</p>
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
                          {{ student.externalReference || 'Sin referencia' }}
                        </p>
                        <p class="mt-1 truncate font-mono text-xs text-slate-500">{{ student.id }}</p>
                      </div>
                      <app-status-badge
                        [label]="student.isActive ? 'Activo' : 'Inactivo'"
                        [tone]="student.isActive ? 'success' : 'danger'"
                      />
                    </div>
                    <div class="mt-3 grid gap-2 text-xs text-slate-400 md:grid-cols-2">
                      <span>Ano: {{ student.enrollmentYear || '-' }}</span>
                      <span class="truncate font-mono">{{ student.primaryWalletAddress || 'Sin wallet' }}</span>
                    </div>
                  </button>
                } @empty {
                  @if (loading()) {
                    <div class="flex min-h-48 flex-col items-center justify-center gap-4 p-10 text-center text-sm text-slate-400">
                      <app-hex-loader label="Cargando estudiantes" />
                      <p>Cargando estudiantes...</p>
                    </div>
                  } @else {
                    <p class="p-10 text-center text-sm text-slate-400">
                      No hay estudiantes registrados.
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
                      <p class="text-xs font-medium uppercase text-blue-300">Estudiante seleccionado</p>
                      <h3 class="mt-1 truncate text-xl font-semibold text-white">
                        {{ selectedStudent()!.externalReference || 'Sin referencia' }}
                      </h3>
                    </div>
                    <button
                      type="button"
                      class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                      aria-label="Cerrar detalle"
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
                      <dt class="text-slate-500">Ano ingreso</dt>
                      <dd class="text-slate-200">{{ selectedStudent()!.enrollmentYear || '-' }}</dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">Wallet primaria</dt>
                      <dd><app-copy-value [value]="selectedStudent()!.primaryWalletAddress" /></dd>
                    </div>
                  </dl>
                  @if (canManageInstitution()) {
                    <button
                      type="button"
                      class="w-full rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
                      (click)="openWalletModal(selectedStudent()!.id)"
                    >
                      Vincular wallet
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
                      Resumen
                    </button>
                    <button
                      type="button"
                      class="border-b-2 px-3 py-2 text-sm font-medium"
                      [ngClass]="infoPanelTab() === 'institution'
                        ? 'border-blue-500 text-blue-300'
                        : 'border-transparent text-slate-400 hover:text-white'"
                      (click)="infoPanelTab.set('institution')"
                    >
                      Institucion
                    </button>
                  </div>

                  @if (infoPanelTab() === 'summary') {
                  <div>
                    <p class="text-xs font-medium uppercase text-blue-300">Resumen estudiantes</p>
                    <h3 class="mt-1 text-xl font-semibold text-white">Vista general</h3>
                    <p class="mt-1 text-sm text-slate-400">Selecciona un estudiante para revisar su detalle.</p>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">Total</p>
                      <p class="mt-2 text-2xl font-bold text-white">{{ students().length }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">Con wallet</p>
                      <p class="mt-2 text-2xl font-bold text-emerald-300">{{ studentsWithWalletCount() }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">Sin wallet</p>
                      <p class="mt-2 text-2xl font-bold text-amber-300">{{ studentsWithoutWalletCount() }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">Activos</p>
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
                <h3 class="text-base font-semibold text-white">Pool de carreras</h3>
                <p class="text-xs text-slate-400">{{ careers().length }} carreras institucionales</p>
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
                        [label]="career.isActive ? 'Activa' : 'Inactiva'"
                        [tone]="career.isActive ? 'success' : 'danger'"
                      />
                    </div>
                    <div class="mt-3 flex items-center gap-2 text-xs text-slate-400">
                      <span class="h-1.5 w-1.5 rounded-full" [ngClass]="career.isActive ? 'bg-emerald-400' : 'bg-red-400'"></span>
                      <span>{{ career.isActive ? 'Disponible para futuras emisiones' : 'Fuera del flujo de emision' }}</span>
                    </div>
                  </button>
                } @empty {
                  <div class="p-10 text-center">
                    @if (loading()) {
                      <div class="flex min-h-40 flex-col items-center justify-center gap-4 text-sm text-slate-400">
                        <app-hex-loader label="Cargando carreras" />
                        <p>Cargando carreras...</p>
                      </div>
                    } @else {
                      <p class="text-sm font-medium text-white">
                        Aun no hay carreras en esta institucion.
                      </p>
                    }
                    @if (!loading() && canManageInstitution()) {
                      <button
                        type="button"
                        class="mt-4 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500"
                        (click)="openCareerModal()"
                      >
                        Crear primera carrera
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
                      <p class="text-xs font-medium uppercase text-blue-300">Carrera seleccionada</p>
                      <h3 class="mt-1 truncate text-xl font-semibold text-white">
                        {{ selectedCareer()!.name }}
                      </h3>
                    </div>
                    <button
                      type="button"
                      class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                      aria-label="Cerrar detalle"
                      (click)="selectedCareer.set(null)"
                    >
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                  <div class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                    <p class="text-xs text-slate-500">Estado operativo</p>
                    <p class="mt-2 text-sm font-semibold" [ngClass]="selectedCareer()!.isActive ? 'text-emerald-300' : 'text-red-300'">
                      {{ selectedCareer()!.isActive ? 'Activa para emision' : 'Inactiva' }}
                    </p>
                  </div>
                  <dl class="space-y-4 text-sm">
                    <div>
                      <dt class="text-slate-500">Codigo</dt>
                      <dd class="font-mono text-slate-200">{{ selectedCareer()!.code }}</dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">ID</dt>
                      <dd><app-copy-value [value]="selectedCareer()!.id" /></dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">Creada</dt>
                      <dd class="text-slate-200">{{ selectedCareer()!.createdAt | date: 'mediumDate' }}</dd>
                    </div>
                  </dl>

                  @if (canManageInstitution()) {
                    <div class="space-y-3 border-t border-slate-700 pt-5">
                      <button
                        type="button"
                        class="w-full rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600"
                        (click)="openCareerModal(selectedCareer()!)"
                      >
                        Editar carrera
                      </button>
                      @if (selectedCareer()!.isActive) {
                        <button
                          type="button"
                          class="w-full rounded-lg border border-red-500/40 px-4 py-2.5 text-sm font-semibold text-red-300 hover:bg-red-500/10"
                          (click)="handleDeactivateCareer(selectedCareer()!)"
                          [disabled]="submitting()"
                        >
                          Desactivar carrera
                        </button>
                      }
                    </div>
                  }
                </div>
              } @else {
                <div class="space-y-6">
                  <div>
                    <p class="text-xs font-medium uppercase text-blue-300">Catalogo academico</p>
                    <h3 class="mt-1 text-xl font-semibold text-white">Carreras institucionales</h3>
                    <p class="mt-1 text-sm text-slate-400">
                      Este pool alimentara el flujo de emision de titulos en el modulo issuer.
                    </p>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">Activas</p>
                      <p class="mt-2 text-2xl font-bold text-emerald-300">{{ activeCareersCount() }}</p>
                    </article>
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">Inactivas</p>
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
                <h3 class="text-base font-semibold text-white">Usuarios institucionales</h3>
                <p class="text-xs text-slate-400">{{ institutionUsers().length }} usuarios activos</p>
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
                      <app-hex-loader label="Cargando usuarios" />
                      <p>Cargando usuarios...</p>
                    </div>
                  } @else {
                    <p class="p-10 text-center text-sm text-slate-400">
                      No hay usuarios institucionales activos.
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
                      <p class="text-xs font-medium uppercase text-blue-300">Usuario seleccionado</p>
                      <h3 class="mt-1 truncate text-xl font-semibold text-white">
                        {{ selectedUser()!.displayName || selectedUser()!.email || 'Usuario institucional' }}
                      </h3>
                    </div>
                    <button
                      type="button"
                      class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white"
                      aria-label="Cerrar detalle"
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
                      <dt class="text-slate-500">Wallet</dt>
                      <dd><app-copy-value [value]="selectedUser()!.walletAddress" /></dd>
                    </div>
                    <div>
                      <dt class="text-slate-500">Rol</dt>
                      <dd class="text-slate-200">{{ selectedUser()!.role }}</dd>
                    </div>
                  </dl>

                  <div class="space-y-3 border-t border-slate-700 pt-5">
                    <label class="block text-sm font-medium text-slate-300" for="detailUserRole">Cambiar rol</label>
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
                      Revocar acceso
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
                      Resumen
                    </button>
                    <button
                      type="button"
                      class="border-b-2 px-3 py-2 text-sm font-medium"
                      [ngClass]="infoPanelTab() === 'institution'
                        ? 'border-blue-500 text-blue-300'
                        : 'border-transparent text-slate-400 hover:text-white'"
                      (click)="infoPanelTab.set('institution')"
                    >
                      Institucion
                    </button>
                  </div>

                  @if (infoPanelTab() === 'summary') {
                  <div>
                    <p class="text-xs font-medium uppercase text-blue-300">Resumen usuarios</p>
                    <h3 class="mt-1 text-xl font-semibold text-white">Vista general</h3>
                    <p class="mt-1 text-sm text-slate-400">Selecciona un usuario para cambiar rol o revocar acceso.</p>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <article class="rounded-lg border border-slate-700 bg-slate-900/60 p-4">
                      <p class="text-xs text-slate-500">Total</p>
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
          <p class="font-medium text-white">Selecciona una institucion</p>
          <p class="mt-2 text-sm text-slate-400">
            Tu rol define que acciones puedes ejecutar dentro de la institucion.
          </p>
        </section>
      }
      </div>

      <ng-template #institutionContextPanel>
        <div class="space-y-5">
          <div>
            <p class="text-xs font-medium uppercase text-blue-300">Contexto institucional</p>
            <h3 class="mt-1 text-xl font-semibold text-white">
              {{ selectedInstitution()?.displayName }}
            </h3>
            <p class="mt-1 text-sm text-slate-400">
              Cambia de institucion o revisa datos rapidos del contexto activo.
            </p>
          </div>

          <div>
            <label class="mb-1 block text-sm font-medium text-slate-300" for="institutionSelector">
              Institucion
            </label>

            @if (authService.hasPlatformAdmin()) {
              <div class="flex gap-3">
                <input
                  id="institutionSelector"
                  class="min-w-0 flex-1 rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30"
                  placeholder="UUID de institucion"
                  [ngModel]="selectedInstitutionId()"
                  (ngModelChange)="selectedInstitutionId.set($event)"
                />
                <button
                  type="button"
                  class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50"
                  [disabled]="loading() || !selectedInstitutionId().trim()"
                  (click)="loadSelectedInstitution()"
                >
                  Cargar
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
            <p class="text-xs text-slate-500">Rol activo</p>
            <p class="mt-1 text-sm font-semibold text-white">{{ activeRoleLabel() }}</p>
          </div>

          <dl class="space-y-3 text-sm">
            <div>
              <dt class="text-slate-500">Codigo</dt>
              <dd class="text-slate-200">{{ selectedInstitution()?.code || '-' }}</dd>
            </div>
            <div>
              <dt class="text-slate-500">Pais</dt>
              <dd class="text-slate-200">{{ selectedInstitution()?.countryCode || '-' }}</dd>
            </div>
            <div>
              <dt class="text-slate-500">Wallet emisora</dt>
              <dd class="break-all font-mono text-xs text-slate-200">
                <app-copy-value [value]="selectedInstitution()?.issuerWalletAddress" emptyLabel="Sin wallet emisora" />
              </dd>
            </div>
          </dl>

          @if (authService.hasPlatformAdmin() && institutions().length) {
            <div class="border-t border-slate-700 pt-4">
              <p class="mb-3 text-sm font-semibold text-white">Instituciones recientes</p>
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
        [isOpen]="careerModalOpen()"
        [title]="editingCareer() ? 'Editar carrera' : 'Crear carrera'"
        description="Administra el catalogo academico que luego usara issuer para emitir titulos."
        (closed)="closeCareerModal()"
      >
        <form class="grid gap-4" (submit)="handleSaveCareer($event)">
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white" placeholder="Codigo, ej: ING-SW" [ngModel]="careerCode()" name="careerCode" (ngModelChange)="careerCode.set($event)" />
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" placeholder="Nombre de carrera" [ngModel]="careerName()" name="careerName" (ngModelChange)="careerName.set($event)" />
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeCareerModal()">Cancelar</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting() || !careerCode().trim() || !careerName().trim()">
              {{ submitting() ? 'Guardando...' : (editingCareer() ? 'Guardar cambios' : 'Crear carrera') }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="createStudentModalOpen()"
        title="Crear estudiante"
        description="Registra un estudiante dentro de la institucion activa. La wallet es opcional."
        (closed)="closeCreateStudentModal()"
      >
        <form class="grid gap-4" (submit)="handleCreateStudent($event)">
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" placeholder="Referencia externa" [ngModel]="studentReference()" name="studentReference" (ngModelChange)="studentReference.set($event)" />
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" placeholder="Ano ingreso" type="number" [ngModel]="studentYear()" name="studentYear" (ngModelChange)="studentYear.set($event)" />
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" placeholder="Wallet opcional 0x..." [ngModel]="studentWallet()" name="studentWallet" (ngModelChange)="studentWallet.set($event)" />
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeCreateStudentModal()">Cancelar</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting()">
              {{ submitting() ? 'Creando...' : 'Crear estudiante' }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="walletModalOpen()"
        title="Vincular wallet"
        description="Asocia manualmente una wallet existente al estudiante seleccionado."
        (closed)="closeWalletModal()"
      >
        <form class="grid gap-4" (submit)="handleAddWallet($event)">
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 font-mono text-sm text-white" placeholder="Student ID" [ngModel]="walletStudentId()" name="walletStudentId" (ngModelChange)="walletStudentId.set($event)" />
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" placeholder="Wallet 0x..." [ngModel]="walletAddress()" name="walletAddress" (ngModelChange)="walletAddress.set($event)" />
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeWalletModal()">Cancelar</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting() || !walletStudentId().trim() || !walletAddress().trim()">
              {{ submitting() ? 'Vinculando...' : 'Vincular wallet' }}
            </button>
          </div>
        </form>
      </app-modal>

      <app-modal
        [isOpen]="inviteUserModalOpen()"
        title="Invitar usuario"
        description="Invita un usuario institucional y define su rol inicial."
        (closed)="closeInviteUserModal()"
      >
        <form class="grid gap-4" (submit)="handleInviteUser($event)">
          <input class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" placeholder="email@institucion.cl" type="email" [ngModel]="inviteEmail()" name="inviteEmail" (ngModelChange)="inviteEmail.set($event)" required />
          <select class="rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white" [ngModel]="inviteRole()" name="inviteRole" (ngModelChange)="inviteRole.set($event)">
            @for (role of roles; track role) {
              <option [value]="role">{{ role }}</option>
            }
          </select>
          <div class="flex justify-end gap-3">
            <button type="button" class="rounded-lg border border-slate-600 bg-slate-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-600" (click)="closeInviteUserModal()">Cancelar</button>
            <button type="submit" class="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-50" [disabled]="submitting() || !inviteEmail().trim()">
              {{ submitting() ? 'Enviando...' : 'Enviar invitacion' }}
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

    return this.currentInstitutionRole() ?? 'sin rol';
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
      this.successMessage.set(`Carrera ${career.code} guardada correctamente.`);
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
      this.successMessage.set(`Carrera ${updatedCareer.code} desactivada.`);
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
      this.successMessage.set(`Estudiante ${student.externalReference ?? student.id} creado correctamente.`);
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
      this.successMessage.set(`Wallet ${wallet.walletAddress} vinculada correctamente.`);
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
      this.successMessage.set(`Invitacion ${invitation.role} enviada a ${invitation.email}.`);
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
      this.successMessage.set(`Rol actualizado a ${role}.`);
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
      this.successMessage.set('Acceso revocado correctamente.');
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
}
