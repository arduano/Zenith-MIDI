import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-discord-user',
  templateUrl: './discord-user.component.html',
  styleUrls: ['./discord-user.component.less']
})
export class DiscordUserComponent implements OnInit {
  @Input() pfpURL = "";
  @Input() username = "";
  @Input() detail = "";
  @Input() link = "";

  constructor() { }

  ngOnInit() {
  }

}
