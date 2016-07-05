'use strict';
/*
	This class implements interaction with UDF-compatible datafeed.

	See UDF protocol reference at
	https://github.com/tradingview/charting_library/wiki/UDF
*/
/*
1. 
JS Console Warning (2016-07-05): "Synchronous XMLHttpRequest on the main thread is deprecated because of its detrimental effects to the end user's experience. 
For more help, check https://xhr.spec.whatwg.org/."
It is in the TradingView code in Vendors.js, not ours. We cannot do anyting. They should fix it later.
l.open(t.type, t.url="localization/translations/en.json", t.async=false)
>The future direction is to only allow XMLHttpRequests in worker threads. The message is intended to be a warning to that effect.
>Browsers now warn for the use of synchronous XHR.
>Also, I'm aware of LAB.js etc (for guaranteed loading of files in specific order), but again, I just want <myXHRObj>.open("GET", <myURL>, false) to just work in the main thread!! 
>So, question for you - will this sync-loading functionality be eventually deprecated completely?
There is a minor bug at GitHub from 2015-10. They will fix it.
https://github.com/tradingview/charting_library/issues/773
"We will update the i18next configuration for Charing Library when get enough resources to test such changes."
"No, you will have to wait until we fix it."

2.
Javascript Exception (while debugging) only in Chrome (not in IE) (2016-07-05):
TypeError: "Iterator result undefined is not an object at Function.all (native)" at http://localhost/charting_library/charting_library/static/bundles/vendors.7062d5298fb21ccdf295.js:7:15144
message: "Iterator result undefined is not an object"
https://github.com/ckeditor/ckeditor5-engine/issues/454
"Since Chrome 51 we've got these errors in couple of places in our code."
"Apparently, there's something wrong with the new V8. "
Looks like the bug was fixed by V8 team: https://bugs.chromium.org/p/v8/issues/detail?id=5057#c18
"Sorry, I can still reproduce the issue on the fiddle, so I guess Chrome 51 doesn't use that fixed V8 version yet."
Decision: Ignore it Chrome, and upgrade to Chrome 52 whenever is possible.

*/

var Datafeeds = {};

Datafeeds.UDFCompatibleDatafeed = function (angularAppComponent, datafeedURL, updateFrequency, protocolVersion) {
    this.angularAppComponent = angularAppComponent;

    this._datafeedURL = datafeedURL;
    this._configuration = undefined;
    //this._configuration = this.defaultConfiguration();

    this._symbolSearch = null;
    this._symbolsStorage = null;
    //this._barsPulseUpdater = new Datafeeds.DataPulseUpdater(this, updateFrequency || 10 * 1000);
    this._barsPulseUpdater = null;
    //this._quotesPulseUpdater = new Datafeeds.QuotesPulseUpdater(this);
    this._quotesPulseUpdater = null;
    this._protocolVersion = protocolVersion || 2;

    this._enableLogging = true;
    this._initializationFinished = false;
    this._callbacks = {};

    this._initialize();   
};

Datafeeds.UDFCompatibleDatafeed.prototype.defaultConfiguration = function () {
    return {
        supports_search: false,
        supports_group_request: true,
        //supported_resolutions:  ['1D', '1W', '1M'];  // this has to be set; if not TV crashes
        supported_resolutions: ['1', '5', '15', '30', '60', '1D', '1W', '1M'],
        supports_marks: true,
        supports_timescale_marks: false
    };
};


//Datafeeds.UDFCompatibleDatafeed = function (angularAppComponent, datafeedURL, updateFrequency) {

//    this.angularAppComponent = angularAppComponent;

//    //this._datafeedURL = datafeedURL;
//    this._configuration = {};   // this is the DefaultConfig in the Test
//    this._configuration.supports_search = false;
//    this._configuration.supports_group_request = true;
//    //this._configuration.supported_resolutions = ["1D", "1W", "1M"];  // this has to be set; if not TV crashes
//    supported_resolutions: ["1", "5", "15", "30", "60", "1D", "1W", "1M"]
//    this._configuration.supports_marks = true;

//    this._enableLogging = true;

//    this._barsPulseUpdater = null;
//};


Datafeeds.UDFCompatibleDatafeed.prototype.getServerTime = function (callback) {
    if (this._configuration.supports_time) {
        this._send(this._datafeedURL + '/time', {})
			.done(function (response) {
			    callback(+response);
			})
			.fail(function () {

			});
    }
};

Datafeeds.UDFCompatibleDatafeed.prototype.on = function (event, callback) {

    if (!this._callbacks.hasOwnProperty(event)) {
        this._callbacks[event] = [];
    }

    this._callbacks[event].push(callback);
    return this;
};

Datafeeds.UDFCompatibleDatafeed.prototype._fireEvent = function (event, argument) {
    if (this._callbacks.hasOwnProperty(event)) {
        var callbacksChain = this._callbacks[event];
        for (var i = 0; i < callbacksChain.length; ++i) {
            callbacksChain[i](argument);
        }

        this._callbacks[event] = [];
    }
};

Datafeeds.UDFCompatibleDatafeed.prototype.onInitialized = function () {
    this._initializationFinished = true;
    this._fireEvent('initialized');
};

Datafeeds.UDFCompatibleDatafeed.prototype._logMessage = function (message) {
    if (this._enableLogging) {
        var now = new Date();
        console.log(now.toLocaleTimeString() + '.' + now.getMilliseconds() + '> ' + message);
    }
};

Datafeeds.UDFCompatibleDatafeed.prototype._send = function (url, params) {
    var request = url;
    if (params) {
        for (var i = 0; i < Object.keys(params).length; ++i) {
            var key = Object.keys(params)[i];
            var value = encodeURIComponent(params[key]);
            request += (i === 0 ? '?' : '&') + key + '=' + value;
        }
    }

    this._logMessage('New request: ' + request);

    return $.ajax({
        type: 'GET',
        url: request,
        contentType: 'text/plain'
    });
};

Datafeeds.UDFCompatibleDatafeed.prototype._initialize = function () {   

    var that = this;
    that._setupWithConfiguration(that.defaultConfiguration());  // no need to download config.json from server

    //this._send(this._datafeedURL + '/config')
	//	.done(function (response) {
	//	    var configurationData = JSON.parse(response);
	//	    that._setupWithConfiguration(configurationData);
	//	})
	//	.fail(function (reason) {
	//	    that._setupWithConfiguration(that.defaultConfiguration());
	//	});
};


//	===============================================================================================================================
//	The functions set below is the implementation of JavaScript API. https://github.com/tradingview/charting_library/wiki/JS-Api


Datafeeds.UDFCompatibleDatafeed.prototype.onReady = function (callback) {
    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.onReady");
    var that = this;
    if (this._configuration) {
        setTimeout(function () {
            callback(that._configuration);
        }, 0);
    } else {
        this.on('configuration_ready', function () {
            callback(that._configuration);
        });
    }

    // this can be the real-time price Updater part
    setInterval(function (datafeedParam) {
        //var todayDate = new Date();
        //var lastBarDate = new Date(todayDate.getUTCFullYear(), todayDate.getUTCMonth(), todayDate.getUTCDate(), 0, 0, 0, 0);
        //var daysToGoBackForLastTradingDay = 1;      // to go back to the last trading day in the SQL history DB
        //if (todayDate.getDay() == 0) {    // Sunday
        //    daysToGoBackForLastTradingDay = 2
        //}
        //if (todayDate.getDay() == 1) {    // Monday
        //    daysToGoBackForLastTradingDay = 3
        //}
        //lastBarDate.setUTCDate(lastBarDate.getUTCDate() - daysToGoBackForLastTradingDay);   // yesterday: 00:00     (on Sunday; it does'nt work. yesterday is saturday) and we don't want that.
        //var weekDay = lastBarDate.getDay();
        //var lastBar = {
        //    time: lastBarDate.getTime(),  // gives back the miliseconds, so it is OK.  //time: data.t[i] * 1000,
        //    close: 10.0 + weekDay + Math.floor((Math.random() * 10) + 1)
        //};
        //lastBar.open = lastBar.close - 0.25;
        //lastBar.high = lastBar.close + 0.25;
        //lastBar.low = lastBar.close - 0.55;
        //var rtSubsc = that._barsPulseUpdater;
        //if (rtSubsc != null) {
        //    rtSubsc.listeners[0](lastBar)
        //}
    }, 10 * 1000); // Pulse every 10 seconds
};

//Datafeeds.UDFCompatibleDatafeed.prototype.onReady = function (callback) {
//    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.onReady");
//    //this._configuration.engine = studyEngineOptions;
//    callback(this._configuration);


//    // this is the real-time price Updater part
//    var that = this;
//    setInterval(function (datafeedParam) {

//        //var todayDate = new Date();
//        //var lastBarDate = new Date(todayDate.getUTCFullYear(), todayDate.getUTCMonth(), todayDate.getUTCDate(), 0, 0, 0, 0);
//        //var daysToGoBackForLastTradingDay = 1;      // to go back to the last trading day in the SQL history DB
//        //if (todayDate.getDay() == 0) {    // Sunday
//        //    daysToGoBackForLastTradingDay = 2
//        //}
//        //if (todayDate.getDay() == 1) {    // Monday
//        //    daysToGoBackForLastTradingDay = 3
//        //}
//        //lastBarDate.setUTCDate(lastBarDate.getUTCDate() - daysToGoBackForLastTradingDay);   // yesterday: 00:00     (on Sunday; it does'nt work. yesterday is saturday) and we don't want that.
//        //var weekDay = lastBarDate.getDay();
//        //var lastBar = {
//        //    time: lastBarDate.getTime(),  // gives back the miliseconds, so it is OK.  //time: data.t[i] * 1000,
//        //    close: 10.0 + weekDay + Math.floor((Math.random() * 10) + 1)
//        //};
//        //lastBar.open = lastBar.close - 0.25;
//        //lastBar.high = lastBar.close + 0.25;
//        //lastBar.low = lastBar.close - 0.55;
//        //var rtSubsc = that._barsPulseUpdater;
//        //if (rtSubsc != null) {
//        //    rtSubsc.listeners[0](lastBar)
//        //}
//    }, 10 * 1000); // Pulse every 10 seconds

//};

//Datafeeds.UDFCompatibleDatafeed.prototype.searchSymbolsByName = function (ticker, exchange, type, onResultReadyCallback) {
//    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.searchSymbolsByName");
//    //If no symbols are found, then callback should be called with an empty array. 
//    onResultReadyCallback([]);
//};

Datafeeds.UDFCompatibleDatafeed.prototype._setupWithConfiguration = function (configurationData) {
    this._configuration = configurationData;

    if (!configurationData.exchanges) {
        configurationData.exchanges = [];
    }

    //	@obsolete; remove in 1.5
    var supportedResolutions = configurationData.supported_resolutions || configurationData.supportedResolutions;
    configurationData.supported_resolutions = supportedResolutions;

    //	@obsolete; remove in 1.5
    var symbolsTypes = configurationData.symbols_types || configurationData.symbolsTypes;
    configurationData.symbols_types = symbolsTypes;

    if (!configurationData.supports_search && !configurationData.supports_group_request) {
        throw 'Unsupported datafeed configuration. Must either support search, or support group request';
    }

    if (!configurationData.supports_search) {
        this._symbolSearch = new Datafeeds.SymbolSearchComponent(this);
    }

    if (configurationData.supports_group_request) {
        //	this component will call onInitialized() by itself
        this._symbolsStorage = new Datafeeds.SymbolsStorage(this);
    } else {
        this.onInitialized();
    }

    this._fireEvent('configuration_ready');
    this._logMessage('Initialized with ' + JSON.stringify(configurationData));
};



Datafeeds.UDFCompatibleDatafeed.prototype._symbolResolveURL = '/symbols';

//	BEWARE: this function does not consider symbol's exchange
Datafeeds.UDFCompatibleDatafeed.prototype.resolveSymbol = function (symbolName, onSymbolResolvedCallback, onResolveErrorCallback) {

    var that = this;

    if (!this._initializationFinished) {
        this.on('initialized', function () {
            that.resolveSymbol(symbolName, onSymbolResolvedCallback, onResolveErrorCallback);
        });

        return;
    }

    var resolveRequestStartTime = Date.now();
    that._logMessage('Resolve requested');

    function onResultReady(data) {
        var postProcessedData = data;
        if (that.postProcessSymbolInfo) {
            postProcessedData = that.postProcessSymbolInfo(postProcessedData);
        }

        that._logMessage('Symbol resolved: ' + (Date.now() - resolveRequestStartTime));

        onSymbolResolvedCallback(postProcessedData);
    }

    if (!this._configuration.supports_group_request) {
        this._send(this._datafeedURL + this._symbolResolveURL, {
            symbol: symbolName ? symbolName.toUpperCase() : ''
        })
			.done(function (response) {
			    var data = JSON.parse(response);

			    if (data.s && data.s != 'ok') {
			        onResolveErrorCallback('unknown_symbol');
			    } else {
			        onResultReady(data);
			    }
			})
			.fail(function (reason) {
			    that._logMessage('Error resolving symbol: ' + JSON.stringify([reason]));
			    onResolveErrorCallback('unknown_symbol');
			});
    } else {
        if (this._initializationFinished) {
            this._symbolsStorage.resolveSymbol(symbolName, onResultReady, onResolveErrorCallback);
        } else {
            this.on('initialized', function () {
                that._symbolsStorage.resolveSymbol(symbolName, onResultReady, onResolveErrorCallback);
            });
        }
    }
};

//This is exactly what TradingView1.1Index.html gives back, so keep only that
//description	"Alcoa Inc."	String
//exchange-listed	"NYSE"	String
//exchange-traded	"NYSE"	String
//has_intraday	false	Boolean
//has_no_volume	false	Boolean
//minmov	1	Number
//minmov2	0	Number
//name	"AA"	String
//pointvalue	1	Number
//pricescale	10	Number
//session	"0930-1630"	String
//ticker	"AA"	String
//timezone	"UTC"	String
//type	"stock"	String
//Symbol resolved: `AA`, SymbolInfo in server response {"name":"AA","exchange-traded":"NYSE","exchange-listed":"NYSE","timezone":"UTC","minmov":1,"minmov2":0,"pricescale":10,"pointvalue":1,"session":"0930-1630","has_intraday":false,"has_no_volume":false,"ticker":"AA","description":"Alcoa Inc.","type":"stock"}
//Symbol info after post-processing: `AA`, SymbolInfo {"name":"AA","exchange-traded":"NYSE","exchange-listed":"NYSE","timezone":"UTC","minmov":1,"minmov2":0,"pricescale":10,"pointvalue":1,"session":"0930-1630","has_intraday":false,"has_no_volume":false,"ticker":"AA","description":"Alcoa Inc.","type":"stock","base_name":["AA"],"legs":["AA"],"exchange":"NYSE","full_name":"NYSE:AA","data_status":"streaming"}

////	BEWARE: this function does not consider symbol's exchange
//// https://github.com/tradingview/charting_library/wiki/Symbology#symbolinfo-structure
//Datafeeds.UDFCompatibleDatafeed.prototype.resolveSymbol = function (symbolName, onSymbolResolvedCallback, onResolveErrorCallback) {
//    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.resolveSymbol");
//    var data = {};
//    data.description = "PV";  //Will be printed in chart legend for this symbol.
//    data['exchange-listed'] = "NYSE";
//    data['exchange-traded'] = "NYSE";
//    data.has_intraday = false;
//    data.has_no_volume = true;
//    data.minmov = 1;
//    data.minmov2 = 0;

//    data.pointvalue = 1;
//    data.pricescale = 100;
//    data.session = "0930-1630";
//    //data.session = "24x7"; doesn't solve the 'loading data' problem
//    data.ticker = "SQ1"; //It's an unique identifier for this symbol in your symbology. If you specify this property then its value will be used for all data requests for this symbol. ticker is treated to be equal to symbol if not specified explicitly.
//    data.timezone = "UTC";
//    //data.timezone = "America/New_York";
//    data.type = "stock";

//    if (this.angularAppComponent.tradingViewChartName == null) {
//        data.name = "SQ strategy name";     //It's name of a symbol. It is a string which your users will see.
//    } else {
//        data.name = this.angularAppComponent.tradingViewChartName;     //It's name of a symbol. It is a string which your users will see.
//    }

//    //after post processing, these will be the defaults: streaming
//    //"base_name":["AA"],"legs":["AA"],"exchange":"NYSE","full_name":"NYSE:AA","data_status":"streaming"

//    //data.supported_resolutions = ["D", "W", "M", "6M"];        // if this is given not exactly how the 'time_frames', then it will not appear
//    //data.has_daily = true; //If has_daily = false then Charting Library will build respective resolutions from intraday by itself. If not, then it will request those bars from datafeed.
//    //data.has_weekly_and_monthly = false;
//    //data.data_status = "endofday";

//    onSymbolResolvedCallback(data);
//};


//calculateHistoryDepth(period, resolutionBack, intervalBack)
//One may affect requested data range by overriding this function. The Charting Library will call your function and use resolutionBack and intervalBack returned by it (if any).
//        Arguments:
//        period: requested symbol's resolution. You may not change it.
//        resolutionBack: desired history period type. `D` (days) or `M` (months) is expected.
//        intervalBack: desired history depth (periods, see resolutionBack)
//Returned value:
//    Your function should return nothing if you do not want to override anything. If you do, it should return an object with respective properties. Only resolutionBack and intervalBack properties are expected.
//        Example:
//        Assume the implementation is
//    Datafeed.prototype.calculateHistoryDepth = function(period,
//    resolutionBack, intervalBack) {
//        if (period == "1D") {
//            return {
//                resolutionBack: 'M',
//                intervalBack: 6
//            };
//        }
//    }
//    This means when Charting Library will request the data for '1D' resolution, the history will be 6 months in depth. In all other cases the history depth will have the default value.
// after I call gTradingViewChartWidget.remove();       , this will be called again. So I can return to the chart, how much data I have.
Datafeeds.UDFCompatibleDatafeed.prototype.calculateHistoryDepth = function (period, resolutionBack, intervalBack) {     // default"D", "M", 12 (which means 6 months is show for daily resolution
    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.calculateHistoryDepth");
    //This function should return undefined if you do not want to override anything. 
    if (period == "D") {
        //return {
        //    resolutionBack: "D",
        //    intervalBack: 870     //if I return 870 Days, getBars() will ask for only 250 days of data. However, if I return 43Months, getBars() asks for 870 data, with a proper startdate. However, the data is there, but it is not rendered.
        return {
            resolutionBack: "M",
            intervalBack: 43
            //intervalBack: 260 * 5      // better to tell TV to ask 5 years of data in one big chunk. Because later when user scrolls, or zooms out, the 'loading data' problem will not appear
            // unfortunately, even if we say, there are 2000 days available, it only asks for 200 data, if the browser window is very wide. Let's wait 
        };

    }
};

//How can I make data_status invisible?, https://github.com/tradingview/charting_library/issues/311
//It's an important part of our UI and there is no way to hide it. Virtually, you can override CSS style for this item but we do not recommend to do this.

// rangeStartDate, rangeEndDate comes in UNIX style, which is seconds; so convert it to msec for JS
Datafeeds.UDFCompatibleDatafeed.prototype.getBars = function (symbolInfo, resolution, rangeStartDate, rangeEndDate, onDataCallback, onErrorCallback) {
    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.getBars");
    //	timestamp sample: 1399939200
    if (rangeStartDate > 0 && (rangeStartDate + "").length > 10) {
        throw "Got a JS time instead of Unix one.";
    }

    var bars = [];

    var startDate = new Date((rangeStartDate) * 1000);    //15h extra because of UTC time and summer time winter time change on 26th October  // rangeStartDate Time field is: 00:00
    var endDate = new Date((rangeEndDate) * 1000);    // rangeEndDate is pretty real time with its Time attribute as seconds

    if (this.angularAppComponent.chartDataToChart == null) {

        var iDate = new Date((rangeStartDate) * 1000);
        //iDate.setUTCDate(iDate.getUTCDate() - 280);  //https://github.com/tradingview/charting_library/issues/266 , Didn't work Now we use our way to return more data than the library request so the library will keep request when scroll to end until actually no more data at server.

        while (iDate <= endDate) {
            if (iDate.getFullYear() == 2014 && iDate.getMonth() == 9 && iDate.getDate() == 24) {
                var x = 0;
            }

            var weekDay = iDate.getDay();
            if (weekDay != 0 && weekDay != 6) {
                var barValue = {
                    time: iDate.getTime(),  // gives back the miliseconds, so it is OK.  //time: data.t[i] * 1000,
                    close: 10.0 + weekDay
                };

                barValue.open = barValue.close - 0.25;
                barValue.high = barValue.close + 0.25;
                barValue.low = barValue.close - 0.55;

                //barValue.open = barValue.high = barValue.low = barValue.close;

                //var nextDate = new Date(iDate.getTime());
                //nextDate.setDate(nextDate.getDate() + 1);

                if (iDate.getFullYear() == 2015 && iDate.getMonth() == 0 && iDate.getDate() == 1) { // new years: holiday
                    //bars.push(barValue);
                    var x = 0;
                } else {
                    bars.push(barValue);
                }

                //if (nextDate <= endDate) {  // if nextDate is out of range, so this is the last date, don't put it to the stack.
                //    bars.push(barValue);
                //} else {
                //    var yy = 0;
                //}
            }



            //iDate.setDate(iDate.getDate() + 1);   // not good
            iDate.setUTCDate(iDate.getUTCDate() + 1);   // this solved the disappearing days: when 2 bars went to the same day. other idea was: increase the time as miliseconds with another 24hours every time.

        }
    } else {
        for (var i = 0; i < this.angularAppComponent.chartDataToChart.length; i++) {
            if (this.angularAppComponent.chartDataToChart[i].time >= rangeStartDate * 1000 && this.angularAppComponent.chartDataToChart[i].time <= rangeEndDate * 1000) {
                bars.push(this.angularAppComponent.chartDataToChart[i]);
            }
        }
    }


    //// repeat the last one value, but with the endDate; with the proper time
    //var barValue = {
    //    time: (rangeEndDate - 1) * 1000,  // gives back the miliseconds, so it is OK.  //time: data.t[i] * 1000,
    //    close: 18
    //};
    //barValue.open = barValue.close - 0.25;
    //barValue.high = barValue.close + 0.25;
    //barValue.low = barValue.close - 0.55;
    //bars.push(barValue);


    onDataCallback(bars);
};


Datafeeds.UDFCompatibleDatafeed.prototype.subscribeBars = function (symbolInfo, resolution, onRealtimeCallback, listenerGUID) {
    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.subscribeBars");

    this._logMessage("Subscribing " + listenerGUID);
    this._barsPulseUpdater = {
        symbolInfo: symbolInfo,
        resolution: resolution,
        lastBarTime: NaN,
        listeners: []
    };
    this._barsPulseUpdater.listeners.push(onRealtimeCallback);

};

Datafeeds.UDFCompatibleDatafeed.prototype.unsubscribeBars = function (listenerGUID) {
    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.unsubscribeBars");
    this._logMessage("Unsubscribing " + listenerGUID);
    this._barsPulseUpdater = null;
};





Datafeeds.UDFCompatibleDatafeed.prototype.getMarks = function (symbolInfo, rangeStart, rangeEnd, onDataCallback, resolution) {
    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.getMarks");
    //if (this._configuration.supports_marks) {
    //    this._send(this._datafeedURL + "/marks", {
    //        symbol: symbolInfo.ticker.toUpperCase(),
    //        from: rangeStart,
    //        to: rangeEnd,
    //        resolution: resolution
    //    })
    //		.done(function (response) {
    //		    onDataCallback(JSON.parse(response));
    //		})
    //		.fail(function () {
    //		    onDataCallback([]);
    //		});
    //}
};


Datafeeds.UDFCompatibleDatafeed.prototype.getQuotes = function (symbols, onDataCallback, onErrorCallback) {
    console.log("Datafeeds.UDFCompatibleDatafeed.prototype.getQuotes");
    //    this._send(this._datafeedURL + "/quotes", { symbols: symbols })
    //		.done(function (response) {
    //		    var data = JSON.parse(response);
    //		    if (data.s == "ok") {
    //		        //	JSON format is {s: "status", [{s: "symbol_status", n: "symbol_name", v: {"field1": "value1", "field2": "value2", ..., "fieldN": "valueN"}}]}
    //		        onDataCallback && onDataCallback(data.d);
    //		    } else {
    //		        onErrorCallback && onErrorCallback(data.errmsg);
    //		    }
    //		})
    //		.fail(function (arg) {
    //		    onErrorCallback && onErrorCallback("network error: " + arg);
    //		});
};

//	==================================================================================================================================================
//	==================================================================================================================================================
//	==================================================================================================================================================

/*
	It's a symbol storage component for ExternalDatafeed. This component can
	  * interact to UDF-compatible datafeed which supports whole group info requesting
	  * do symbol resolving -- return symbol info by its name
*/
Datafeeds.SymbolsStorage = function (datafeed) {
    this._datafeed = datafeed;

    this._exchangesList = ['NYSE', 'FOREX', 'AMEX'];
    this._exchangesWaitingForData = {};
    this._exchangesDataCache = {};

    this._symbolsInfo = {};
    this._symbolsList = [];

    //this._requestFullSymbolsList();
    this._datafeed.onInitialized();     // set initilazid=true on the data field
};

Datafeeds.SymbolsStorage.prototype._requestFullSymbolsList = function () {

    var that = this;
    var datafeed = this._datafeed;

    for (var i = 0; i < this._exchangesList.length; ++i) {

        var exchange = this._exchangesList[i];

        if (this._exchangesDataCache.hasOwnProperty(exchange)) {
            continue;
        }

        this._exchangesDataCache[exchange] = true;

        this._exchangesWaitingForData[exchange] = 'waiting_for_data';

        this._datafeed._send(this._datafeed._datafeedURL + '/symbol_info', {
            group: exchange
        })
			.done(function (exchange) {
			    return function (response) {
			        that._onExchangeDataReceived(exchange, JSON.parse(response));
			        that._onAnyExchangeResponseReceived(exchange);
			    };
			}(exchange)) //jshint ignore:line
			.fail(function (exchange) {
			    return function (reason) {
			        that._onAnyExchangeResponseReceived(exchange);
			    };
			}(exchange)); //jshint ignore:line
    }
};

Datafeeds.SymbolsStorage.prototype._onExchangeDataReceived = function (exchangeName, data) {

    function tableField(data, name, index) {
        return data[name] instanceof Array ?
			data[name][index] :
			data[name];
    }

    try {
        for (var symbolIndex = 0; symbolIndex < data.symbol.length; ++symbolIndex) {

            var symbolName = data.symbol[symbolIndex];
            var listedExchange = tableField(data, 'exchange-listed', symbolIndex);
            var tradedExchange = tableField(data, 'exchange-traded', symbolIndex);
            var fullName = tradedExchange + ':' + symbolName;

            //	This feature support is not implemented yet
            //	var hasDWM = tableField(data, "has-dwm", symbolIndex);

            var hasIntraday = tableField(data, 'has-intraday', symbolIndex);

            var tickerPresent = typeof data.ticker != 'undefined';

            var symbolInfo = {
                name: symbolName,
                base_name: [listedExchange + ':' + symbolName],
                description: tableField(data, 'description', symbolIndex),
                full_name: fullName,
                legs: [fullName],
                has_intraday: hasIntraday,
                has_no_volume: tableField(data, 'has-no-volume', symbolIndex),
                listed_exchange: listedExchange,
                exchange: tradedExchange,
                minmov: tableField(data, 'minmovement', symbolIndex) || tableField(data, 'minmov', symbolIndex),
                minmove2: tableField(data, 'minmove2', symbolIndex) || tableField(data, 'minmov2', symbolIndex),
                fractional: tableField(data, 'fractional', symbolIndex),
                pointvalue: tableField(data, 'pointvalue', symbolIndex),
                pricescale: tableField(data, 'pricescale', symbolIndex),
                type: tableField(data, 'type', symbolIndex),
                session: tableField(data, 'session-regular', symbolIndex),
                ticker: tickerPresent ? tableField(data, 'ticker', symbolIndex) : symbolName,
                timezone: tableField(data, 'timezone', symbolIndex),
                supported_resolutions: tableField(data, 'supported-resolutions', symbolIndex) || this._datafeed.defaultConfiguration().supported_resolutions,
                force_session_rebuild: tableField(data, 'force-session-rebuild', symbolIndex) || false,
                has_daily: tableField(data, 'has-daily', symbolIndex) || true,
                intraday_multipliers: tableField(data, 'intraday-multipliers', symbolIndex) || ['1', '5', '15', '30', '60'],
                has_fractional_volume: tableField(data, 'has-fractional-volume', symbolIndex) || false,
                has_weekly_and_monthly: tableField(data, 'has-weekly-and-monthly', symbolIndex) || false,
                has_empty_bars: tableField(data, 'has-empty-bars', symbolIndex) || false,
                volume_precision: tableField(data, 'volume-precision', symbolIndex) || 0
            };

            this._symbolsInfo[symbolInfo.ticker] = this._symbolsInfo[symbolName] = this._symbolsInfo[fullName] = symbolInfo;
            this._symbolsList.push(symbolName);
        }
    }
    catch (error) {
        throw 'API error when processing exchange `' + exchangeName + '` symbol #' + symbolIndex + ': ' + error;
    }
};

Datafeeds.SymbolsStorage.prototype._onAnyExchangeResponseReceived = function (exchangeName) {

    delete this._exchangesWaitingForData[exchangeName];

    var allDataReady = Object.keys(this._exchangesWaitingForData).length === 0;

    if (allDataReady) {
        this._symbolsList.sort();
        this._datafeed._logMessage('All exchanges data ready');
        this._datafeed.onInitialized();
    }
};

//	BEWARE: this function does not consider symbol's exchange
Datafeeds.SymbolsStorage.prototype.resolveSymbol = function (symbolName, onSymbolResolvedCallback, onResolveErrorCallback) {
    var that = this;

    setTimeout(function () {
        // inside setTimeout(), this becamase the Window object. That is why 'that' temporary variable is needed
        console.log("Datafeeds.UDFCompatibleDatafeed.prototype.resolveSymbol: symbol name: " + symbolName);
        var data = {};
        data.description = "PV";  //Will be printed in chart legend for this symbol.
        data['exchange-listed'] = "NYSE";
        data['exchange-traded'] = "NYSE";
        data.has_intraday = false;
        data.has_no_volume = true;
        data.minmov = 1;
        data.minmov2 = 0;

        data.pointvalue = 1;
        data.pricescale = 100;
        data.session = "0930-1630";
        //data.session = "24x7"; doesn't solve the 'loading data' problem
        data.ticker = "SQ1"; //It's an unique identifier for this symbol in your symbology. If you specify this property then its value will be used for all data requests for this symbol. ticker is treated to be equal to symbol if not specified explicitly.
        data.timezone = "UTC";
        //data.timezone = "America/New_York";
        data.type = "stock";

        if (that._datafeed.angularAppComponent.tradingViewChartName == null) {
            data.name = "SQ strategy name";     //It's name of a symbol. It is a string which your users will see.
        } else {
            data.name = that._datafeed.angularAppComponent.tradingViewChartName;     //It's name of a symbol. It is a string which your users will see.
        }

        //after post processing, these will be the defaults: streaming
        //"base_name":["AA"],"legs":["AA"],"exchange":"NYSE","full_name":"NYSE:AA","data_status":"streaming"

        //data.supported_resolutions = ["D", "W", "M", "6M"];        // if this is given not exactly how the 'time_frames', then it will not appear
        //data.has_daily = true; //If has_daily = false then Charting Library will build respective resolutions from intraday by itself. If not, then it will request those bars from datafeed.
        //data.has_weekly_and_monthly = false;
        //data.data_status = "endofday";
        onSymbolResolvedCallback(data);

        //if (!that._symbolsInfo.hasOwnProperty(symbolName)) {
        //    onResolveErrorCallback('invalid symbol');
        //} else {
        //    onSymbolResolvedCallback(that._symbolsInfo[symbolName]);
        //}
    }, 0);
};


//	==================================================================================================================================================
//	==================================================================================================================================================
//	==================================================================================================================================================

/*
	It's a symbol search component for ExternalDatafeed. This component can do symbol search only.
	This component strongly depends on SymbolsDataStorage and cannot work without it. Maybe, it would be
	better to merge it to SymbolsDataStorage.
*/

Datafeeds.SymbolSearchComponent = function (datafeed) {
    this._datafeed = datafeed;
};

//	searchArgument = { searchString, onResultReadyCallback}
Datafeeds.SymbolSearchComponent.prototype.searchSymbols = function (searchArgument, maxSearchResults) {

    if (!this._datafeed._symbolsStorage) {
        throw 'Cannot use local symbol search when no groups information is available';
    }

    var symbolsStorage = this._datafeed._symbolsStorage;

    var results = []; // array of WeightedItem { item, weight }
    var queryIsEmpty = !searchArgument.searchString || searchArgument.searchString.length === 0;
    var searchStringUpperCase = searchArgument.searchString.toUpperCase();

    for (var i = 0; i < symbolsStorage._symbolsList.length; ++i) {
        var symbolName = symbolsStorage._symbolsList[i];
        var item = symbolsStorage._symbolsInfo[symbolName];

        if (searchArgument.type && searchArgument.type.length > 0 && item.type != searchArgument.type) {
            continue;
        }

        if (searchArgument.exchange && searchArgument.exchange.length > 0 && item.exchange != searchArgument.exchange) {
            continue;
        }

        var positionInName = item.name.toUpperCase().indexOf(searchStringUpperCase);
        var positionInDescription = item.description.toUpperCase().indexOf(searchStringUpperCase);

        if (queryIsEmpty || positionInName >= 0 || positionInDescription >= 0) {
            var found = false;
            for (var resultIndex = 0; resultIndex < results.length; resultIndex++) {
                if (results[resultIndex].item == item) {
                    found = true;
                    break;
                }
            }

            if (!found) {
                var weight = positionInName >= 0 ? positionInName : 8000 + positionInDescription;
                results.push({ item: item, weight: weight });
            }
        }
    }

    searchArgument.onResultReadyCallback(
		results
			.sort(function (weightedItem1, weightedItem2) {
			    return weightedItem1.weight - weightedItem2.weight;
			})
			.map(function (weightedItem) {
			    var item = weightedItem.item;
			    return {
			        symbol: item.name,
			        full_name: item.full_name,
			        description: item.description,
			        exchange: item.exchange,
			        params: [],
			        type: item.type,
			        ticker: item.name
			    };
			})
			.slice(0, Math.min(results.length, maxSearchResults))
	);
};