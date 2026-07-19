import { Injectable, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'en' | 'es';

const LANGUAGE_STORAGE_KEY = 'sovereignid.language';
const SUPPORTED_LANGUAGES: readonly AppLanguage[] = ['en', 'es'];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly currentLanguage = signal<AppLanguage>(this.resolveInitialLanguage());

  readonly language = this.currentLanguage.asReadonly();
  readonly languageLabel = computed(() => this.currentLanguage() === 'es' ? 'Espanol' : 'English');
  readonly locale = computed(() => this.currentLanguage() === 'es' ? 'es-CL' : 'en-US');

  constructor() {
    this.translate.addLangs([...SUPPORTED_LANGUAGES]);
    this.translate.setFallbackLang('en');
    this.applyLanguage(this.currentLanguage(), false);
  }

  setLanguage(language: AppLanguage): void {
    this.applyLanguage(language, true);
  }

  isSupported(language: string): language is AppLanguage {
    return SUPPORTED_LANGUAGES.includes(language as AppLanguage);
  }

  private applyLanguage(language: AppLanguage, persist: boolean): void {
    this.currentLanguage.set(language);
    this.translate.use(language);
    document.documentElement.lang = language;

    if (persist) {
      localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
    }
  }

  private resolveInitialLanguage(): AppLanguage {
    const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    if (stored && this.isSupported(stored)) {
      return stored;
    }

    const deviceLanguage = navigator.language || navigator.languages?.[0] || '';
    return deviceLanguage.toLowerCase().startsWith('es') ? 'es' : 'en';
  }
}
