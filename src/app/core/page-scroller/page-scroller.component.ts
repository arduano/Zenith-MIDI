import { Component, OnInit, Input, ElementRef, ViewChild, HostListener } from '@angular/core';
import { UIService } from '../../services/ui/ui.service';
import { setTimeout, setInterval } from 'timers';
import { max, min } from 'rxjs/operators';
declare var ResizeSensor:any;

@Component({
  selector: 'app-page-scroller',
  templateUrl: './page-scroller.component.html',
  styleUrls: ['./page-scroller.component.less']
})
export class PageScrollerComponent implements OnInit {
  @Input() fullPage = true;
  @ViewChild('scrollContainer') scrollContainer: ElementRef;
  @ViewChild('scrollBar') scrollBar: ElementRef;
  public updateLoop:any;
  
  scrollHidden:boolean = true;

  public headSize = 0;
  public topSize = 0;
  public bottomSize = 1;

  public headHeight = 0.3;

  constructor(private ui: UIService) { }

  public external;
  public internal;
  public scrollTop = 0;

  public dragging = false;

  public dragStartPos;
  public dragHeadStartPos;

  ngOnInit() {
    window.setTimeout(() => {
      this.updateScroll();
      this.updateLoop = window.setInterval(() => this.updateScroll(), 300);
    }, 1000)  
    window.onmouseup = () => {this.dragging = false}
    window.onmousemove = (a) => {this.headDrag(a)}
    if(this.fullPage){
      this.ui.scrollDistance = this.scrollTop;
    }
  }
  
  updateScroll(){
    this.external = this.scrollContainer.nativeElement.clientHeight;
    let s = window.getComputedStyle(this.scrollContainer.nativeElement.children[0]);
    let margin = parseFloat(s.marginTop) + parseFloat(s.marginBottom);
    this.internal = this.scrollContainer.nativeElement.children[0].clientHeight + margin;
    this.headHeight = Math.max(this.external / this.internal, 0.1);
    this.scrollHidden =  this.internal <= this.external;
    let scroll = this.scrollTop / (this.internal - this.external);
    let top = scroll;
    let bottom = 1 - scroll;
    this.topSize = top * (1 - this.headHeight) * 100;
    this.topSize = this.topSize < 0 ? 0 : this.topSize;
    this.bottomSize = bottom * (1 - this.headHeight) * 100;
    this.bottomSize = this.bottomSize < 0 ? 0 : this.bottomSize;
    this.headSize = this.headHeight * 100;

    //console.log(this.external - this.internal);
  }
  
  onScroll($event) {
    this.scrollTop = $event.srcElement.scrollTop;
    if(this.fullPage){
      this.ui.scrollDistance = this.scrollTop;
    }
    this.updateScroll()
  }

  dragStart($event){
    console.log($event);
    this.dragStartPos = $event.clientY;
    this.dragHeadStartPos = this.scrollTop / (this.internal - this.external);
    this.dragging = true;
  }

  headDrag($event){
    //console.log($event);
    if(this.dragging){
      let drag = (this.dragStartPos - $event.clientY) / (this.scrollBar.nativeElement.clientHeight * (1 - this.headHeight));

    console.log();
      
      this.scrollContainer.nativeElement.scrollTop = (this.dragHeadStartPos - drag) * (this.internal - this.external);
    }
  }

  ngAfterViewInit() {
    window.setTimeout(() => ResizeSensor(this.scrollContainer.nativeElement.children[0], () => {this.updateScroll();}), 1000)
  }

  ngOnDestroy(){
    window.clearInterval(this.updateLoop);
  }
}
