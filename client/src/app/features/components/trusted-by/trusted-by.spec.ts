import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TrustedBy } from './trusted-by';

describe('TrustedBy', () => {
  let component: TrustedBy;
  let fixture: ComponentFixture<TrustedBy>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrustedBy]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TrustedBy);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
