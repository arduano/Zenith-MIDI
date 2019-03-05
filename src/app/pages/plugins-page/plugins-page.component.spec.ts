import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PluginsPageComponent } from './plugins-page.component';

describe('PluginsPageComponent', () => {
  let component: PluginsPageComponent;
  let fixture: ComponentFixture<PluginsPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PluginsPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PluginsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
