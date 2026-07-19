import { CommonModule } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { AppLanguage, LanguageService } from '../../../core/services/language.service';

type LanguageSwitcherVariant = 'menu' | 'floating';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <div [ngClass]="containerClass()" role="group" [attr.aria-label]="'common.language.label' | translate">
      @if (showLabel()) {
        <p [ngClass]="labelClass()">{{ 'common.language.label' | translate }}</p>
      }
      <div class="grid grid-cols-2 gap-2">
        <button
          type="button"
          [ngClass]="buttonClass('es')"
          [attr.aria-pressed]="languageService.language() === 'es'"
          (click)="setLanguage('es')"
        >
          <span class="font-bold">ES</span>
          @if (variant() !== 'floating') {
            <span class="text-[11px] opacity-80">{{ 'common.language.spanish' | translate }}</span>
          }
        </button>
        <button
          type="button"
          [ngClass]="buttonClass('en')"
          [attr.aria-pressed]="languageService.language() === 'en'"
          (click)="setLanguage('en')"
        >
          <span class="font-bold">EN</span>
          @if (variant() !== 'floating') {
            <span class="text-[11px] opacity-80">{{ 'common.language.english' | translate }}</span>
          }
        </button>
      </div>
    </div>
  `,
})
export class LanguageSwitcherComponent {
  readonly languageService = inject(LanguageService);

  readonly variant = input<LanguageSwitcherVariant>('menu');
  readonly showLabel = input(true);

  setLanguage(language: AppLanguage): void {
    this.languageService.setLanguage(language);
  }

  containerClass(): string {
    return this.variant() === 'floating'
      ? 'rounded-lg border border-slate-700/80 bg-slate-900/80 p-2 shadow-lg shadow-black/20 backdrop-blur-sm'
      : 'px-4 py-3';
  }

  labelClass(): string {
    return this.variant() === 'floating'
      ? 'mb-2 text-center text-[11px] font-semibold uppercase text-slate-400'
      : 'mb-2 text-xs font-semibold uppercase text-slate-500';
  }

  buttonClass(language: AppLanguage): string {
    const active = this.languageService.language() === language;
    const base = this.variant() === 'floating'
      ? 'flex min-w-14 flex-col items-center justify-center rounded-md border px-2 py-1.5 text-xs transition'
      : 'flex flex-col items-center justify-center rounded-md border px-2 py-1.5 text-xs transition';

    return active
      ? `${base} border-cyan-400/60 bg-cyan-400/15 text-cyan-100`
      : `${base} border-slate-700 bg-slate-950 text-slate-300 hover:border-slate-500 hover:text-white`;
  }
}
