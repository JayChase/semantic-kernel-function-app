import { TestBed } from '@angular/core/testing';

import { ChatClient } from './chat-client';

describe('ChatClient', () => {
  let service: ChatClient;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ChatClient);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
