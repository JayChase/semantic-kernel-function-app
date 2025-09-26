import { AiMessage } from './ai-message';

export function isAiMessage(
  data: AiMessage | unknown | null | undefined
): data is AiMessage {
  return !!(data as AiMessage).contents;
}
