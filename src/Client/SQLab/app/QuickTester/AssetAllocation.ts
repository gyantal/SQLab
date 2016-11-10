import {AppComponent} from './app.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';

export class AssetAllocation {

    //public bullishTradingInstrument = ["Long SPY", "Long ^GSPC", "Long ^IXIC", "Long ^RUT", "Long QQQ", "Long QLD", "Long TQQQ", "Long IWM", "Long IYR", "Short VXX", "Short VXX.SQ", "Short VXZ", "Short VXZ.SQ"];
    //public selectedBullishTradingInstrument = this.bullishTradingInstrument[0];
    //public param = "UseKellyLeverage=false;MaxLeverage=1.0";      // params shouldn't use &, because that is used for URI.Query parameters. Better to avoid it. Use CSV, come separation instead. Or better: ";"

 
    app: AppComponent;
    constructor(p_app: AppComponent) {
        this.app = p_app;
        console.log("AssetAllocation ctor()");
    }

    //public bullishTradingInstrumentChanged(newValue) {
    //    console.log("bullishTradingInstrumentChanged(): " + newValue);
    //    this.selectedBullishTradingInstrument = newValue;
    //    this.app.tipToUser = this.selectedBullishTradingInstrument;
    //}
    
    public SubStrategySelected() {
        if (this.app.selectedStrategyMenuItemId == "idMenuItemTAA") {
            this.app.selectedStrategyName = "Tactical Asset Allocation (TAA)";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1onl2TKr-8RJqlQjsdIcN6X55fSK-4LO9k5T0Ts79-WU";
            this.app.selectedStrategyWebApiName = "TAA";
        }
    }

    public StartBacktest(http: Http) {
        console.log("StartBacktest_AssetAllocation() 1");
        if (this.app.selectedStrategyMenuItemId != "idMenuItemTAA")
            return;
        console.log("StartBacktest_AssetAllocation() 2");

        //var url = "http://localhost/qt?StartDate=&EndDate=&strategy=AdaptiveUberVxx&BullishTradingInstrument=Long%20SPY&param=UseKellyLeverage=false;MaxLeverage=1.0&Name=Fomc&Priority=3&Combination=Avg&StartDate=&EndDate=&TradingStartAt=2y&Param=&Name=Holiday&Priority=&Combination=&StartDate=&EndDate=&TradingStartAt=&Param=&Name=TotM&Priority=2&Combination=Avg&StartDate=&EndDate=&TradingStartAt=2y&Param=TrainingTicker=SPY&Name=Connor&Priority=1&Combination=Avg&StartDate=&EndDate=&TradingStartAt=100td&Param=LookbackWindowDays=100;ProbDailyFTThreshold=47
        var url = "/qt?" + this.app.generalInputParameters + "&strategy=" + this.app.selectedStrategyWebApiName +
            "";
            //"&BullishTradingInstrument=" + this.selectedBullishTradingInstrument + "&param=" + this.param
            //+ "&" + this.fomc.toUrlQueryString()
            //+ "&" + this.holiday.toUrlQueryString()
            //+ "&" + this.totm.toUrlQueryString()
            //+ "&" + this.connor.toUrlQueryString();

        http.get(url)
            .map(res => res.json()) // Call map on the response observable to get the parsed people object
            .subscribe(data => { // Subscribe to the observable to get the parsed people object and attach it to the component
                console.log("StartBacktest_AssetAllocation(): data received 1: " + data);
                this.app.tradingViewChartName = "Asset Allocation";
                this.app.ProcessStrategyResult(data);
            }, error => {
                console.log("ERROR. StartBacktest(): data received error: " + error);
                this.app.errorMessage = error;
            });
    }

}

