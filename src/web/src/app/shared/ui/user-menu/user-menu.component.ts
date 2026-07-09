import { CommonModule } from '@angular/common';
import { Component, HostListener, inject, input, output, signal } from '@angular/core';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relative" (click)="$event.stopPropagation()">
      <button
        type="button"
        class="flex max-w-72 items-center gap-3 rounded-lg border border-slate-600 bg-slate-700/50 px-3 py-2 text-left text-sm text-slate-200 transition hover:bg-slate-700 hover:text-white"
        aria-haspopup="menu"
        [attr.aria-expanded]="isOpen()"
        (click)="toggle()"
      >
        <span
          class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-600 text-xs font-bold uppercase text-white"
        >
          {{ initials() }}
        </span>
        <span class="min-w-0">
          <span class="block truncate font-semibold">{{ resolvedDisplayName() }}</span>
          @if (role()) {
            <span class="mt-0.5 block text-xs text-slate-400">{{ role() }}</span>
          }
        </span>
        <svg class="h-4 w-4 shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      @if (isOpen()) {
        <div
          class="absolute right-0 z-50 mt-2 w-64 overflow-hidden rounded-lg border border-slate-700 bg-slate-800 shadow-xl shadow-black/30"
          role="menu"
        >
          <div class="border-b border-slate-700 px-4 py-3">
            <p class="truncate text-sm font-semibold text-white">{{ resolvedDisplayName() }}</p>
            <p class="mt-1 truncate font-mono text-xs text-slate-400">{{ authService.getShortAddress() }}</p>
            @if (role()) {
              <span class="mt-2 inline-flex rounded-full border border-blue-500/30 bg-blue-500/10 px-2 py-0.5 text-xs font-semibold text-blue-200">
                {{ role() }}
              </span>
            }
          </div>
          <button
            type="button"
            class="flex w-full cursor-not-allowed items-center justify-between px-4 py-3 text-left text-sm text-slate-500"
            disabled
            role="menuitem"
          >
            Perfil
            <span class="text-xs">Pendiente</span>
          </button>
          <button
            type="button"
            class="flex w-full items-center px-4 py-3 text-left text-sm font-medium text-slate-200 hover:bg-slate-700 hover:text-white"
            role="menuitem"
            (click)="handleLogout()"
          >
            Cerrar sesion
          </button>
        </div>
      }
    </div>
  `,
})
export class UserMenuComponent {
  readonly authService = inject(AuthService);

  readonly displayName = input<string | null>(null);
  readonly role = input<string | null>(null);
  readonly logout = output<void>();

  readonly isOpen = signal(false);

  @HostListener('document:click')
  close(): void {
    this.isOpen.set(false);
  }

  toggle(): void {
    this.isOpen.update((value) => !value);
  }

  handleLogout(): void {
    this.isOpen.set(false);
    this.logout.emit();
  }

  resolvedDisplayName(): string {
    const name = this.displayName()?.trim();
    return name || this.authService.getUserDisplayName();
  }

  initials(): string {
    const source = this.resolvedDisplayName();
    const words = source
      .replace(/^0x/i, '')
      .split(/\s+/)
      .filter(Boolean);

    if (words.length >= 2) {
      return `${words[0][0]}${words[1][0]}`.toUpperCase();
    }

    return source.slice(0, 2).toUpperCase();
  }
}
