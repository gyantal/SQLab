import {bootstrap}    from '@angular/platform-browser-dynamic';
import {HTTP_PROVIDERS} from '@angular/http';
import {AppComponent} from './app.component';
//import {AngularInit_TotM} from './TotM';
//import {AngularInit_LEtfDistcrepancy} from './L-ETF-Discrepancy'
//import {AngularInit_VXX} from './VXX_SPY_Controversial'

bootstrap(AppComponent, [HTTP_PROVIDERS]);      // needed for Http injection
