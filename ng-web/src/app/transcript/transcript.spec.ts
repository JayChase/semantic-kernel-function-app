import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Transcript } from './transcript';

describe('Transcript', () => {
  let component: Transcript;
  let fixture: ComponentFixture<Transcript>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Transcript]
    }).compileComponents();

    fixture = TestBed.createComponent(Transcript);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
