/// <reference types="dom-speech-recognition" />

import { Platform } from '@angular/cdk/platform';
import { DOCUMENT } from '@angular/common';
import { inject, InjectionToken } from '@angular/core';

export type NgWindow = Window & {
  webkitSpeechRecognition: SpeechRecognition;
};

export const WINDOW = new InjectionToken<NgWindow | null>(
  'window with webkitSpeechRecognition',
  {
    factory: () =>
      inject(Platform).isBrowser
        ? (inject(DOCUMENT).defaultView as unknown as NgWindow)
        : null
  }
);
