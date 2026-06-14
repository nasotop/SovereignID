import { Buffer } from 'buffer';

// Polyfill global para librerías Web3
(window as any).global = window;
(window as any).Buffer = Buffer;
(window as any).process = { env: { DEBUG: undefined } };
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
