import { timer } from 'rxjs';
import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';

@Component({
  selector: 'app-social-media',
  templateUrl: './social-media.component.html',
  styleUrls: ['./social-media.component.less']
})
export class SocialMediaComponent implements OnInit {
  @ViewChild('twitter') _twitter: ElementRef;
  open: boolean = false;
  public interval = null;
  time = 0;

  constructor() { }

  ngOnInit() {
  }

  toggleOpen() {
    this.open = !this.open;
    window.clearInterval(this.interval);
    if (this.time > 0 && this.time < 4) { this.time = 4; }
    if (this.time > 4 && this.time > 8) { this.time = 0; }
    this.interval = window.setInterval(() => {
      this.time += 1;
      
      if (this.time == 4 || this.time > 8) {
        window.clearInterval(this.interval);
        if (this.time > 8) window.setTimeout(() => this.time = 0, 100) 
        //console.log(this.time);
      }
    }, 20)
  }

}
