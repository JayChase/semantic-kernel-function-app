import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Conversation } from './conversation';

describe('Conversation', () => {
  let component: Conversation;
  let fixture: ComponentFixture<Conversation>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Conversation]
    }).compileComponents();

    fixture = TestBed.createComponent(Conversation);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
