import { UIService } from './services/ui/ui.service';
import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { StartPageComponent } from './pages/start-page/start-page.component';
import { MainComponent } from './core/main/main.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { PageScrollerComponent } from './core/page-scroller/page-scroller.component';
import { SocialMediaComponent } from './misc/social-media/social-media.component';
import { MatButtonModule, MatCheckboxModule, MatCardModule, MatFormFieldModule, MatInputModule, MatNativeDateModule, MatRadioModule, MatProgressBarModule,} from '@angular/material';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from 'selenium-webdriver/http';
import { HttpClientModule } from '@angular/common/http';
import { BenchmarksPageComponent } from './pages/benchmarks-page/benchmarks-page.component';
import { BenchmarkComponent } from './misc/benchmark/benchmark.component';
import { PluginsPageComponent } from './pages/plugins-page/plugins-page.component';
import { PluginComponent } from './misc/plugin/plugin.component';


@NgModule({
  declarations: [
    AppComponent,
    StartPageComponent,
    MainComponent,
    PageScrollerComponent,
    SocialMediaComponent,
    BenchmarksPageComponent,
    BenchmarkComponent,
    PluginsPageComponent,
    PluginComponent,
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,

    MatButtonModule, MatCheckboxModule, MatCardModule,
    MatFormFieldModule, MatInputModule, MatRadioModule,
    MatProgressBarModule,

    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    MatNativeDateModule,
    HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
