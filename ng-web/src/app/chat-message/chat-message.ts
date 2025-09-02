import { Component, input } from '@angular/core';
import { MarkdownModule } from 'ngx-markdown';
import { AiMessage } from '../chat-client/ai-message';

@Component({
  selector: 'app-chat-message',
  imports: [MarkdownModule],
  templateUrl: './chat-message.html',
  styleUrl: './chat-message.scss'
})
export class ChatMessage {
  aiMessage = input.required<AiMessage>();

  text(): string {
    return this.aiMessage()
      .contents.map((content) => content.text)
      .join('');
  }
}
