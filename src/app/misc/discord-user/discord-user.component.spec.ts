import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DiscordUserComponent } from './discord-user.component';

describe('DiscordUserComponent', () => {
  let component: DiscordUserComponent;
  let fixture: ComponentFixture<DiscordUserComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DiscordUserComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DiscordUserComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
