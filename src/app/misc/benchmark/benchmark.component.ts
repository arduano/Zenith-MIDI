import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-benchmark',
  templateUrl: './benchmark.component.html',
  styleUrls: ['./benchmark.component.less']
})
export class BenchmarkComponent implements OnInit {

  @Input() midiName: string;
  @Input() fileSize: string;
  @Input() noteCount: string;
  @Input() avgMem: string;
  @Input() maxMem: string;
  @Input() mintes: string;

  constructor() { }

  ngOnInit() {
  }

}
