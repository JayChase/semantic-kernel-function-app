import {
  HttpClient,
  HttpDownloadProgressEvent,
  HttpErrorResponse,
  HttpParams,
  HttpResponse
} from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import {
  BehaviorSubject,
  catchError,
  filter,
  map,
  merge,
  Observable,
  of,
  ReplaySubject,
  shareReplay,
  Subject,
  switchMap,
  take
} from 'rxjs';
import { environment } from '../../environments/environment';
import { stateful } from '../stateful/stateful';
import { AiMessage } from './ai-message';
import { isHttpDownloadProgressEvent } from './is-http-download-progress-event';
import { isHttpResponse } from './is-http-response';

@Injectable({
  providedIn: 'root'
})
export class ChatClient {
  private apiKey = signal<string | undefined | null>(null);
  private statusSubject = new ReplaySubject<string>(1);
  status$ = this.statusSubject.asObservable();

  private cancelSubject = new Subject<void>();
  cancel$ = this.cancelSubject.asObservable();

  private busySubject = new BehaviorSubject<boolean>(false);
  busy$ = this.busySubject.asObservable();

  /**
   * the user's voice (utterances as chat requests)
   */
  private userSubject = new Subject<AiMessage>();
  user$ = this.userSubject.asObservable();

  /**
   * the ai's responses (currently chat but it could respond with other actions eg: handing over a file)
   */
  ai$ = this.user$.pipe(switchMap((aiMessage) => this.chat(aiMessage)));

  /**
   * the live conversation
   */
  conversation$ = merge(this.user$, this.ai$);

  //TODO load up history from api on start
  /**
   * the transcription of the live conversation
   */
  private transcriptSubject = new BehaviorSubject<AiMessage[]>([]);

  transcript$ = this.transcriptSubject.asObservable().pipe(
    stateful(this.conversation$, (transcript, aiFeed) => {
      // as the message will be streamed the updated version will pass through here multiple times
      // so remove the existing version and replace it with the latest
      if (!!aiFeed) {
        const updatedTranscript = transcript.filter(
          (aiMessage) => aiMessage.messageId !== aiFeed.messageId
        );

        updatedTranscript.push(aiFeed);

        return updatedTranscript;
      } else {
        return transcript;
      }
    }),
    shareReplay({
      bufferSize: 1,
      refCount: true
    })
  );

  private httpClient = inject(HttpClient);

  constructor() {}

  converse(aiMessage: AiMessage) {
    //TODO enforce user as role
    this.busySubject.next(true);
    this.userSubject.next(aiMessage);
  }

  cancel() {
    this.busy$.pipe(take(1)).subscribe({
      next: (busy) => {
        if (busy) {
          this.cancelSubject.next();
        }
      }
    });
  }

  private chat(chatMessage: AiMessage): Observable<AiMessage> {
    // this will need to take the transcription and add it as the history
    // optionally it could summarize it
    return this.transcript$.pipe(
      take(1),
      switchMap((transcript) => {
        const apiKey = this.apiKey();
        var params = new HttpParams();

        if (apiKey) {
          params = params.append('code', apiKey);
        }

        return this.httpClient.post<string>(
          `${environment.aiApi.uri}/${environment.aiApi.endpoints.chat}`,
          {
            utterance: chatMessage,
            history: transcript
          },
          {
            responseType: 'text' as 'json',
            observe: 'events',
            reportProgress: true,
            params
          }
        );
      }),
      catchError((error: HttpErrorResponse) => {
        this.statusSubject.next(error.message);
        return of(null);
      }),
      filter(
        (httpEvent) =>
          isHttpDownloadProgressEvent(httpEvent) || isHttpResponse(httpEvent)
      ),
      map((httpEvent) => {
        return this.textToAiMessage(httpEvent);
      }),
      map((messages) => {
        console.log(`messages ${messages}`);
        if (messages.length === 0) {
          return null;
        } else {
          const aiMessage = messages[messages.length - 1];
          aiMessage.contents = messages
            .map((message) => message.contents)
            .reduce((acc, arr) => [...acc, ...arr], []);

          return aiMessage;
        }
      }),
      filter((aiMessage) => !!aiMessage)
    );
  }

  private textToAiMessage(
    httpEvent: HttpDownloadProgressEvent | HttpResponse<string>
  ): AiMessage[] {
    const isProgressEvent = isHttpDownloadProgressEvent(httpEvent);
    const text = isProgressEvent ? httpEvent.partialText : httpEvent.body;

    return (
      text
        ?.split('\n')
        .map((line) => {
          try {
            const aIChatCompletionDelta = line
              ? (JSON.parse(line) as AiMessage)
              : undefined;

            if (aIChatCompletionDelta) {
              aIChatCompletionDelta.complete = !isProgressEvent;
            }

            return aIChatCompletionDelta;
          } catch (e) {
            // TODO need to log the error
            console.error('Error parsing AIChatCompletionDelta:', e);
            return null;
          }
        })
        .filter((result) => !!result) ?? []
    );
  }

  setApiKey(apiKey: string | undefined | null) {
    this.apiKey.set(apiKey);
  }
}
