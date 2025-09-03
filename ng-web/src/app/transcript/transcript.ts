import { Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { AiMessage } from '../chat-client/ai-message';
import { ChatMessage } from '../chat-message/chat-message';

@Component({
  selector: 'app-transcript',
  imports: [ChatMessage, MatCardModule, MatButtonModule],
  templateUrl: './transcript.html',
  styleUrl: './transcript.scss'
})
export class Transcript {
  transcript = input.required<AiMessage[]>();
}
