import { Component } from '@angular/core';

declare var window: any;

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent {
    public g_sqWebAppName = '';

    constructor() {
        if (typeof window == "undefined")   //  window is not defined in server rendering
            this.g_sqWebAppName = 'window is not defined.';
        else if (window == null)
            this.g_sqWebAppName = 'window is null.'
        else if ('sqWebAppName' in window)
            this.g_sqWebAppName = window.sqWebAppName;
        else
            this.g_sqWebAppName = 'window is defined, but no window.sqWebAppName.';
            
    }
}
