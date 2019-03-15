import { Component, OnInit, Input, HostListener, ViewChild, ElementRef } from '@angular/core';
import { setInterval } from 'timers';
import { timer } from 'rxjs';
import { take } from 'rxjs/operators';
import { Scroll, Router, ActivatedRoute, OutletContext } from '@angular/router';
import { routerTransition } from '../../router.animations';
import { UIService } from '../../services/ui/ui.service';
declare var $ :any;

@Component({
  selector: 'app-main',
  animations: [routerTransition],
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.less']
})
export class MainComponent implements OnInit {

  public scroll = 0;
  public urlChild:string;
  public mobile:boolean = false;

  public navbarOut: boolean = false;
  public navbarCollapseUp: boolean = false;
  public navbarCollapseDown: boolean = false;

  private timer:any = null;

  public links:any[] = [
    {name:'Start', url:'start'},
    {name:'Benchmarks', url:'benchmarks'},
    {name:'Plugins', url:'plugins'},
    {name:'Credit', url:'credit'}
  ]

  toggleMenu(){
    this.navbarOut = !this.navbarOut;
    clearTimeout(this.timer)
    this.navbarCollapseDown = false;
    this.navbarCollapseUp = false;
    if(this.navbarOut){
      this.navbarCollapseDown = true;
      this.timer = setTimeout(() => { this.navbarCollapseDown = false; }, 350)
    }
    else{
      this.navbarCollapseUp = true;
      this.timer = setTimeout(() => { this.navbarCollapseUp = false; }, 350)
    }
  }

  @HostListener('window:scroll', ['$event'])
  checkScroll() {
    this.scroll = window.pageYOffset;
  }
  constructor(public router: Router, private route: ActivatedRoute, public ui: UIService) { }

  @HostListener('window:resize', ['$event'])
  onResize(event) {
    this.mobile = event.target.innerWidth < 768;
  }

  ngOnInit() {
    this.mobile = window.innerWidth < 768;
  }

  routeActivated(){
    this.route.firstChild.url.subscribe(url => {
      this.urlChild = url[0].path
    })
  }

  transition(o){
    return o.isActivated ? o.activatedRoute : '';
  }

  routeTo(url) {
    this.router.navigate(['Zenith-MIDI/' + url])
  }
}
