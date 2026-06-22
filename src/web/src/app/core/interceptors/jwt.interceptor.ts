import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpEvent,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Functional HTTP interceptor that adds JWT to Authorization header
 * for authenticated requests
 */
export const jwtInterceptor: HttpInterceptorFn = (
  request: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);
  const jwt = authService.getJwt();

  if (jwt) {
    request = request.clone({
      setHeaders: {
        Authorization: `Bearer ${jwt}`,
      },
    });
  }

  return next(request);
};
