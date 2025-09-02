import { HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';

export function isHttpResponse<T>(
  httpEvent: HttpEvent<T> | HttpResponse<T> | null | undefined
): httpEvent is HttpResponse<T> {
  return !!httpEvent && httpEvent.type === HttpEventType.Response;
}
