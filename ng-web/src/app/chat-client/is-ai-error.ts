import { AiError } from './ai-error';

export function isAiError(
  data: AiError | unknown | null | undefined
): data is AiError {
  return !!(data as AiError).errorCode && !!(data as AiError).message;
}
