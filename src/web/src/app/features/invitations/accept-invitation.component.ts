import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, WritableSignal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { InstitutionInvitationAccepted } from '../../api/bff/models/institution-invitation-accepted';
import { AcademyService } from '../../core/services/academy.service';
import { Web3Service } from '../../core/services/web3.service';
import { toErrorMessage } from '../../core/utils/error.utils';

type AcceptState = 'idle' | 'loading' | 'success' | 'error';

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-900 px-4">
      <div class="max-w-lg w-full bg-slate-800 border border-slate-700 rounded-xl shadow-lg p-8">
        <div class="text-center mb-8">
          <h1 class="text-2xl font-bold text-white mb-2">SovereignID</h1>
          <p class="text-slate-400">Aceptar invitación institucional</p>
        </div>

        @if (!invitationToken()) {
          <div
            class="p-4 bg-red-900/40 border border-red-700 rounded-lg text-red-100 text-sm"
          >
            <p class="font-semibold">Token de invitación no encontrado</p>
            <p class="mt-1">
              El enlace debe incluir el parámetro
              <code class="text-red-200">?token=...</code>.
            </p>
          </div>
        } @else {
          @if (state() === 'loading') {
            <div class="text-center py-6">
              <p class="text-slate-300">Procesando invitación con MetaMask...</p>
            </div>
          }

          @if (state() === 'success' && accepted()) {
            <div class="space-y-4">
              <div class="p-4 bg-emerald-900/30 border border-emerald-700 rounded-lg">
                <p class="text-emerald-300 font-semibold">
                  Invitación aceptada correctamente
                </p>
                <dl class="mt-3 space-y-2 text-sm text-slate-300">
                  <div class="flex justify-between gap-4">
                    <dt class="text-slate-400">Institución</dt>
                    <dd class="font-mono text-right break-all">
                      {{ accepted()!.institutionId }}
                    </dd>
                  </div>
                  <div class="flex justify-between gap-4">
                    <dt class="text-slate-400">Rol</dt>
                    <dd>{{ accepted()!.role }}</dd>
                  </div>
                  <div class="flex justify-between gap-4">
                    <dt class="text-slate-400">DID</dt>
                    <dd class="font-mono text-right break-all text-xs">
                      {{ accepted()!.did }}
                    </dd>
                  </div>
                </dl>
              </div>
              <a
                routerLink="/login"
                class="block w-full text-center bg-blue-600 hover:bg-blue-700 text-white font-semibold py-3 px-4 rounded-lg transition"
              >
                Ir a iniciar sesión
              </a>
            </div>
          }

          @if (state() === 'error') {
            <div class="space-y-4">
              <div
                class="p-4 bg-red-900/40 border border-red-700 rounded-lg text-red-100 text-sm"
              >
                {{ errorMessage() }}
              </div>
              <button
                type="button"
                class="w-full bg-slate-700 hover:bg-slate-600 text-white font-semibold py-3 px-4 rounded-lg transition"
                (click)="resetState()"
              >
                Reintentar
              </button>
            </div>
          }

          @if (state() === 'idle') {
            @if (!web3Service.isMetaMaskAvailable()) {
              <div
                class="mb-6 p-4 bg-red-900/40 border border-red-700 rounded-lg text-red-100 text-sm"
              >
                MetaMask no está disponible. Instálalo para vincular tu wallet.
              </div>
            }

            <div class="space-y-4">
              <div>
                <label
                  for="displayName"
                  class="block text-sm font-medium text-slate-300 mb-1"
                >
                  Nombre para mostrar (opcional)
                </label>
                <input
                  id="displayName"
                  type="text"
                  class="w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-white placeholder-slate-500 focus:border-blue-500 focus:outline-none"
                  placeholder="Ej. Admin Institución"
                  [value]="displayName()"
                  (input)="onDisplayNameInput($event)"
                />
              </div>

              @if (connectedWallet()) {
                <div class="rounded-lg border border-orange-500/40 bg-orange-500/10 p-4 text-sm text-slate-200">
                  <p class="font-semibold text-orange-200">Wallet seleccionada</p>
                  <p class="mt-2 break-all font-mono text-xs">{{ connectedWallet() }}</p>
                  <p class="mt-2 text-slate-400">
                    La invitacion quedara asociada a esta wallet. Si no corresponde, cambia la cuenta activa en MetaMask y vuelve a conectar.
                  </p>
                </div>
              }

              <button
                type="button"
                class="w-full bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed text-white font-bold py-3 px-4 rounded-lg transition"
                [disabled]="!web3Service.isMetaMaskAvailable()"
                (click)="handleConnectWallet()"
              >
                {{ connectedWallet() ? 'Cambiar wallet conectada' : 'Conectar MetaMask' }}
              </button>

              <button
                type="button"
                class="w-full bg-orange-500 hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed text-white font-bold py-3 px-4 rounded-lg transition"
                [disabled]="!connectedWallet()"
                (click)="handleAccept()"
              >
                Aceptar invitacion con esta wallet
              </button>
            </div>
          }
        }
      </div>
    </div>
  `,
})
export class AcceptInvitationComponent implements OnInit {
  readonly web3Service = inject(Web3Service);
  private readonly academyService = inject(AcademyService);
  private readonly route = inject(ActivatedRoute);

  readonly invitationToken = signal<string | null>(null);
  readonly displayName = signal('');
  readonly state = signal<AcceptState>('idle');
  readonly errorMessage = signal<string | null>(null);
  readonly accepted = signal<InstitutionInvitationAccepted | null>(null);
  readonly connectedWallet = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    this.invitationToken.set(token);
  }

  onDisplayNameInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.displayName.set(target.value);
  }

  async handleAccept(): Promise<void> {
    const token = this.invitationToken();
    const walletAddress = this.connectedWallet();
    if (!token || !walletAddress) {
      return;
    }

    this.state.set('loading');
    this.errorMessage.set(null);

    try {
      const displayName = this.displayName().trim();
      const result = await this.academyService.acceptInvitationPublic({
        token,
        walletAddress,
        displayName: displayName || null,
      });

      this.accepted.set(result);
      this.state.set('success');
    } catch (error: unknown) {
      this.state.set('error');
      this.errorMessage.set(toErrorMessage(error));
    }
  }

  async handleConnectWallet(): Promise<void> {
    this.errorMessage.set(null);

    try {
      const walletAddress = await this.web3Service.connectWallet();
      if (!walletAddress) {
        throw new Error('No se pudo conectar la wallet');
      }

      this.connectedWallet.set(walletAddress);
    } catch (error: unknown) {
      this.state.set('error');
      this.errorMessage.set(toErrorMessage(error));
    }
  }

  resetState(): void {
    this.state.set('idle');
    this.errorMessage.set(null);
  }
}
