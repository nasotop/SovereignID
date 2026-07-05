import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
  {
    path: 'login',
    component: LoginComponent,
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./features/auth/unauthorized/unauthorized.component').then(
        (m) => m.UnauthorizedComponent,
      ),
  },
  {
    path: 'issuer',
    loadComponent: () =>
      import('./features/portals/issuer/issuer.component').then(
        (m) => m.IssuerComponent,
      ),
    canActivate: [
      authGuard,
      roleGuard({ institutionRoles: ['issuer', 'admin'] }),
    ],
  },
  {
    path: 'holder',
    loadComponent: () =>
      import('./features/portals/holder/holder.component').then(
        (m) => m.HolderComponent,
      ),
    canActivate: [authGuard, roleGuard({ holder: true })],
  },
  {
    path: 'verifier',
    loadComponent: () =>
      import('./features/portals/verifier/verifier.component').then(
        (m) => m.VerifierComponent,
      ),
  },
  {
    path: 'academy',
    loadComponent: () =>
      import('./features/portals/academy/academy.component').then(
        (m) => m.AcademyComponent,
      ),
    canActivate: [
      authGuard,
      roleGuard({
        platformAdmin: true,
        institutionRoles: ['admin', 'issuer', 'viewer'],
        mode: 'any',
      }),
    ],
  },
  {
    path: 'platform',
    loadComponent: () =>
      import('./features/portals/platform/platform.component').then(
        (m) => m.PlatformComponent,
      ),
    canActivate: [authGuard, roleGuard({ platformAdmin: true })],
  },
  {
    path: 'institution-invitations/accept',
    loadComponent: () =>
      import('./features/invitations/accept-invitation.component').then(
        (m) => m.AcceptInvitationComponent,
      ),
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];
