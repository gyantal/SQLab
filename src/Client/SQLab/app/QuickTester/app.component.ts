import {Component, OnInit, AfterViewInit} from '@angular/core';
//import {Http, Response, Headers} from '@angular/http';
import {Http,HTTP_PROVIDERS} from '@angular/http';
import {Observable} from 'rxjs/Observable';
import {TotM} from './TotM'
import {LEtfDistcrepancy, AngularInit_LEtfDistcrepancy} from './L-ETF-Discrepancy'
import {VXX_SPY_Controversial} from './VXX_SPY_Controversial'
import {StopWatch} from './Utils'

declare var gSqUserEmail: string;
//declare var $: JQueryStatic;    // Declaring $ (or jQuery) as JQueryStatic will give you a typed reference to jQuery.
// good demos here: http://www.syntaxsuccess.com/angular-2-samples/#/demo/http

//ZONES
//In Angular 1 you have to tell the framework that it needs to run this check by doing scope.$apply.
//You don’t need to worry about it in Angular 2. Angular 2 uses Zone.js to know when this check is required., 
//This means that you do not need to call scope.$apply to integrate with third- party libraries.

// the data binding, namely view update after data changed works in Chrome and Edge, but not in IE. Grr. Forget IE anyway. And it is Angular2 Beta only.
@Component({
    selector: 'quick-tester-app',
    templateUrl: '/app/QuickTester/app.component.html',
    //template: '<h1>HealthMonitor Angular2 App</h1><br/><button (click)="onClickMe()">Click me!</button><br/><br/>{{clickMessage }}'
    //providers: [HMDataService],
    //viewProviders: [HMDataService],
    //viewBindings: [HMDataService],
    inputs: ['m_data', 'm_title'],
   // for CSS in Angular see: http://blog.thoughtram.io/angular/2015/06/29/shadow-dom-strategies-in-angular2.html
   // styleUrls: ['/app/app.component.css'],  // it should work, but it doesn't work in Angular2 beta, so I put the <link> into Dashboard.html
})


export class AppComponent implements OnInit, AfterViewInit {
    public m_userEmail: string = 'Unknown user';

    public m_versionShortInfo: string = "v0.2.30";    // strongly typed variables in TS
    public versionLongInfo: string = "SQ QuickTester  \nVersion 0.2.30  \nDeployed: 2015-07-10T21:00Z";  // Z means Zero UTC offset, so, it is the UTC time, http://en.wikipedia.org/wiki/ISO_8601
    public tipToUser: string = "Select Strategy and press 'Start Backtest'...";
    public tradingViewChartWidget = null;

    public tradingViewChartName: string = "DayOfTheWeek data";

    public inputStartDateStr: string = "";  // empty string means maximum available
    public inputEndDateStr: string = "";    // empty string means: today

    // Inputs area
    //public selectedStrategy = "LETFDiscrepancy1";
    public selectedStrategyMenuItemId = null;
    public selectedStrategyName = null;
    public strategyGoogleDocHelpUri = null;
    public selectedStrategyWebApiName = null;

    public profilingBacktestStopWatch = null;
    public profilingBacktestCallbackMSec = null;
    public profilingBacktestAtChartReadyStartMSec = null;
    public profilingBacktestAtChartReadyEndMSec = null;

    // Output Statistics area
    public startDateStr: string = "";
    public endDateStr: string = "";
    public rebalanceFrequencyStr: string = "";
    public benchmarkStr: string = "";

    public pvStartValue: number = 1;
    public pvEndValue: number = 1;
    public totalGainPct: number = 1;
    public cagr: number = 1;
    public annualizedStDev: number = 1;
    public sharpeRatio: number = 1;
    public sortinoRatio: number = 1;
    public maxDD: number = 1;
    public ulcerInd: number = 1;// = qMean DD
    public maxTradingDaysInDD: number = 1;
    public winnersStr = 1;
    public losersStr = 1;

    public benchmarkCagr: number = 1;
    public benchmarkMaxDD: number = 1;
    public benchmarkCorrelation: number = 0;

    public pvCash: number = 1;
    public nPositions: number = 0;
    public holdingsListStr: string = "";
    public htmlNoteFromStrategy: string = "";

    

    public chartDataFromServer = null;  // original, how it arrived from server
    public chartDataInStr = null;   // for showing it in HTML for debug porposes
    public chartDataToChart = null; // processed: it has time: close, open values, so we have to process it only once
    public nMonthsInTimeFrame: string = "24";

    public generalInputParameters: string = "";
    public debugMessage: string = "";
    public errorMessage: string = "";

    public strategy_LEtfDistcrepancy: LEtfDistcrepancy;
    public strategy_VXX_SPY_Controversial: VXX_SPY_Controversial;
    public strategy_TotM: TotM;

    constructor(private http: Http) { }

    ngOnInit() {
        console.log("ngOnInit() START");
        //this.getHMData(gDefaultHMData);
        this.m_userEmail = gSqUserEmail;

        this.strategy_LEtfDistcrepancy = new LEtfDistcrepancy(this);
        this.strategy_VXX_SPY_Controversial = new VXX_SPY_Controversial(this);
        this.strategy_TotM = new TotM(this);

        this.SelectStrategy("idMenuItemTotM");

        //AngularInit_TotM(this);
        //AngularInit_VXX(this);
        //AngularInit_LEtfDistcrepancy(this);
    }

    ngAfterViewInit() {     // equivalent to $(document).ready()
         //Ideally you should wait till component content get initialized, in order to make the DOM available on which you wanted to apply jQuery. For that you need to use AfterViewInit which is one of hook of angular2 lifecycle.
        //here you will have code where component content is ready.
        console.log("ngAfterViewInit() START");
        //$('.mymyButton1Class').hide();

        /* Next part of code handles hovering effect and submenu appearing */
        $('.sqMenuItemLevel0').hover(
            function () { //appearing on hover
                $('.sqMenuBarLevel1', this).fadeIn();
            },
            function () { //disappearing on hover
                $('.sqMenuBarLevel1', this).fadeOut();
            }
        );

        $('.sqMenuItemLevel1').hover(
            function () { //appearing on hover
                $('.sqMenuBarLevel2', this).fadeIn();
            },
            function () { //disappearing on hover
                $('.sqMenuBarLevel2', this).fadeOut();
            }
        );
        console.log("ngAfterViewInit() START 2");
    } 

    SelectStrategy(menuItemId: string) {
        this.selectedStrategyMenuItemId = menuItemId;

        this.strategy_TotM.SubStrategySelected_TotM();
        this.strategy_VXX_SPY_Controversial.SubStrategySelected_VXX();
        this.strategy_LEtfDistcrepancy.SubStrategySelected_LEtfDistcrepancy();
    }



    TradingViewOnready() {
        //https://github.com/tradingview/charting_library/wiki/Widget-Constructor
        //var widget = new TradingView.widget({
        //    //fullscreen: true,
        //    symbol: 'PV',
        //    //symbol: 'AA',
        //    interval: 'D',
        //    container_id: "tv_chart_container",
        //    //	BEWARE: no trailing slash is expected in feed URL
        //    datafeed: new Datafeeds.UDFCompatibleDatafeed($scope, "http://demo_feed.tradingview.com"),
        //    library_path: "../charting_library/",
        //    locale: "en",
        //    //	Regression Trend-related functionality is not implemented yet, so it's hidden for a while
        //    drawings_access: { type: 'black', tools: [{ name: "Regression Trend" }] },

        //    charts_storage_url: 'http://saveload.tradingview.com',
        //    client_id: 'tradingview.com',
        //    user_id: 'public_user_id'


        //    , width: "90%"        //Remark: if you want the chart to occupy all the available space, do not use '100%' in those field. Use fullscreen parameter instead (see below). It's because of issues with DOM nodes resizing in different browsers.
        //    , height: 400
        //    //https://github.com/tradingview/charting_library/wiki/Featuresets
        //    //,enabled_features: ["trading_options"]    
        //    //, enabled_features: ["charting_library_debug_mode", "narrow_chart_enabled", "move_logo_to_main_pane"] //narrow_chart_enabled and move_logo_to_main_pane doesn't do anything to me
        //    , enabled_features: ["charting_library_debug_mode"]
        //    //, disabled_features: ["use_localstorage_for_settings", "volume_force_overlay", "left_toolbar", "control_bar", "timeframes_toolbar", "border_around_the_chart", "header_widget"]
        //    , disabled_features: ["border_around_the_chart"]
        //    , debug: true   // Setting this property to true makes the chart to write detailed API logs to console. Feature charting_library_debug_mode is a synonym for this field usage.
        //    , time_frames: [
        //        //{ text: "All", resolution: "6M" }, crash: first character should be a Number
        //        //{ text: "600m", resolution: "D" },   // "600m" 50 years : Put an insanely high value here. But later in the calculateHistoryDepth() we will decrease it to backtested range
        //        //{ text: "601m", resolution: "D" },   // "601m" 50 years : Put an insanely high value here. But later in the calculateHistoryDepth() we will decrease it to backtested range
        //        { text: $scope.nMonthsInTimeFrame + "m", resolution: "D" },   // this can be equivalent to ALL. Just calculate before how many years, or month. DO WORK with months.
        //        { text: $scope.nMonthsInTimeFrame + "m", resolution: "W" },   // this can be equivalent to ALL. Just calculate before how many years, or month. DO WORK with months.
        //        { text: $scope.nMonthsInTimeFrame + "m", resolution: "M" },   // this can be equivalent to ALL. Just calculate before how many years, or month. DO WORK with months.
        //        //{ text: "12y", resolution: "D" },   // this can be equivalent to ALL. Just calculate before how many years, or month.
        //        //{ text: "6000d", resolution: "D" },   // this can be equivalent to ALL. Just calculate before how many years, or month. DO NOT WORK. Max days: 350

        //        //{ text: "50y", resolution: "6M" },
        //        //{ text: "3y", resolution: "W" },
        //        //{ text: "8m", resolution: "D" },
        //        //{ text: "2m", resolution: "D" }
        //    ]

        //    , overrides: {
        //        "mainSeriesProperties.style": 3,    // area style
        //        "symbolWatermarkProperties.color": "#644",
        //        "moving average exponential.length": 13     // but doesn't work. It will be changed later anyway.
        //    },


        //});

        //widget.onChartReady(function () {   // this click() takes about 680msec, because the click will ask for the whole data, and redraw itself. So, it is understandable: sort of, but slow. Why it takes almost a second for TradingView to do this.
        //    if ($scope.profilingBacktestStopWatch != null) {
        //        $scope.$apply(function () {
        //            $scope.profilingBacktestAtChartReadyStartMSec = $scope.profilingBacktestStopWatch.GetTimestampInMsec();
        //        });
        //    }

        //    $scope.tradingViewChartWidget = widget;
        //    widget.createStudy('Moving Average Exponential', false, false, [26]);       //inputs: (since version 1.2) an array of study inputs.

        //    //////if (gBacktestIsReady) {
        //    //$scope.tradingViewChartWidget.postMessage.post($scope.tradingViewChartWidget._messageTarget(), "loadRangeAgy", {
        //    //    res: "D",
        //    //    val: $scope.nMonthsInTimeFrame + "m"  // the updated range after backtest is ready
        //    //})

        //    // this is better than the gTradingViewChartWidget.postMessage.post(gTradingViewChartWidget._messageTarget(), "loadRangeAgy", because the 'loading data' bug doesn't effect it, and because I can use the minified TV library
        //    // however, Chart Cache-s the getBars() data for every Time-Frame button, so it will not ask later for the new data. So, Removing(), Creating() chart is still necessary
        //    var z1 = document.getElementById("tv_chart_container");
        //    //var dateRangeDiv = z1.children[0].contentDocument.childNodes['1'].children['1'].children['library-container'].children['2'].children['chart-area'].children['0'].children['1'].children['1'];
        //    var dateRangeDiv = z1.children[0].contentDocument.childNodes['1'].children['1'].children['library-container'].children['2'].children['chart-area'].children['0'].children['0'].children['1'];
        //    dateRangeDiv.children['0'].click();
        //    dateRangeDiv.children['0'].innerHTML = "All";   // it takes effect, but if I click it Afterwards, than it will change back to original; so modify the Text After the Click

        //    if ($scope.profilingBacktestStopWatch != null) {
        //        $scope.$apply(function () {
        //            $scope.profilingBacktestAtChartReadyEndMSec = $scope.profilingBacktestStopWatch.GetTimestampInMsec();
        //        });
        //    }


        //    //////}
        //});
    }



    MenuItemStartBacktestClicked() {
        console.log("MenuItemStartBacktestClicked() START");

        this.generalInputParameters = "&StartDate=" + this.inputStartDateStr + "&EndDate=" + this.inputEndDateStr;

        this.strategy_TotM.StartBacktest_TotM(this.http);
        this.strategy_VXX_SPY_Controversial.StartBacktest_VXX(this.http);
        this.strategy_LEtfDistcrepancy.StartBacktest_LEtfDistcrepancy(this.http);

        this.profilingBacktestStopWatch = new StopWatch();
        this.profilingBacktestStopWatch.Start();
    }


    MenuItemVersionInfoClicked() {
        alert(this.versionLongInfo);
    }
   

    
    ProcessStrategyResult(strategyResult: any) {
        console.log("ProcessStrategyResult() START");

        this.profilingBacktestCallbackMSec = this.profilingBacktestStopWatch.GetTimestampInMsec();

        if (strategyResult.errorMessage != "") {
            alert(strategyResult.errorMessage);
            return; // in this case, don't do anything; there is no real Data.
        }

        this.startDateStr = strategyResult.startDateStr;
        this.rebalanceFrequencyStr = strategyResult.rebalanceFrequencyStr;
        this.benchmarkStr = strategyResult.benchmarkStr;

        this.endDateStr = strategyResult.endDateStr;
        this.pvStartValue = strategyResult.pvStartValue;
        this.pvEndValue = strategyResult.pvEndValue;
        this.totalGainPct = strategyResult.totalGainPct;
        this.cagr = strategyResult.cagr;
        this.annualizedStDev = strategyResult.annualizedStDev;
        this.sharpeRatio = strategyResult.sharpeRatio;
        this.sortinoRatio = strategyResult.sortinoRatio;
        this.maxDD = strategyResult.maxDD;
        this.ulcerInd = strategyResult.ulcerInd;
        this.maxTradingDaysInDD = strategyResult.maxTradingDaysInDD;
        this.winnersStr = strategyResult.winnersStr;
        this.losersStr = strategyResult.losersStr;

        this.benchmarkCagr = strategyResult.benchmarkCagr;
        this.benchmarkMaxDD = strategyResult.benchmarkMaxDD;
        this.benchmarkCorrelation = strategyResult.benchmarkCorrelation;

        this.pvCash = strategyResult.pvCash;
        this.nPositions = strategyResult.nPositions;
        this.holdingsListStr = strategyResult.holdingsListStr;

        this.htmlNoteFromStrategy = strategyResult.htmlNoteFromStrategy;
        document.getElementById("idHtmlNoteFromStrategy").innerHTML = strategyResult.htmlNoteFromStrategy;

        this.debugMessage = strategyResult.debugMessage;
        this.errorMessage = strategyResult.errorMessage;

        this.chartDataFromServer = strategyResult.chartData;

        this.chartDataToChart = [];
        var prevDayClose = null;
        for (var i = 0; i < strategyResult.chartData.length; i++) {
            var rowParts = strategyResult.chartData[i].split(",");
            var dateParts = rowParts[0].split("-");
            var dateUtc = new Date(Date.UTC(parseInt(dateParts[0]), parseInt(dateParts[1]) - 1, parseInt(dateParts[2]), 0, 0, 0));

            var closePrice = parseFloat(rowParts[1]);
            var barValue = {
                time: dateUtc.getTime(),  // gives back the miliseconds, so it is OK.  //time: data.t[i] * 1000,
                close: closePrice,
                open: (i == 0) ? closePrice : prevDayClose,
                high: (i == 0) ? closePrice : ((barValue.open > barValue.close) ? barValue.open : barValue.close),
                low: (i == 0) ? closePrice : ((barValue.open < barValue.close) ? barValue.open : barValue.close)
            }

            prevDayClose = barValue.close;
            this.chartDataToChart.push(barValue);
        }

        // calculate number of months in the range
        var startDateUtc = new Date(this.chartDataToChart[0].time);
        var endDateUtc = new Date(this.chartDataToChart[this.chartDataToChart.length - 1].time);
        var nMonths = (endDateUtc.getFullYear() - startDateUtc.getFullYear()) * 12;
        nMonths -= startDateUtc.getMonth() + 1;
        nMonths += endDateUtc.getMonth();
        nMonths = nMonths <= 0 ? 1 : nMonths;   // if month is less than 0, tell the chart to have 1 month

        this.chartDataInStr = strategyResult.chartData.reverse().join("\n");

        this.nMonthsInTimeFrame = nMonths.toString();

        // click the first item on the TimeFrames toolbar
        // this is better than the gTradingViewChartWidget.postMessage.post(gTradingViewChartWidget._messageTarget(), "loadRangeAgy", because the 'loading data' bug doesn't effect it
        //var z1 = document.getElementById("tv_chart_container");
        //var dateRangeDiv = z1.children[0].contentDocument.childNodes['1'].children['1'].children['library-container'].children['2'].children['chart-area'].children['0'].children['1'].children['1'];
        //dateRangeDiv.children['0'].click();
        //dateRangeDiv.children['0'].innerHTML = "All";   // it takes effect, but if I click it Afterwards, than it will change back to original; so modify the Text After the Click



        //////***!!!!This is the best if we have to work with the official Chart, but postMessage works without this
        //////  Refresh TVChart (make it call the getBars()), version 2: idea stolen from widget.setLangue() inner implementation. It will redraw the Toolbars too, not only the inner area. But it can change TimeFrames Toolbar
        // this part will set up the Timeframes bar properly, but later is chart.onChartReady() you have to click the first button by "dateRangeDiv.children['0'].click();"
        this.tradingViewChartWidget.remove();       // this is the way to the widget.options to be effective
        //gTradingViewChartWidget.options.time_frames[0].text = "All";    // cannot be "All"; it crashes.
        this.tradingViewChartWidget.options.time_frames[0].text = nMonths + "m";
        this.tradingViewChartWidget.options.time_frames[1].text = nMonths + "m";
        this.tradingViewChartWidget.options.time_frames[2].text = nMonths + "m";
        //this.tradingViewChartWidget.options.time_frames[1].text = "61m";    // I can calculate dynamically, but not important now.
        //gTradingViewChartWidget.options.width = "50%";        // works too in Remove(), Create()
        this.tradingViewChartWidget.create()

        ////***!!!! This can be used only with the updated Chart, but the time-frame bar will not update visually, but re-creation will not Blink, as it will not create a short-term version of the chart for 1second
        ////***!!! cannot be used.... because once it goes to the 'loading data' bug, after, it will never refresh the chart. Because it will not ask getBars() ever. So, we have to re-create the chart.
        ////this.tradingViewChartWidget.postMessage.post(this.tradingViewChartWidget._messageTarget(), "loadRangeAgy", {  // don't post this message until the chart is ready() again. Post it later in the onReady() callback.
        ////    res: "D",
        ////    val: nMonths + "m"
        ////})
    }



    onHeadProcessing() {
        console.log('onHeadProcessing()');
    }


    


    //TradingView.onready(function() {
    //    var controllerElement = document.querySelector('body');
    //    var controllerScope = angular.element(controllerElement).scope();

    //    controllerScope.TradingViewOnready();

    //})



    MenuItemStrategyClick(event) {
        console.log("MenuItemStrategyClick() START");

        $(".sqMenuBarLevel2").hide();
        $(".sqMenuBarLevel1").hide();

        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var value = idAttr.nodeValue;
        this.SelectStrategy(value);

        //var controllerElement = document.querySelector('body');
        //var controllerScope = angular.element(controllerElement).scope();

        ////http://jimhoskins.com/2012/12/17/angularjs-and-apply.html
        //controllerScope.$apply(controllerScope.SelectStrategy(element.id));  // use Apply from MenuClick, but you don't have to use it from an Angular function

    }



    SQToggle(hiddenTextID: any, alwaysVisibleSwitchID: any, switchDisplayText: any) {
        var hiddenText = document.getElementById(hiddenTextID);
        var switchElement = document.getElementById(alwaysVisibleSwitchID);
        if (hiddenText.style.display == "block") {
            hiddenText.style.display = "none";
            switchElement.innerHTML = "+ Show " + switchDisplayText;
        } else {
            hiddenText.style.display = "block";
            switchElement.innerHTML = "- Hide " + switchDisplayText;
        }
    }

    OnParameterInputKeypress(event: any) {
        var chCode = ('charCode' in event) ? event.charCode : event.keyCode;
        if (chCode == 13)
            this.MenuItemStartBacktestClicked();
     //alert("The Unicode character code is: " + chCode);
    }















    //public m_data: HMData;
    

    // debug info here
    m_webAppResponse: string;
    m_wasRefreshClicked: any;

    clickMessage = '';
    onClickMe() {
        this.clickMessage = 'You are my hero!';
    }

    ////constructor(http: Http) {        //Exception was thrown at line 4, column 10194 in https://code.angularjs.org/tools/system.js //        0x80000000 - JavaScript runtime error: undefined
    //constructor(private m_HMDataService: HMDataService) {   // private variables become members; sythetic sugar for this.m_a = p_a;
    //    //this.m_http = http;
    //    this.m_wasRefreshClicked = "Refresh was not yet clicked.";
    //}

   

    //getHMData(p_hmDataToSend) {
    //    //this.m_HMDataService.getHttpHMData().then(hmDataReturned => {

    //    this.m_HMDataService.getHttpWithPostHMData(p_hmDataToSend).then(hmDataReturned => {
    //        // don't worry if view is not updated in IE. Angula2 bug in IE. In Chrome and Edge it works.
    //        //console.log("getHttpWithPostHMData() returned");
    //        var hmData: HMData = <HMData>hmDataReturned; // Typescript cast: remember that this is a compile-time cast, and not a runtime cast.
    //        this.m_data = hmData;

    //        // Sadly Javascript loves Local time, so work in Local time; easier;
    //        // 1. StartDate
    //        this.m_data.StartDateLoc = new Date(hmData.StartDate);  // "2015-12-29 00:49:54.000Z", because of the Z Zero, this UTC string is converted properly to local time
    //        this.m_data.StartDate = localDateToUtcString_yyyy_mm_dd_hh_mm_ss(this.m_data.StartDateLoc);    // take away the miliseconds from the dateStr
    //        var localNow = new Date();  // this is local time: <checked>
    //        //var utcNowGetTime = new Date().getTime();  //getTime() returns the number of seconds in UTC.
    //        this.m_data.StartDateTimeSpanStr = getTimeSpanStr(this.m_data.StartDateLoc, localNow);

    //        //this.m_data.ResponseToFrontEnd = "ERROR";

    //        this.m_data.AppOk = 'OK';
    //        if (this.m_data.ResponseToFrontEnd.toUpperCase().indexOf('ERROR') >= 0)
    //            this.m_data.AppOk = 'ERROR';

    //        this.m_data.RtpsOk = 'OK';
    //        for (var i in this.m_data.RtpsDownloads) {
    //            if (this.m_data.RtpsDownloads[i].indexOf('OK') >= 0) {  // if 'OK' is found
    //                continue;
    //            }
    //            this.m_data.RtpsOk = 'ERROR';
    //        }

    //        this.m_data.VBrokerOk = 'OK';
    //        for (var i in this.m_data.VBrokerReports) {
    //            if (this.m_data.VBrokerReports[i].indexOf('OK') >= 0) {  // if 'OK' is found
    //                continue;
    //            }
    //            this.m_data.VBrokerOk = 'ERROR';
    //        }

    //        this.m_webAppResponse = JSON.stringify(hmData);
    //    });
    //}

    //setControlValue(controlName, value) {
    //    console.log("setControlValue():" + controlName + "/" + value);
    //    console.log("setControlValue():" + controlName + "/" + value + "/" + this.m_data.DailyEmailReportEnabled);
    //    if (controlName == 'chkDailyEmail') {
    //        if (this.m_data.DailyEmailReportEnabled != value) {
    //            this.m_data.DailyEmailReportEnabled = value;
    //            this.m_data.CommandToBackEnd = "ApplyTheDifferences";
    //            this.getHMData(this.m_data);
    //        }
    //    } else if (controlName == 'chkRtps') {
    //        if (this.m_data.RtpsTimerEnabled != value) {
    //            this.m_data.RtpsTimerEnabled = value;
    //            this.m_data.CommandToBackEnd = "ApplyTheDifferences";
    //            this.getHMData(this.m_data);
    //        }
    //    } else if (controlName == 'chkVBroker') {
    //        if (this.m_data.ProcessingVBrokerMessagesEnabled != value) {
    //            this.m_data.ProcessingVBrokerMessagesEnabled = value;
    //            this.m_data.CommandToBackEnd = "ApplyTheDifferences";
    //            this.getHMData(this.m_data);
    //        }
    //    }




    //    //this.todo.controls[controlName].updateValue(value);
    //    //this.todo.controls[controlName].markAsDirty();
    //}

    //refreshViewClicked() {
    //    console.log("refreshViewClicked");
    //    this.m_wasRefreshClicked = "refreshViewClicked";
    //}

    //refreshDataClicked() {
    //    console.log("refreshDataClicked");
    //    this.m_wasRefreshClicked = "refreshDataClicked";
    //    this.m_data.CommandToBackEnd = "OnlyGetData";
    //    this.getHMData(this.m_data);
    //}
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




//var gDefaultHMData: HMData = {
//    AppOk: "OK",
//    StartDate: '1998-11-16T00:00:00',
//    StartDateLoc: new Date('1998-11-16T00:00:00'),
//    StartDateTimeSpanStr: '',
//    DailyEmailReportEnabled: false,

//    RtpsOk: 'OK',
//    RtpsTimerEnabled: false,
//    RtpsTimerFrequencyMinutes: -999,
//    RtpsDownloads: ['a', 'b'],

//    VBrokerOk: 'OK',
//    ProcessingVBrokerMessagesEnabled: false,
//    VBrokerReports: ['a', 'b'],
//    VBrokerDetailedReports: ['a', 'b'],

//    CommandToBackEnd: "OnlyGetData",
//    ResponseToFrontEnd: "OK"
//};


