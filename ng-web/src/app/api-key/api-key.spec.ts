import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApiKey } from './api-key';

describe('ApiKey', () => {
  let component: ApiKey;
  let fixture: ComponentFixture<ApiKey>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApiKey]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ApiKey);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
