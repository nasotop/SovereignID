import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-900 px-4">
      <div class="max-w-md w-full bg-gray-800 rounded-lg shadow-lg p-8 text-center">
        <h1 class="text-2xl font-bold text-white mb-2">Acceso no autorizado</h1>
        <p class="text-gray-400 mb-6">
          Tu wallet no tiene permisos para acceder a esta seccion.
        </p>
        <a
          routerLink="/login"
          class="inline-block bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 px-4 rounded-lg transition"
        >
          Volver al inicio de sesion
        </a>
      </div>
    </div>
  `,
})
export class UnauthorizedComponent {}
