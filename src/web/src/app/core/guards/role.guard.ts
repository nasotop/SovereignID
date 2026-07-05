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

    const checks: boolean[] = [];

    if (options.platformAdmin) {
      checks.push(authService.hasPlatformAdmin());
    }

    if (options.holder) {
      checks.push(authService.isHolder());
    }

    if (options.institutionRoles?.length) {
      const hasRole = authService
        .getMemberships()
        .some((membership) =>
          options.institutionRoles!.includes(membership.role),
        );

      checks.push(hasRole);
    }

    const allowed = options.mode === 'any'
      ? checks.some(Boolean)
      : checks.every(Boolean);

    if (!allowed) {
      return router.createUrlTree(['/unauthorized']);
    }

    return true;
  };
}
