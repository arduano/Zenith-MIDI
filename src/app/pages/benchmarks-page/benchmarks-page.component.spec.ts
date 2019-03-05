import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BenchmarksPageComponent } from './benchmarks-page.component';

describe('BenchmarksPageComponent', () => {
  let component: BenchmarksPageComponent;
  let fixture: ComponentFixture<BenchmarksPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BenchmarksPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BenchmarksPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
