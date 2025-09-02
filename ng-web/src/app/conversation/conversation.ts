import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { filter } from 'rxjs';
import { ApiKey } from '../api-key/api-key';
import { AiMessage } from '../chat-client/ai-message';
import { ChatClient } from '../chat-client/chat-client';
import { MessageEditor } from '../message-editor/message-editor';
import { Transcript } from '../transcript/transcript';

@Component({
  selector: 'app-conversation',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    Transcript,
    MessageEditor,
    MatSnackBarModule,
    ApiKey
  ],
  templateUrl: './conversation.html',
  styleUrl: './conversation.scss'
})
export class Conversation implements OnInit {
  private matSnackBar = inject(MatSnackBar);
  private chatClient = inject(ChatClient);
  private destroyRef = inject(DestroyRef);

  transcript$ = this.chatClient.transcript$;

  ngOnInit(): void {
    this.chatClient.status$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        filter((status) => !!status)
      )
      .subscribe({
        next: (status) => {
          this.matSnackBar.open(status, undefined, { duration: 3000 });
        }
      });
  }

  converse(aiMessage: AiMessage) {
    this.chatClient.converse(aiMessage);
  }

  updateKey(apiKey: string | undefined | null) {
    this.chatClient.setApiKey(apiKey);
  }
}
