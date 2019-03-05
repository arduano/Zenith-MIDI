import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { HttpClient } from "@angular/common/http";

@Component({
  selector: "app-start-page",
  templateUrl: "./start-page.component.html",
  styleUrls: ["../page-styles.less", "./start-page.component.less"]
})
export class StartPageComponent implements OnInit {
  constructor(public router: Router, private http: HttpClient) {}

  url86: string = '';
  url64: string = '';

  gitURL: string =
    'https://api.github.com/repos/arduano/Zenith-MIDI/releases/latest';
  async ngOnInit() {
    let data: any = this.http.get(this.gitURL);
    data = await data.toPromise();
    console.log(data.assets);
    for (let i = 0; i < data.assets.length; i++) {
      let asset = data.assets[i];
      let name: string = asset.name;
      if (name.includes('86')) {
        this.url86 = asset.browser_download_url;
        console.log(this.url86);
      }
      if (name.includes('64')) {
        this.url64 = asset.browser_download_url;
        console.log(this.url64);
      }
    }
  }
}
