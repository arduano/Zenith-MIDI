import { MainComponent } from './core/main/main.component';
import { NgModule } from '@angular/core';
import { Routes, RouterModule, ActivatedRouteSnapshot, DetachedRouteHandle, RouteReuseStrategy } from '@angular/router';
import { StartPageComponent } from './pages/start-page/start-page.component';
import { BenchmarksPageComponent } from './pages/benchmarks-page/benchmarks-page.component';
import { PluginsPageComponent } from './pages/plugins-page/plugins-page.component';


const routes: Routes = [
  { path: 'Zenith-MIDI', redirectTo: 'Zenith-MIDI/start', pathMatch: 'full' },
  { path: '', redirectTo: 'Zenith-MIDI/start', pathMatch: 'full' },
  {
    path: 'Zenith-MIDI', component: MainComponent, children: [
      { path: 'start', component: StartPageComponent },
      { path: 'benchmarks', component: BenchmarksPageComponent},
      { path: 'plugins', component: PluginsPageComponent},
    ]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
