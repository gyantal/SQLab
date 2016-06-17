import {Component, OnInit} from '@angular/core';
import {Http, Response, Headers} from '@angular/http';
import {Observable} from 'rxjs/Observable';
import {HMData} from './HMData';
import {HMDataService} from './HMData.service';

declare var gSqUserEmail: string;
// good demos here: http://www.syntaxsuccess.com/angular-2-samples/#/demo/http

//ZONES
//In Angular 1 you have to tell the framework that it needs to run this check by doing scope.$apply.
//You don’t need to worry about it in Angular 2. Angular 2 uses Zone.js to know when this check is required., 
//This means that you do not need to call scope.$apply to integrate with third- party libraries.

// the data binding, namely view update after data changed works in Chrome and Edge, but not in IE. Grr. Forget IE anyway. And it is Angular2 Beta only.
@Component({
    selector: 'health-monitor-app',
    templateUrl: '/app/HealthMonitor/app.component.html',
    //template: '<h1>HealthMonitor Angular2 App</h1><br/><button (click)="onClickMe()">Click me!</button><br/><br/>{{clickMessage }}'
    providers: [HMDataService],
    viewProviders: [HMDataService],
    //viewBindings: [HMDataService],
    inputs: ['m_data', 'm_title'],
   // for CSS in Angular see: http://blog.thoughtram.io/angular/2015/06/29/shadow-dom-strategies-in-angular2.html
   // styleUrls: ['/app/app.component.css'],  // it should work, but it doesn't work in Angular2 beta, so I put the <link> into Dashboard.html
})


export class AppComponent implements OnInit {
    public m_title: string = 'SQ HealthMonitor Dashboard';    // strongly typed variables in TS
    public m_data: HMData;
    public m_userEmail: string = 'Unknown user';

    // debug info here
    m_webAppResponse: string;
    m_wasRefreshClicked: any;

    //constructor(http: Http) {        //Exception was thrown at line 4, column 10194 in https://code.angularjs.org/tools/system.js //        0x80000000 - JavaScript runtime error: undefined
    constructor(private m_HMDataService: HMDataService) {   // private variables become members; sythetic sugar for this.m_a = p_a;
        //this.m_http = http;
        this.m_wasRefreshClicked = "Refresh was not yet clicked.";
    }

    ngOnInit() {
        this.getHMData(gDefaultHMData);
        this.m_userEmail = gSqUserEmail;
    }

    getHMData(p_hmDataToSend) {
        //this.m_HMDataService.getHttpHMData().then(hmDataReturned => {

        this.m_HMDataService.getHttpWithPostHMData(p_hmDataToSend).then(hmDataReturned => {
            // don't worry if view is not updated in IE. Angula2 bug in IE. In Chrome and Edge it works.
            //console.log("getHttpWithPostHMData() returned");
            var hmData: HMData = <HMData>hmDataReturned; // Typescript cast: remember that this is a compile-time cast, and not a runtime cast.
            this.m_data = hmData;

            // Sadly Javascript loves Local time, so work in Local time; easier;
            // 1. StartDate
            this.m_data.StartDateLoc = new Date(hmData.StartDate); // "2015-12-29 00:49:54.000Z", because of the Z Zero, this UTC string is converted properly to local time
            this.m_data.StartDate = localDateToUtcString_yyyy_mm_dd_hh_mm_ss(this.m_data.StartDateLoc);    // take away the miliseconds from the dateStr
            var localNow = new Date();  // this is local time: <checked>
            //var utcNowGetTime = new Date().getTime();  //getTime() returns the number of seconds in UTC.
            this.m_data.StartDateTimeSpanStr = getTimeSpanStr(this.m_data.StartDateLoc, localNow);

            //this.m_data.ResponseToFrontEnd = "ERROR";

            this.m_data.AppOk = 'OK';
            if (this.m_data.ResponseToFrontEnd.toUpperCase().indexOf('ERROR') >= 0)
                this.m_data.AppOk = 'ERROR';

            this.m_data.RtpsOk = 'OK';
            for (var i in this.m_data.RtpsDownloads) {
                if (this.m_data.RtpsDownloads[i].indexOf('OK') >= 0) {  // if 'OK' is found
                    continue;
                }
                this.m_data.RtpsOk = 'ERROR';
            }

            this.m_data.VBrokerOk = 'OK';
            for (var i in this.m_data.VBrokerReports) {
                if (this.m_data.VBrokerReports[i].indexOf('OK') >= 0) {  // if 'OK' is found
                    continue;
                }
                this.m_data.VBrokerOk = 'ERROR';
            }

            this.m_webAppResponse = JSON.stringify(hmData);
        });
    }

    setControlValue(controlName, value) {
        console.log("setControlValue():" + controlName + "/" + value);
        console.log("setControlValue():" + controlName + "/" + value + "/" + this.m_data.DailyEmailReportEnabled);
        if (controlName == 'chkDailyEmail') {
            if (this.m_data.DailyEmailReportEnabled != value) {
                this.m_data.DailyEmailReportEnabled = value;
                this.m_data.CommandToBackEnd = "ApplyTheDifferences";
                this.getHMData(this.m_data);
            }
        } else if (controlName == 'chkRtps') {
            if (this.m_data.RtpsTimerEnabled != value) {
                this.m_data.RtpsTimerEnabled = value;
                this.m_data.CommandToBackEnd = "ApplyTheDifferences";
                this.getHMData(this.m_data);
            }
        } else if (controlName == 'chkVBroker') {
            if (this.m_data.ProcessingVBrokerMessagesEnabled != value) {
                this.m_data.ProcessingVBrokerMessagesEnabled = value;
                this.m_data.CommandToBackEnd = "ApplyTheDifferences";
                this.getHMData(this.m_data);
            }
        }




        //this.todo.controls[controlName].updateValue(value);
        //this.todo.controls[controlName].markAsDirty();
    }

    refreshViewClicked() {
        console.log("refreshViewClicked");
        this.m_wasRefreshClicked = "refreshViewClicked";
    }

    refreshDataClicked() {
        console.log("refreshDataClicked");
        this.m_wasRefreshClicked = "refreshDataClicked";
        this.m_data.CommandToBackEnd = "OnlyGetData";
        this.getHMData(this.m_data);
    }
}









// ************** Utils section

function localDateToUtcString_yyyy_mm_dd_hh_mm_ss(p_date: Date) {
    var year = "" + p_date.getUTCFullYear();
    var month = "" + (p_date.getUTCMonth() + 1); if (month.length == 1) { month = "0" + month; }
    var day = "" + p_date.getUTCDate(); if (day.length == 1) { day = "0" + day; }
    var hour = "" + p_date.getUTCHours(); if (hour.length == 1) { hour = "0" + hour; }
    var minute = "" + p_date.getUTCMinutes(); if (minute.length == 1) { minute = "0" + minute; }
    var second = "" + p_date.getUTCSeconds(); if (second.length == 1) { second = "0" + second; }
    return year + "-" + month + "-" + day + " " + hour + ":" + minute + ":" + second;
}

// Started on 2015-12-23 00:44 (0days 0h 12m ago)
function getTimeSpanStr(date1: Date, date2: Date) {
    var diff = date2.getTime() - date1.getTime();

    var days = Math.floor(diff / (1000 * 60 * 60 * 24));
    diff -= days * (1000 * 60 * 60 * 24);

    var hours = Math.floor(diff / (1000 * 60 * 60));
    diff -= hours * (1000 * 60 * 60);

    var mins = Math.floor(diff / (1000 * 60));
    diff -= mins * (1000 * 60);

    var seconds = Math.floor(diff / (1000));
    diff -= seconds * (1000);

    return "(" + days + "days " + hours + "h " + mins + "m " + seconds + "s ago)";
}




var gDefaultHMData: HMData = {
    AppOk: "OK",
    StartDate: '1998-11-16T00:00:00',
    StartDateLoc: new Date('1998-11-16T00:00:00'),
    StartDateTimeSpanStr: '',
    DailyEmailReportEnabled: false,

    RtpsOk: 'OK',
    RtpsTimerEnabled: false,
    RtpsTimerFrequencyMinutes: -999,
    RtpsDownloads: ['a', 'b'],

    VBrokerOk: 'OK',
    ProcessingVBrokerMessagesEnabled: false,
    VBrokerReports: ['a', 'b'],
    VBrokerDetailedReports: ['a', 'b'],

    CommandToBackEnd: "OnlyGetData",
    ResponseToFrontEnd: "OK"
};


