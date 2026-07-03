import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { RoleGuardOptions } from '../models/auth.models';
import { AuthService } from '../services/auth.service';

export function roleGuard(options: RoleGuardOptions): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (options.platformAdmin && !authService.hasPlatformAdmin()) {
      return router.createUrlTree(['/unauthorized']);
    }

    if (options.holder && !authService.isHolder()) {
      return router.createUrlTree(['/unauthorized']);
    }

    if (options.institutionRoles?.length) {
      const hasRole = authService
        .getMemberships()
        .some((membership) =>
          options.institutionRoles!.includes(membership.role),
        );

      if (!hasRole) {
        return router.createUrlTree(['/unauthorized']);
      }
    }

    return true;
  };
}
