import {Injectable} from '@angular/core';
//import {XHR} from 'angular2/XHR';
//import {} from 'angular2/core/compiler/xhr/xhr';
//import {Http, Response, Headers} from 'angular2/http';
import {HMData} from './HMData';

@Injectable()
export class HMDataService {

    constructor() {
    }

    //getDefaultHMData() {
    //    return Promise.resolve(gDefaultHMData);
    //}

    //getHMDataSlowly() {
    //    return new Promise(resolve =>
    //        setTimeout(() => resolve(gDefaultHMData), 5000) // 5 seconds
    //    );
    //}

    //http://muthukumaransankaranainar.blogspot.co.uk/2015/05/angular-2-how-to-make-ajax-call-with.html
    //1.Angluar JS has a separate data persistence layer, that will cover HTTP.This is not finished yet.
    //2.Because of Zone in Angular 2 you can use any existing mechanism for fetching data.This includes XMLHttpRequest fetch() and any other third party libraries.
    //3.XHR in the compiler is meant to be private, and should not be used.
    // so on 2015-12-15: don't use Angular2/http yet.
    getHttpHMData() {
        console.log("http Get start");
        return new Promise(resolve => {
            //$sqhttp.get('http://jsonplaceholder.typicode.com/posts?userId=1').then(function (json) {  // this allows CORS, but not mine
            $sqhttp.get('/WebServer/ReportHealthMonitorCurrentStateToDashboardInJSON').then(function (json) {
                console.log("sqhttp post returned: " + json);
                //resolve(gDefaultHMData);
                resolve(json);
            });
        }
        );
        //console.log("http Get end");
    }

    getHttpWithPostHMData(p_hmDataToSend) {
        console.log("http with Post start");
        return new Promise(resolve => {
            $sqhttp.post('/WebServer/ReportHealthMonitorCurrentStateToDashboardInJSON', p_hmDataToSend).then(function (json) {
                console.log("sqhttp post returned: " + JSON.stringify(json));
                //var obj = JSON.parse(json);
                resolve(json);
            });
        }
        );
        //console.log("http with Post end");
    }

    logError(err) {
        console.error('There was an error: ' + err);
    }
}


export const $sqhttp = {
    get: function (url: string) {
        return _sendRequest(url, null, 'GET');
    },
    post: function (url: string, payload: any) {
        return _sendRequest(url, payload, 'POST');  // post is more general than Put. POST sends data to a specific URI and expects the resource at that URI to handle the request.
    },
    put: function (url: string, payload: any) {
        return _sendRequest(url, payload, 'PUT');   // Put is like a file upload; replace the file at destination
    },
    delete: function (url: string, payload: any) {
        return _sendRequest(url, null, 'DELETE');
    }
}

function _sendRequest(url: string, payLoad: any, type: string) {
    return new Promise(function (resolve, reject) {
        var req = new XMLHttpRequest();
        req.open(type, url);
        // req.setRequestHeader("Content-Type", "application/json;charset=UTF-8");

        req.onload = function () {
            if (req.status == 200) {
                resolve(JSON.parse(req.response));
            } else {
                reject(JSON.parse(req.response));
            }
        };

        req.onerror = function () {
            reject(JSON.parse(req.response));
        };

        if (payLoad) {
            req.send(JSON.stringify(payLoad));
        } else {
            req.send(null);
        }
    });
}
