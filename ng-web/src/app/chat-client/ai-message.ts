import { AiRole } from './ai-role';

export type AiMessage = {
  messageId?: string;
  role: AiRole;
  complete?: boolean;
  contents: {
    $type: 'text'; //TODO should this be markdown or html what is the standard?
    text: string;
  }[];
};
