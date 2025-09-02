import {
  HttpDownloadProgressEvent,
  HttpEvent,
  HttpEventType
} from '@angular/common/http';

export function isHttpDownloadProgressEvent<T>(
  httpEvent: HttpEvent<T> | HttpDownloadProgressEvent | null | undefined
): httpEvent is HttpDownloadProgressEvent {
  return !!httpEvent && httpEvent.type === HttpEventType.DownloadProgress;
}
