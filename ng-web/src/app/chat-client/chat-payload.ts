import { ChatMessage } from '../chat-message/chat-message';

export type ChatPayload = {
  utterance: ChatMessage;
  history?: ChatMessage[];
};
