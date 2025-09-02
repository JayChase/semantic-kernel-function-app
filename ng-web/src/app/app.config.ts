import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideMarkdown } from 'ngx-markdown';

import { provideHttpClient, withFetch } from '@angular/common/http';
import { ErrorStateMatcher } from '@angular/material/core';
import {
  provideClientHydration,
  withEventReplay
} from '@angular/platform-browser';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withFetch()),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideMarkdown(),
    {
      provide: ErrorStateMatcher,
      useValue: {
        isErrorState(): boolean {
          return false;
        }
      }
    }
  ]
};
