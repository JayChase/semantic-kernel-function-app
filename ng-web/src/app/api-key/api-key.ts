import { CommonModule } from '@angular/common';
import {
  Component,
  DestroyRef,
  inject,
  OnInit,
  output,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-api-key',
  imports: [
    CommonModule,

    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatCardModule
  ],
  templateUrl: './api-key.html',
  styleUrl: './api-key.scss'
})
export class ApiKey implements OnInit {
  private destroyRef = inject(DestroyRef);
  protected readonly getKeyCommand = environment.aiApi.getKeyCommand;
  protected readonly keyInputType = signal<'password' | 'text'>('password');

  apiKey = output<string | undefined | null>();

  key = new FormControl<string | null>(null, {
    validators: []
  });

  ngOnInit(): void {
    this.key.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.apiKey.emit(value));
  }
}
