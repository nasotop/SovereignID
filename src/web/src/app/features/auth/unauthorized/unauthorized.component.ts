import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-900 px-4">
      <div class="max-w-md w-full bg-gray-800 rounded-lg shadow-lg p-8 text-center">
        <h1 class="text-2xl font-bold text-white mb-2">{{ 'unauthorized.title' | translate }}</h1>
        <p class="text-gray-400 mb-6">
          {{ 'unauthorized.body' | translate }}
        </p>
        <a
          routerLink="/login"
          class="inline-block bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 px-4 rounded-lg transition"
        >
          {{ 'unauthorized.back' | translate }}
        </a>
      </div>
    </div>
  `,
})
export class UnauthorizedComponent {}
