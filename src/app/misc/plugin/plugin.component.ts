import { Component, OnInit, Input } from "@angular/core";

@Component({
  selector: "app-plugin",
  templateUrl: "./plugin.component.html",
  styleUrls: ["./plugin.component.less"]
})
export class PluginComponent implements OnInit {
  @Input() imgUrl = '';
  @Input() name = '';

  constructor() {}

  ngOnInit() {}
}
