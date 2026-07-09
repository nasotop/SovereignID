import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';

import { UserMenuComponent } from '../user-menu/user-menu.component';

type PortalAccent = 'blue' | 'violet';
type PortalLayoutWidth = 'contained' | 'full';

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  imports: [CommonModule, UserMenuComponent],
  host: {
    class: 'block h-full',
  },
  template: `
    <div class="bg-slate-900" [ngClass]="rootClass()">
      <nav class="shrink-0 border-b border-slate-700/60 bg-slate-800/80 backdrop-blur-sm">
        <div
          class="mx-auto flex items-center justify-between"
          [ngClass]="navContainerClass()"
        >
          <div class="flex items-center gap-3">
            <div
              class="w-9 h-9 rounded-lg flex items-center justify-center"
              [ngClass]="accentClass()"
            >
              <svg
                class="w-5 h-5 text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                aria-hidden="true"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                />
              </svg>
            </div>
            <div>
              <h1 class="text-lg font-bold text-white tracking-tight">
                SovereignID
              </h1>
              <p class="text-xs text-slate-400">{{ portalLabel() }}</p>
            </div>
          </div>

          <app-user-menu
            [displayName]="userName()"
            [role]="userRole()"
            (logout)="logout.emit()"
          />
        </div>
      </nav>

      <main class="mx-auto" [ngClass]="mainClass()">
        @if (!hideHeader()) {
          <header class="shrink-0" [ngClass]="headerClass()">
            <h2 class="text-2xl font-bold text-white">{{ title() }}</h2>
            @if (subtitle()) {
              <p class="text-slate-400 mt-1">{{ subtitle() }}</p>
            }
          </header>
        }

        <div class="flex min-h-0 flex-1 flex-col overflow-hidden">
          <ng-content />
        </div>
      </main>
    </div>
  `,
})
export class PortalShellComponent {
  readonly portalLabel = input.required<string>();
  readonly title = input.required<string>();
  readonly subtitle = input<string>('');
  readonly accent = input<PortalAccent>('blue');
  readonly layoutWidth = input<PortalLayoutWidth>('contained');
  readonly hideHeader = input(false);
  readonly userName = input<string | null>(null);
  readonly userRole = input<string | null>(null);
  readonly logout = output<void>();

  accentClass(): string {
    return this.accent() === 'violet' ? 'bg-violet-600' : 'bg-blue-600';
  }

  containerClass(): string {
    return this.layoutWidth() === 'full'
      ? 'w-full max-w-none 2xl:px-8'
      : 'max-w-7xl';
  }

  rootClass(): string {
    return this.layoutWidth() === 'full'
      ? 'flex h-screen flex-col overflow-hidden'
      : 'min-h-screen';
  }

  navContainerClass(): string {
    return this.layoutWidth() === 'full'
      ? 'w-full max-w-none px-6 py-4 2xl:px-8'
      : 'max-w-7xl px-6 py-4';
  }

  mainClass(): string {
    return this.layoutWidth() === 'full'
      ? 'flex min-h-0 w-full max-w-none flex-1 flex-col overflow-hidden px-6 py-6 2xl:px-8'
      : 'max-w-7xl px-6 py-8';
  }

  headerClass(): string {
    return this.layoutWidth() === 'full' ? 'mb-6' : 'mb-8';
  }
}
