import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CreditPageComponent } from './credit-page.component';

describe('CreditPageComponent', () => {
  let component: CreditPageComponent;
  let fixture: ComponentFixture<CreditPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CreditPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreditPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
