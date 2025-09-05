import { Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AiMessage } from '../chat-client/ai-message';
import { ChatMessage } from '../chat-message/chat-message';
import { ScrollHere } from '../scroll-here/scroll-here';

@Component({
  selector: 'app-transcript',
  imports: [
    ChatMessage,
    MatCardModule,
    MatButtonModule,
    ScrollHere,
    MatProgressBarModule
  ],
  templateUrl: './transcript.html',
  styleUrl: './transcript.scss'
})
export class Transcript {
  transcript = input.required<AiMessage[]>();
}
