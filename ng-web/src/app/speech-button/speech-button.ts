import {
  Directive,
  HostListener,
  Inject,
  OnInit,
  output,
  signal
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatSnackBar } from '@angular/material/snack-bar';

import { BehaviorSubject } from 'rxjs';
import { NgWindow, WINDOW } from '../window/window.token';

// https://blog.pamelafox.org/2024/12/add-browser-speech-inputoutput-to-your.html

@Directive({
  selector: '[appSpeechButton]',
  exportAs: 'appSpeechButton',
  standalone: true
})
export class SpeechButtonDirective implements OnInit {
  speechRecognition: SpeechRecognition | null | undefined;

  available = signal<boolean>(false);

  // output recording signal
  private listeningSubject = new BehaviorSubject<boolean>(false);
  private listening$ = this.listeningSubject.asObservable();
  listening = toSignal(this.listening$);

  //output text on mouse down
  transcriptReady = output<string>();

  constructor(
    @Inject(WINDOW) private window: NgWindow,
    private matSnackBar: MatSnackBar
  ) {}

  @HostListener('click', ['$event'])
  onClick(clickEvent: MouseEvent): void {
    this.listeningSubject.next(!this.listeningSubject.value);
  }

  ngOnInit(): void {
    if (this.window?.webkitSpeechRecognition) {
      const SpeechRecognition =
        window.SpeechRecognition || window.webkitSpeechRecognition;

      this.speechRecognition = new SpeechRecognition();

      this.speechRecognition.lang = this.window.navigator.language;
      this.speechRecognition.interimResults = false;
      this.speechRecognition.continuous = true;
      this.speechRecognition.maxAlternatives = 1;

      this.available.set(true);

      this.listening$.subscribe({
        next: (isListening) => {
          if (isListening) {
            this.speechRecognition?.start();
          } else {
            this.speechRecognition?.stop();
          }
        }
      });

      this.speechRecognition.onresult = (event) => {
        const transcript = Array.from(event.results)
          .map((result) => result[0].transcript)
          .join('');
        this.transcriptReady.emit(transcript);
      };

      this.speechRecognition.onend = (event) => {
        this.listeningSubject.next(false);
      };

      this.speechRecognition.onerror = (event) => {
        if (event.error === 'aborted') {
          return;
        }

        let errorMessage: string;

        switch (event.error) {
          case 'no-speech':
            errorMessage =
              'No speech was detected. Please check your system audio settings and try again.';
            break;
          case 'language-not-supported':
            errorMessage =
              'The selected language is not supported. Please try a different language.';
            break;
          default:
            errorMessage =
              'An error occurred recording the audio. Please check your microphone settings and try again.';
            break;
        }

        this.matSnackBar.open(errorMessage, 'Ok', {
          duration: 200
        });
      };
    }
  }
}
