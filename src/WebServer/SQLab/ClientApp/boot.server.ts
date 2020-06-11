import 'reflect-metadata';
import 'zone.js';
import 'rxjs/add/operator/first';
import { APP_BASE_HREF } from '@angular/common';
import { enableProdMode, ApplicationRef, NgZone, ValueProvider } from '@angular/core';
import { platformDynamicServer, PlatformState, INITIAL_CONFIG } from '@angular/platform-server';
import { createServerRenderer, RenderResult } from 'aspnet-prerendering';
import { AppModule } from './app/app.server.module';

enableProdMode();

// Freeze possibility: (QuickTester, HealthMonitor) Angular Developer mode problem in production environment: ngServe can be called every 5 seconds (in DEV mode) to be refreshed
// in log files: 00:46:44.9#HTTP GET '/dist/__webpack_hmr' from 52.211.231.5 (u: ) ret: 200 in 888.63ms
// Angular's DEV mode ngServe has memory leaks and in 2-3 days, it consumes all RAM on a small server
// the 'nodejs' process is stalling in 'D' Disk -sleeping mode, and on the top of it, the 'dotnet' process of Kestrell busy-waits on it, doing 99% CPU utilization.
// the whole server freezes and has to be rebooted
// solution: don't leave the QuickTester on a Chrome tab-page. Every time it is used, close the tab-page down. (it is fixed in SqCore, because we don't use Dev mode there, only static HTML files) 

declare const setImmediate: Function;

export default createServerRenderer(params => {
    const providers = [
        { provide: INITIAL_CONFIG, useValue: { document: '<app></app>', url: params.url } },
        { provide: APP_BASE_HREF, useValue: params.baseUrl },
        { provide: 'BASE_URL', useValue: params.origin + params.baseUrl },
    ];

    return platformDynamicServer(providers).bootstrapModule(AppModule).then(moduleRef => {
        const appRef: ApplicationRef = moduleRef.injector.get(ApplicationRef);
        const state = moduleRef.injector.get(PlatformState);
        const zone = moduleRef.injector.get(NgZone);


        ////if ((params.data != null) && ('sqWebAppName' in params.data) && (params.data.sqWebAppName != null) && (params.data.sqWebAppName == "HealthMonitorWebApp"))
        if (params.location.path.toLowerCase() == '/healthmonitor')
        {
            var result = '<h1>Hello world!</h1>'
                + '<p>Current time in Node is: ' + new Date() + '</p>'
                + '<p>Request path is: ' + params.location.path + '</p>'    // "/HealthMonitor"
                + '<p>Absolute URL is: ' + params.absoluteUrl + '</p>';     // "http://localhost/HealthMonitor"

            //const result = `<h1>Hello, WebApp ${params.data.sqWebAppName}</h1>`;

            return new Promise<RenderResult>((resolve, reject) => {
                zone.onError.subscribe((errorInfo: any) => reject(errorInfo));
                appRef.isStable.first(isStable => isStable).subscribe(() => {
                    // Because 'onStable' fires before 'onError', we have to delay slightly before
                    // completing the request in case there's an error to report
                    setImmediate(() => {
                        resolve({
                            //html: result,
                            html: state.renderToString(),
                            globals: {  // these go to global variables in the client browser: window.*
                                sqWebAppName: params.data.sqWebAppName,
                                sqRequestPath: params.location.path
                            }
                        });
                        moduleRef.destroy();
                    });
                });
            });
        }

        return new Promise<RenderResult>((resolve, reject) => {
            zone.onError.subscribe((errorInfo: any) => reject(errorInfo));
            appRef.isStable.first(isStable => isStable).subscribe(() => {
                // Because 'onStable' fires before 'onError', we have to delay slightly before
                // completing the request in case there's an error to report
                setImmediate(() => {
                    resolve({
                        html: state.renderToString()
                    });
                    moduleRef.destroy();
                });
            });
        });
    });
});
