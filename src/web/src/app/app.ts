import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LanguageService } from './core/services/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
  host: {
    class: 'block h-full',
  },
})
export class App {
  private readonly languageService = inject(LanguageService);
  protected readonly title = signal('sovereignid-web');
}
