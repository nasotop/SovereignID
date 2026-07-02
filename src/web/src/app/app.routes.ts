import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { authGuard } from './core/guards/auth.guard';

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
    path: 'issuer',
    loadComponent: () =>
      import('./features/portals/issuer/issuer.component').then(
        (m) => m.IssuerComponent
      ),
    canActivate: [authGuard],
  },
  {
    path: 'holder',
    loadComponent: () =>
      import('./features/portals/holder/holder.component').then(
        (m) => m.HolderComponent
      ),
    canActivate: [authGuard],
  },
  {
    path: 'verifier',
    loadComponent: () =>
      import('./features/portals/verifier/verifier.component').then(
        (m) => m.VerifierComponent
      ),
  },
  {
    path: 'platform',
    loadComponent: () =>
      import('./features/portals/platform/platform.component').then(
        (m) => m.PlatformComponent
      ),
    canActivate: [authGuard],
  },
  {
    path: 'institution-invitations/accept',
    loadComponent: () =>
      import('./features/invitations/accept-invitation.component').then(
        (m) => m.AcceptInvitationComponent
      ),
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];
