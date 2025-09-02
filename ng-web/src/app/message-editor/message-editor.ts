import { CommonModule } from '@angular/common';
import { Component, output } from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { AiMessage } from '../chat-client/ai-message';
import { SpeechButtonDirective } from '../speech-button/speech-button';

@Component({
  selector: 'app-message-editor',
  imports: [
    CommonModule,
    MatButtonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatCardModule,
    SpeechButtonDirective
  ],
  templateUrl: './message-editor.html',
  styleUrl: './message-editor.scss'
})
export class MessageEditor {
  formGroup = new FormGroup({
    prompt: new FormControl<string | null>(null, {
      validators: [Validators.required]
    })
  });

  asked = output<AiMessage>();

  ask() {
    const prompt = this.formGroup.controls.prompt.value;

    //clear after request
    // timestamp requests
    // send on enter

    if (prompt) {
      this.asked.emit({
        role: 'user',
        contents: [
          {
            $type: 'text',
            text: prompt
          }
        ]
      });

      this.formGroup.controls.prompt.setValue(null);
      this.formGroup.controls.prompt.markAsPristine();
      this.formGroup.controls.prompt.markAsUntouched();
    }
  }

  write(transcription: string) {
    this.formGroup.controls.prompt.setValue(transcription);
  }

  cancel() {}
}
