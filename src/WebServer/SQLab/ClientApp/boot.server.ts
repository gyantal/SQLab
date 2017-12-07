import 'reflect-metadata';
import 'zone.js';
import 'rxjs/add/operator/first';
import { APP_BASE_HREF } from '@angular/common';
import { enableProdMode, ApplicationRef, NgZone, ValueProvider } from '@angular/core';
import { platformDynamicServer, PlatformState, INITIAL_CONFIG } from '@angular/platform-server';
import { createServerRenderer, RenderResult } from 'aspnet-prerendering';
import { AppModule } from './app/app.server.module';

enableProdMode();

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
